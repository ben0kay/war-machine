// One event-driven scatterer per category, with shared mesh variants and small Terrain3D-grounded batches.
using Godot;
using System.Collections.Generic;

public partial class ContentScatter : Node3D
{
    [Export] public ContentScatterSettings Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private WorldConfig _world = null!;
    private readonly Dictionary<Vector2I, List<MultiMeshInstance3D>> _chunks = new();
    private Mesh[] _meshes = System.Array.Empty<Mesh>();
    private FastNoiseLite _patch = null!;
    private ShaderMaterial? _wind;
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _chunkSize;
    private bool _initialized, _queued;

    // =========================================================
    // Saved terrain is read-only. Movement and map signals wake placement, never per-frame polling.
    public void Initialize(Node3D terrain, ExplorationController explorer, WorldConfig world)
    {
        _data = terrain.Get("data").AsGodotObject(); _explorer = explorer; _world = world;
        _explorer.ViewPositionChanged += Move;
        _data.Connect("height_maps_changed", Callable.From(QueueRefresh)); _initialized = true;
        SetProcess(false); SetPhysicsProcess(false); ApplySettings();
    }

    // =========================================================
    // Explicit rebuild/disable owns all transient batches; imported art bypasses generation.
    public void ApplySettings()
    {
        foreach (var nodes in _chunks.Values) foreach (var node in nodes) { RemoveChild(node); node.QueueFree(); }
        _chunks.Clear(); _meshes = System.Array.Empty<Mesh>(); _wind = null;
        _center = new Vector2I(int.MinValue, int.MinValue);
        if (!_initialized || !Settings.Enabled || Settings.Density <= 0) return;
        _chunkSize = Mathf.Clamp(Settings.ChunkSize, 16, 64);
        _patch = new FastNoiseLite { Seed = Settings.Seed, Frequency = Mathf.Max(0.001f, Settings.PatchFrequency) };
        var meshes = new List<Mesh>();
        foreach (var imported in Settings.ImportedMeshes) if (imported != null) meshes.Add(imported);
        if (meshes.Count == 0)
        {
            bool plant = Settings.Kind == ContentScatterSettings.ContentKind.Flowers || Settings.Kind == ContentScatterSettings.ContentKind.Bushes;
            Material material;
            if (plant)
            {
                _wind = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Content/Vegetation/Trees/Shaders/LeafWind.gdshader") };
                material = _wind; ApplyWind();
            }
            else material = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
            for (int i = 0; i < 3; i++)
            {
                var random = new RandomNumberGenerator { Seed = Seed(Settings.Seed, i, 41) };
                meshes.Add(Settings.Kind switch {
                    ContentScatterSettings.ContentKind.Flowers => PlantGenerator.Create(Settings, random, material),
                    ContentScatterSettings.ContentKind.Bushes => BushGenerator.Create(Settings, random, material),
                    ContentScatterSettings.ContentKind.FallenLogs => FallenLogGenerator.Create(Settings, random, material),
                    _ => RockGenerator.Create(Settings, random, material)
                });
            }
        }
        _meshes = meshes.ToArray(); Move(_explorer.GlobalPosition);
    }

    // =========================================================
    // Shared GPU wind follows EnvironmentMaster sliders without touching geometry or placement.
    public void ApplyWind()
    {
        if (_wind == null) return;
        _wind.SetShaderParameter("wind_direction", EnvironmentSettings.WindDirection.Normalized());
        _wind.SetShaderParameter("wind_strength", Settings.WindStrength * EnvironmentSettings.GrassWindStrength);
        _wind.SetShaderParameter("wind_speed", EnvironmentSettings.GrassWindSpeed * 48f);
        _wind.SetShaderParameter("flutter_strength", 0.08f);
    }

    // =========================================================
    // Chunk crossing creates entering cells and evicts distant ones; native culling handles rendering.
    private void Move(Vector3 position)
    {
        if (_meshes.Length == 0) return;
        var center = new Vector2I(Mathf.FloorToInt(position.X / _chunkSize), Mathf.FloorToInt(position.Z / _chunkSize));
        if (center == _center) return; _center = center;
        int radius = Mathf.CeilToInt(Mathf.Clamp(Settings.RenderDistance, 16, 256) / _chunkSize) + 1;
        var remove = new List<Vector2I>();
        foreach (var key in _chunks.Keys)
            if (Mathf.Abs(key.X - center.X) > radius + 1 || Mathf.Abs(key.Y - center.Y) > radius + 1) remove.Add(key);
        foreach (var key in remove)
        { foreach (var node in _chunks[key]) { RemoveChild(node); node.QueueFree(); } _chunks.Remove(key); }
        int count = Mathf.CeilToInt((float)_world.Size / _chunkSize);
        for (int x = Mathf.Max(0, center.X - radius); x <= Mathf.Min(count - 1, center.X + radius); x++)
            for (int z = Mathf.Max(0, center.Y - radius); z <= Mathf.Min(count - 1, center.Y + radius); z++)
            { var key = new Vector2I(x, z); if (!_chunks.ContainsKey(key)) CreateChunk(key); }
    }

