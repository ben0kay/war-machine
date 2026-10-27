// Shared terrain-following ground wisps: cached local height texture, GPU animation and movement-only updates.
using Godot;
using System.Collections.Generic;

public partial class GroundHaze : Node3D
{
    [Export] public Shader WispsShader { get; set; } = null!;
    private EnvironmentConfig _settings = null!;
    private Godot.Environment _environment = null!;
    private GodotObject _data = null!;
    private Node3D _anchor = null!;
    private ExplorationController? _explorer;
    private PlayerController? _player;
    private Camera3D _camera = null!;
    private WildernessCave? _cave;
    private FogVolume? _volume;
    private ShaderMaterial _material = null!;
    private Image? _heights;
    private ImageTexture? _heightTexture;
    private Dictionary<Vector2I, float> _cache = new(), _nextCache = new();
    private Vector2I _tile = new(int.MinValue, int.MinValue);
    private Vector3 _lastPosition = new(float.PositiveInfinity, 0, 0);
    private bool _initialized, _active, _ownsFog, _oldEnabled;
    private float _oldDensity, _oldLength, _radius;
    private const float TileWidth = 8, SampleSpacing = 4;
    private int _halfSamples;

    // =========================================================
    // Retain the wilderness API and its cave/flight suppression.
    public void Initialize(Node3D terrain, ExplorationController explorer, EnvironmentConfig settings, Node sky, WildernessCave cave)
    {
        Disconnect();
        _explorer = explorer; _cave = cave;
        Setup(terrain, explorer, explorer.GetNode<Camera3D>("Camera3D"), settings,
            (Godot.Environment)sky.GetNode("SkyDome").Get("environment").AsGodotObject());
        explorer.ViewPositionChanged += OnMoved; explorer.CameraModeChanged += OnCameraMode;
        ApplySettings();
    }

    // =========================================================
    // Active world supplies its existing Sky3D environment and player; no new WorldEnvironment.
    public void Initialize(Node3D terrain, PlayerController player, Camera3D camera, EnvironmentConfig settings, Godot.Environment environment)
    {
        Disconnect(); _player = player;
        Setup(terrain, player, camera, settings, environment);
        player.PositionChanged += OnMoved; ApplySettings();
    }

