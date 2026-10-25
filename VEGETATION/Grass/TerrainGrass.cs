// GPU ribbon grass adapted from Terrain3D's MIT example. Fixed tile pool, event-driven camera/data updates.
using Godot;
using System.Collections.Generic;

public partial class TerrainGrass : Node3D
{
    [Export] public VegetationConfig Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    private Node3D _terrain = null!;
    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private Camera3D _camera = null!;
    private ShaderMaterial _placement = null!, _ribbon = null!;
    private readonly List<GpuParticles3D> _tiles = new();
    private readonly List<Vector2I> _coordinates = new();
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _half, _width;
    private float _tileWidth, _radius;
    private bool _refreshQueued, _initialized;
    private readonly string[] _terrainSignals = { "maps_changed", "region_map_changed", "height_maps_changed", "control_maps_changed", "color_maps_changed" };
    public int AllocatedParticles { get; private set; }
    public int TerrainBindingUpdates { get; private set; }
    public int TileRestarts { get; private set; }

    // =========================================================
    // Attach only after the world has selected saved terrain and placed its explorer.
    public void Initialize(Node3D terrain, ExplorationController explorer)
    {
        _terrain = terrain; _data = terrain.Get("data").AsGodotObject();
        _explorer = explorer; _camera = explorer.GetNode<Camera3D>("Camera3D");
        _explorer.ViewPositionChanged += OnCameraMoved;
        foreach (string signal in _terrainSignals) _data.Connect(signal, Callable.From(QueueTerrainRefresh));
        _initialized = true;
        SetProcess(false); SetPhysicsProcess(false);
        ApplySettings();
    }

    // =========================================================
    // Rebuild explicitly after configuration changes; OFF/zero density removes all GPU emitters.
    public void ApplySettings()
    {
        foreach (var tile in _tiles) { tile.Emitting = false; tile.ProcessMode = ProcessModeEnum.Disabled; RemoveChild(tile); tile.QueueFree(); }
        _tiles.Clear(); _coordinates.Clear(); AllocatedParticles = 0;
        _center = new Vector2I(int.MinValue, int.MinValue);
        if (!_initialized || !Settings.Enabled || Settings.Density <= 0) return;
        _tileWidth = Mathf.Clamp(Settings.TileWidth, 16, 64);
        _radius = Mathf.Clamp(Settings.RenderDistance, 8, 128);
        _half = Mathf.CeilToInt(_radius / _tileWidth); _width = _half * 2 + 1;
        float qualityDensity = Settings.Quality switch { VegetationConfig.GrassQuality.Low => 0.25f, VegetationConfig.GrassQuality.Medium => 0.6f, _ => 1f };
        float spacing = 0.25f / Mathf.Sqrt(Mathf.Clamp(Settings.Density, 0.01f, 2f) * qualityDensity);
        int budget = Mathf.Clamp(Settings.ParticleBudget, 10000, 1000000);
        int rows = Mathf.Min(Mathf.Max(1, Mathf.FloorToInt(_tileWidth / spacing)), Mathf.FloorToInt(Mathf.Sqrt((float)budget / (_width * _width))));
        spacing = _tileWidth / rows; // Exact tiling: no gaps/overlaps when budget limits density.
        _placement = (ShaderMaterial)GD.Load<ShaderMaterial>("res://VEGETATION/Grass/PlacementMaterial.tres").Duplicate();
        _ribbon = (ShaderMaterial)GD.Load<ShaderMaterial>("res://VEGETATION/Grass/RibbonMaterial.tres").Duplicate();
        Vector2 wind = EnvironmentSettings.WindDirection;
        wind = wind.LengthSquared() > 0.0001f ? wind.Normalized() : Vector2.Right;
        _placement.SetShaderParameter("wind_direction", wind); _ribbon.SetShaderParameter("wind_direction", wind);
        _placement.SetShaderParameter("wind_strength", Mathf.Clamp(EnvironmentSettings.GrassWindStrength, 0, 1));
        _placement.SetShaderParameter("wind_speed", EnvironmentSettings.GrassWindSpeed);
        _placement.SetShaderParameter("instance_rows", rows); _placement.SetShaderParameter("instance_spacing", spacing);
        _placement.SetShaderParameter("position_offset", new Vector3(0, 0.45f, 0));
        float height = Mathf.Clamp(Settings.Height, 0.1f, 3);
        float bladeWidth = Mathf.Clamp(Settings.BladeWidth, 0.03f, 0.3f);
        _placement.SetShaderParameter("min_scale", new Vector3(bladeWidth, 0.5f * height, bladeWidth));
        _placement.SetShaderParameter("max_scale", new Vector3(bladeWidth, height, bladeWidth));
        _placement.SetShaderParameter("clod_scale_boost", Mathf.Clamp(Settings.ClumpHeightBoost, 0, 4) * height);
        _placement.SetShaderParameter("surface_slope_min", Mathf.Clamp(Settings.MinimumNormalY, 0, 1));
        _placement.SetShaderParameter("main_noise_scale", Mathf.Max(0.0001f, Settings.PatchNoiseScale));
        float patchMin = Mathf.Clamp(Settings.PatchThresholds.X, 0, 0.999f);
        float clumpMin = Mathf.Clamp(Settings.ClumpThresholds.X, 0, 0.999f);
        _placement.SetShaderParameter("patch_min_threshold", patchMin);
        _placement.SetShaderParameter("patch_max_threshold", Mathf.Clamp(Settings.PatchThresholds.Y, patchMin + 0.001f, 1));
        _placement.SetShaderParameter("clod_min_threshold", clumpMin);
        _placement.SetShaderParameter("clod_max_threshold", Mathf.Clamp(Settings.ClumpThresholds.Y, clumpMin + 0.001f, 1));
        _ribbon.SetShaderParameter("render_distance", _radius);
        _ribbon.SetShaderParameter("fade_start_fraction", Mathf.Clamp(Settings.FadeStartFraction, 0.1f, 0.95f));
        _ribbon.SetShaderParameter("blade_color", new Vector3(Settings.BladeColor.R, Settings.BladeColor.G, Settings.BladeColor.B));
        BindTerrain();
        var mesh = new RibbonTrailMesh { Shape = RibbonTrailMesh.ShapeEnum.Flat, SectionLength = 0.18f, SectionSegments = 1 };
        int fps = Mathf.Clamp(Settings.SimulationFps, 10, 60);
        if (Settings.Quality == VegetationConfig.GrassQuality.Low) fps = Mathf.Min(fps, 20);
        for (int x = -_half; x <= _half; x++)
            for (int z = -_half; z <= _half; z++)
            {
                var tile = new GpuParticles3D { Name = $"GrassTile{_tiles.Count}", Amount = rows * rows,
                    Lifetime = 600, Explosiveness = 1, FixedFps = fps, Preprocess = 1.0 / fps,
                    ProcessMaterial = _placement, DrawPass1 = mesh, MaterialOverride = _ribbon,
                    UseFixedSeed = true, Seed = Settings.Seed, LocalCoords = false,
                    CastShadow = Settings.CastShadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
                    Emitting = false };
                AddChild(tile); _tiles.Add(tile); _coordinates.Add(new Vector2I(x, z));
            }
        AllocatedParticles = rows * rows * _tiles.Count;
        UpdateBounds(); OnCameraMoved(_camera.GlobalPosition);
    }