    // =========================================================
    // Candidate count is bounded; slope-aware placement follows the actual saved height map.
    private void CreateChunk(Vector2I key)
    {
        var bins = new List<Transform3D>[_meshes.Length]; for (int i = 0; i < bins.Length; i++) bins[i] = new();
        var random = new RandomNumberGenerator { Seed = Seed(Settings.Seed, key.X, key.Y) };
        int cells = Mathf.Clamp(Mathf.CeilToInt(_chunkSize / Mathf.Max(2, Settings.Spacing)), 1, 16);
        float step = (float)_chunkSize / cells;
        var anchor = new Vector3((key.X + 0.5f) * _chunkSize, 0, (key.Y + 0.5f) * _chunkSize);
        float anchorHeight = Height(anchor); if (!float.IsFinite(anchorHeight)) { _chunks.Add(key, new()); return; }
        anchor.Y = anchorHeight;
        for (int x = 0; x < cells; x++) for (int z = 0; z < cells; z++)
        {
            if (random.Randf() > Mathf.Clamp(Settings.Density, 0, 1)) continue;
            var pos = new Vector3((key.X * _chunkSize) + (x + random.RandfRange(0.15f, 0.85f)) * step, 0,
                key.Y * _chunkSize + (z + random.RandfRange(0.15f, 0.85f)) * step);
            if (pos.X >= _world.Size - 2 || pos.Z >= _world.Size - 2 || _patch.GetNoise2D(pos.X, pos.Z) < Settings.PatchThreshold) continue;
            float h = Height(pos), hx = Height(pos + Vector3.Right), hz = Height(pos + Vector3.Back);
            if (!float.IsFinite(h) || !float.IsFinite(hx) || !float.IsFinite(hz)) continue;
            var normal = new Vector3(h - hx, 1, h - hz).Normalized();
            if (new Vector2(hx - h, hz - h).Length() > Settings.MaximumSlope) continue;
            float scale = random.RandfRange(Mathf.Max(0.1f, Mathf.Min(Settings.MinimumScale, Settings.MaximumScale)), Mathf.Max(0.1f, Mathf.Max(Settings.MinimumScale, Settings.MaximumScale)));
            float yaw = random.RandfRange(0, Mathf.Tau);
            var forward = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));
            forward = (forward - normal * forward.Dot(normal)).Normalized();
            // Plants remain upright; rocks and logs align to the surface normal.
            bool grounded = Settings.Kind == ContentScatterSettings.ContentKind.Rocks || Settings.Kind == ContentScatterSettings.ContentKind.FallenLogs;
            var basis = grounded ? new Basis(normal.Cross(forward).Normalized(), normal, forward) : new Basis(Vector3.Up, yaw);
            pos.Y = h - 0.03f;
            bins[random.RandiRange(0, bins.Length - 1)].Add(new Transform3D(basis.Scaled(Vector3.One * scale), pos - anchor));
        }
        var nodes = new List<MultiMeshInstance3D>();
        for (int i = 0; i < bins.Length; i++)
        {
            if (bins[i].Count == 0) continue;
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = _meshes[i], InstanceCount = bins[i].Count };
            for (int j = 0; j < bins[i].Count; j++) multi.SetInstanceTransform(j, bins[i][j]);
            var node = new MultiMeshInstance3D { Position = anchor, Multimesh = multi, ExtraCullMargin = 1,
                VisibilityRangeEnd = Mathf.Clamp(Settings.RenderDistance, 16, 256), VisibilityRangeEndMargin = 4,
                CastShadow = Settings.CastShadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off };
            AddChild(node); nodes.Add(node);
        }
        _chunks.Add(key, nodes);
    }

    // =========================================================
    private float Height(Vector3 position) => _data.Call("get_height", position).AsSingle();
    // =========================================================
    private static ulong Seed(int seed, int x, int z) => unchecked((uint)seed ^ (uint)x * 73856093u ^ (uint)z * 19349663u);
    // =========================================================
    private void QueueRefresh() { if (_queued) return; _queued = true; CallDeferred(nameof(RefreshTerrain)); }
    // =========================================================
    public void RefreshTerrain() { _queued = false; if (_initialized) ApplySettings(); }
    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= Move;
        if (IsInstanceValid(_data)) _data.Disconnect("height_maps_changed", Callable.From(QueueRefresh));
    }
}