    // =========================================================
    private void Setup(Node3D terrain, Node3D anchor, Camera3D camera, EnvironmentConfig settings, Godot.Environment environment)
    {
        _data = terrain.Get("data").AsGodotObject(); _anchor = anchor; _camera = camera;
        _settings = settings; _environment = environment; _initialized = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Explicit UI/config changes only. OFF retains cached resources but hides all rendering and releases the fog pass.
    public void ApplySettings()
    {
        if (!_initialized) return;
        _active = _settings.GroundHazeEnabled && _settings.GroundHazeDensity > 0;
        if (!_active) { if (_volume != null) _volume.Hide(); RestoreFog(); return; }
        if (ProjectSettings.GetSetting("rendering/renderer/rendering_method", "forward_plus").AsString() != "forward_plus")
        { _active = false; GD.PushWarning("Ground haze requires Forward+."); if (_volume != null) _volume.Hide(); RestoreFog(); return; }
        if (_volume == null)
        {
            _material = new ShaderMaterial { Shader = WispsShader ?? GD.Load<Shader>("res://ENVIRONMENT/Weather/Shaders/GroundWisps.gdshader") };
            _volume = new FogVolume { Name = "TerrainWisps", Shape = RenderingServer.FogVolumeShape.Box, Material = _material, Visible = false };
            AddChild(_volume);
        }
        _radius = Mathf.Clamp(_settings.GroundHazeCoverageRadius, 6, 48);
        int half = Mathf.CeilToInt((_radius + TileWidth) / SampleSpacing);
        if (_heights == null || half != _halfSamples)
        {
            _halfSamples = half; int size = half * 2 + 1;
            _heights = Image.CreateEmpty(size, size, false, Image.Format.Rgf);
            _heightTexture = ImageTexture.CreateFromImage(_heights);
            _material.SetShaderParameter("terrain_heights", _heightTexture);
            _cache.Clear(); _nextCache.Clear();
        }
        _material.SetShaderParameter("density", Mathf.Clamp(_settings.GroundHazeDensity, 0, 0.5f));
        _material.SetShaderParameter("layer_height", Mathf.Clamp(_settings.GroundHazeHeight, 0.25f, 8));
        _material.SetShaderParameter("noise_scale", Mathf.Clamp(_settings.GroundHazeNoiseScale, 0.05f, 1));
        _material.SetShaderParameter("drift_speed", Mathf.Clamp(_settings.GroundHazeDriftSpeed, 0, 2));
        _material.SetShaderParameter("contrast", Mathf.Clamp(_settings.GroundHazeContrast, 0, 1));
        _material.SetShaderParameter("turbulence", Mathf.Clamp(_settings.GroundHazeTurbulence, 0, 1));
        _material.SetShaderParameter("fog_color", _settings.GroundHazeColor);
        _material.SetShaderParameter("coverage_radius", _radius);
        Vector2 wind = _settings.WindDirection.LengthSquared() > 0.001f ? _settings.WindDirection.Normalized() : Vector2.Right;
        _material.SetShaderParameter("wind_direction", wind);
        _tile = new Vector2I(int.MinValue, int.MinValue);
        _lastPosition = new Vector3(float.PositiveInfinity, 0, 0);
        OnMoved(_anchor.GlobalPosition);
    }

    // =========================================================
    private void OnCameraMode(bool firstPerson) => RefreshView();

    // =========================================================
    // Called explicitly on overhead framing changes; an elevated overhead camera must not suppress ground fog.
    public void RefreshView()
    {
        if (!_initialized || !_active) return;
        _lastPosition = new Vector3(float.PositiveInfinity, 0, 0); OnMoved(_anchor.GlobalPosition);
    }

    // =========================================================
    // No frame callback: movement threshold for coverage, eight-metre crossings for terrain texture refresh.
    private void OnMoved(Vector3 position)
    {
        if (!_active || _volume == null || position.DistanceSquaredTo(_lastPosition) < 2.25f) return;
        _lastPosition = position;
        bool visible = true;
        if (_explorer != null)
        {
            float ground = _data.Call("get_height", position).AsSingle();
            float altitude = _camera.GlobalPosition.Y - ground;
            visible = float.IsFinite(ground) && altitude > -0.5f && altitude < 35 && !(_cave?.IsSheltered(_camera.GlobalPosition) ?? false);
        }
        if (!visible) { _volume.Hide(); RestoreFog(); return; }
        var tile = new Vector2I(Mathf.FloorToInt(position.X / TileWidth), Mathf.FloorToInt(position.Z / TileWidth));
        if (tile != _tile) { _tile = tile; UpdateTerrain(tile); }
        _material.SetShaderParameter("coverage_center", new Vector2(position.X, position.Z));
        if (_cache.Count == 0) { _volume.Hide(); RestoreFog(); return; }
        if (!_volume.Visible) _volume.Show(); EnableFog();
    }

    // =========================================================
    // Reuse overlap samples and the image/texture. Only entering strips query Terrain3D; no actor/scene scans.
    private void UpdateTerrain(Vector2I tile)
    {
        int size = _halfSamples * 2 + 1;
        Vector2I center = tile * 2;
        Vector2I origin = center - new Vector2I(_halfSamples, _halfSamples);
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        _nextCache.Clear();
        for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
        {
            Vector2I key = origin + new Vector2I(x, z);
            if (!_cache.TryGetValue(key, out float height))
                height = _data.Call("get_height", new Vector3(key.X * SampleSpacing, 0, key.Y * SampleSpacing)).AsSingle();
            _nextCache[key] = height;
            bool valid = float.IsFinite(height);
            _heights!.SetPixel(x, z, new Color(valid ? height : 0, valid ? 1 : 0, 0));
            if (valid) { low = Mathf.Min(low, height); high = Mathf.Max(high, height); }
        }
        var previous = _cache; _cache = _nextCache; _nextCache = previous;
        _heightTexture!.Update(_heights!);
        _material.SetShaderParameter("terrain_origin", new Vector2(origin.X * SampleSpacing, origin.Y * SampleSpacing));
        _material.SetShaderParameter("terrain_span", (size - 1) * SampleSpacing);
        _material.SetShaderParameter("terrain_texels", (float)size);
        if (!float.IsFinite(low)) { _volume!.Hide(); _cache.Clear(); return; }
        float top = high + Mathf.Clamp(_settings.GroundHazeHeight, 0.25f, 8) * 1.5f;
        float span = (size - 1) * SampleSpacing;
        _volume!.Size = new Vector3(span, top - low + 0.4f, span);
        _volume.GlobalPosition = new Vector3(center.X * SampleSpacing, (low - 0.2f + top + 0.2f) * 0.5f, center.Y * SampleSpacing);
    }

    // =========================================================
    // Preserve an existing volumetric baseline. Only a previously disabled pass gets zero global density.
    private void EnableFog()
    {
        if (!_ownsFog)
        {
            _oldEnabled = _environment.VolumetricFogEnabled; _oldDensity = _environment.VolumetricFogDensity;
            _oldLength = _environment.VolumetricFogLength; _ownsFog = true;
            if (!_oldEnabled) _environment.VolumetricFogDensity = 0;
            _environment.VolumetricFogEnabled = true;
        }
        // Overhead zoom can settle after its input event; reserve the supported 100m camera distance up front.
        float length = Mathf.Max(_oldLength, (_player != null ? 100 : _camera.GlobalPosition.DistanceTo(_anchor.GlobalPosition)) + _radius + 12);
        if (!Mathf.IsEqualApprox(_environment.VolumetricFogLength, length)) _environment.VolumetricFogLength = length;
    }

    // =========================================================
    // Shared ray controls own the baseline; haze keeps its local pass when rays are switched off.
    public void SetFogBaseline(bool enabled, float density, float length)
    {
        if (!_initialized) return;
        if (_ownsFog)
        { _oldEnabled = enabled; _oldDensity = density; _oldLength = length; }
        _environment.VolumetricFogEnabled = enabled || _ownsFog;
        _environment.VolumetricFogDensity = density;
        _environment.VolumetricFogLength = length;
        if (_ownsFog) EnableFog();
    }

    // =========================================================
    private void RestoreFog()
    {
        if (!_ownsFog || !IsInstanceValid(_environment)) return;
        _environment.VolumetricFogEnabled = _oldEnabled; _environment.VolumetricFogDensity = _oldDensity;
        _environment.VolumetricFogLength = _oldLength; _ownsFog = false;
    }

    // =========================================================
    private void Disconnect()
    {
        if (IsInstanceValid(_explorer)) { _explorer!.ViewPositionChanged -= OnMoved; _explorer.CameraModeChanged -= OnCameraMode; }
        if (IsInstanceValid(_player)) _player!.PositionChanged -= OnMoved;
        RestoreFog(); _active = false; _initialized = false; _explorer = null; _player = null; _cave = null;
        _cache.Clear(); _nextCache.Clear(); _tile = new Vector2I(int.MinValue, int.MinValue);
    }

    // =========================================================
    public override void _ExitTree() => Disconnect();
}