    // =========================================================
    // Player transform events do nothing inside one tile. Only entering rows/columns restart on crossing.
    private void OnCameraMoved(Vector3 position)
    {
        if (_tiles.Count == 0) return;
        var center = new Vector2I(Mathf.FloorToInt(position.X / _tileWidth), Mathf.FloorToInt(position.Z / _tileWidth));
        if (center == _center) return;
        bool first = _center.X == int.MinValue;
        _center = center;
        for (int i = 0; i < _tiles.Count; i++)
        {
            Vector2I old = _coordinates[i];
            var next = first ? old + center : new Vector2I(center.X + Mathf.PosMod(old.X - center.X + _half, _width) - _half,
                center.Y + Mathf.PosMod(old.Y - center.Y + _half, _width) - _half);
            if (!first && next == old) continue;
            _coordinates[i] = next;
            _tiles[i].GlobalPosition = new Vector3((next.X + 0.5f) * _tileWidth, 0, (next.Y + 0.5f) * _tileWidth);
            _tiles[i].Emitting = true; _tiles[i].Restart(true); TileRestarts++;
        }
    }

    // =========================================================
    // Coalesce related native map signals into one deferred terrain refresh, without idle polling.
    private void QueueTerrainRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true; CallDeferred(nameof(RefreshTerrain));
    }

    // =========================================================
    // Refresh map RIDs after authoring/region changes, then replace particle placements on the GPU.
    public void RefreshTerrain()
    {
        _refreshQueued = false;
        if (_tiles.Count == 0) return;
        BindTerrain(); UpdateBounds();
        foreach (var tile in _tiles) { tile.Restart(true); TileRestarts++; }
    }

    // =========================================================
    // Upload static terrain data once per change rather than at physics-frame frequency.
    private void BindTerrain()
    {
        _placement.SetShaderParameter("_background_mode", _terrain.Get("material").AsGodotObject().Get("world_background"));
        float spacing = _terrain.Get("vertex_spacing").AsSingle();
        float size = _terrain.Get("region_size").AsSingle();
        _placement.SetShaderParameter("_vertex_spacing", spacing); _placement.SetShaderParameter("_vertex_density", 1f / spacing);
        _placement.SetShaderParameter("_region_size", size); _placement.SetShaderParameter("_region_texel_size", 1f / size);
        _placement.SetShaderParameter("_region_map_size", 32);
        _placement.SetShaderParameter("_region_map", _data.Call("get_region_map"));
        _placement.SetShaderParameter("_region_locations", _data.Call("get_region_locations"));
        _placement.SetShaderParameter("_height_maps", _data.Call("get_height_maps_rid"));
        _placement.SetShaderParameter("_control_maps", _data.Call("get_control_maps_rid"));
        _placement.SetShaderParameter("_color_maps", _data.Call("get_color_maps_rid"));
        TerrainBindingUpdates++;
    }

    // =========================================================
    // Correct world-height bounds plus blade/wind margin; no per-frame AABB writes.
    private void UpdateBounds()
    {
        Vector2 range = _data.Call("get_height_range").AsVector2();
        float maximumBlade = Mathf.Clamp(Settings.Height, 0.1f, 3) * (1 + Mathf.Clamp(Settings.ClumpHeightBoost, 0, 4));
        float margin = Mathf.Max(4, maximumBlade * 2 + 2);
        foreach (var tile in _tiles)
            tile.CustomAabb = new Aabb(new Vector3(-_tileWidth * 0.5f - margin, range.X - margin, -_tileWidth * 0.5f - margin),
                new Vector3(_tileWidth + margin * 2, range.Y - range.X + margin * 2, _tileWidth + margin * 2));
    }

    // =========================================================
    // Disconnect subscriptions on world unload; children and their GPU resources are freed by Godot.
    public override void _ExitTree()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= OnCameraMoved;
        if (IsInstanceValid(_data))
            foreach (string signal in _terrainSignals) _data.Disconnect(signal, Callable.From(QueueTerrainRefresh));
    }
}
