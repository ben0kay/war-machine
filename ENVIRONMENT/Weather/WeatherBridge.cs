// C# owner for addon weather preview; dormant when clear, with camera-local rain/snow and cave shelter.
using Godot;

public partial class WeatherBridge : Node
{
    [Export] public PackedScene EffectsScene { get; set; } = null!;
    private Node3D _effects = null!;
    private ExplorationController _explorer = null!;
    private Camera3D _camera = null!;
    private WildernessCave _cave = null!;
    private EnvironmentConfig _settings = null!;
    private bool _sheltered, _initialized;
    private float _coverage;

    // =========================================================
    // Instantiate the existing addon components with project-owned configuration and environment ownership.
    public void Initialize(ExplorationController explorer, EnvironmentConfig settings, WildernessCave cave)
    {
        _camera = explorer.GetNode<Camera3D>("Camera3D");
        _explorer = explorer; _cave = cave;
        InitializeView(_camera, _camera, settings);
        _explorer.ViewPositionChanged += OnMoved;
    }

    // =========================================================
    // Camera-independent anchor also supports orthographic previews without an explorer or cave dependency.
    public void InitializeView(Camera3D camera, Node3D anchor, EnvironmentConfig settings)
    {
        _camera = camera; _settings = settings;
        _effects = (EffectsScene ?? GD.Load<PackedScene>("res://ENVIRONMENT/Weather/TestWeather.tscn")).Instantiate<Node3D>();
        _effects.Set("camera", _camera);
        _effects.Set("effect_target", anchor);
        AddChild(_effects); _initialized = true;
        ApplySettings(); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Explicit zoom changes widen bounded emitters around the player, rather than raining at the elevated camera.
    public void SetOrthographicCoverage(float viewSize)
    {
        if (!_initialized) return;
        float aspect = _camera.GetViewport().GetVisibleRect().Size.Aspect();
        float depth = 1f / Mathf.Max(0.3f, Mathf.Abs(_camera.GlobalBasis.Z.Y));
        float radius = Mathf.Clamp(viewSize * 0.6f * Mathf.Sqrt(aspect * aspect + depth * depth), 20, 160);
        if (Mathf.IsEqualApprox(radius, _coverage)) return;
        _coverage = radius;
        foreach (var child in _effects.GetNode("Precipitation").GetChildren())
        {
            if (child is not GpuParticles3D emitter || emitter.ProcessMaterial is not ParticleProcessMaterial material) continue;
            float extent = emitter.Name.ToString().EndsWith("Near") ? radius * 0.65f : radius;
            material.EmissionBoxExtents = new Vector3(extent, material.EmissionBoxExtents.Y, extent);
            emitter.VisibilityAabb = new Aabb(new Vector3(-radius - 30, -65, -radius - 30), new Vector3(radius * 2 + 60, 90, radius * 2 + 60));
        }
    }

    // =========================================================
    // UI events update one state; no duplicated fog/sun controller or per-frame material writes from this bridge.
    public void ApplySettings()
    {
        if (!_initialized) return;
        _effects.ProcessMode = ProcessModeEnum.Inherit;
        bool outdoors = !_sheltered && _settings.WeatherEnabled;
        Vector2 direction = _settings.WindDirection.Normalized();
        var state = new Godot.Collections.Dictionary
        {
            ["rain_intensity"] = outdoors ? _settings.RainIntensity : 0f,
            ["snow_intensity"] = outdoors ? _settings.SnowIntensity : 0f,
            ["mist_intensity"] = outdoors ? _settings.LocalMistIntensity : 0f,
            ["airborne_intensity"] = outdoors ? _settings.AirborneIntensity : 0f,
            ["airborne_type"] = 2, // Pollen-like drifting motes; optional and off by default.
            ["wind_direction"] = new Vector3(direction.X, 0, direction.Y),
            ["wind_speed"] = _settings.GrassWindStrength * 6f * _settings.GrassWindSpeed / 0.025f,
            ["gust_strength"] = _settings.WeatherGusts,
            ["gust_frequency"] = 0.18f,
            ["turbulence"] = _settings.WeatherTurbulence,
            ["particle_size_scale"] = _settings.WeatherParticleSize,
            ["fall_speed_scale"] = _settings.WeatherFallSpeed
        };
        _effects.Call("set_quality", Mathf.Clamp(Mathf.RoundToInt(_settings.WeatherQuality), 0, 2));
        _effects.Call("apply_preview", state);
        bool precipitation = outdoors && (_settings.RainIntensity > 0.001f || _settings.SnowIntensity > 0.001f);
        bool atmosphere = outdoors && (_settings.LocalMistIntensity > 0.001f || _settings.AirborneIntensity > 0.001f);
        SetActive(_effects.GetNode<Node3D>("Precipitation"), precipitation);
        SetActive(_effects.GetNode<Node3D>("AtmosphereParticles"), atmosphere);
        if (!precipitation && !atmosphere) _effects.ProcessMode = ProcessModeEnum.Disabled;
    }

    // =========================================================
    // Zero means stopped GPU simulation and no component callbacks; reactivation directly wakes emitters.
    private static void SetActive(Node3D node, bool active)
    {
        node.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        node.SetProcess(active);
        foreach (var child in node.GetChildren())
            if (child is GpuParticles3D emitter)
            {
                bool emitting = active && emitter.AmountRatio > 0.001f;
                emitter.ProcessMode = emitting ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
                emitter.Visible = emitting;
            }
    }

    // =========================================================
    // Shelter is tested only on actual movement; state is reapplied only when crossing the cave boundary.
    private void OnMoved(Vector3 position)
    {
        bool sheltered = _cave.IsSheltered(_camera.GlobalPosition);
        if (sheltered == _sheltered) return;
        _sheltered = sheltered; ApplySettings();
    }

    // =========================================================
    public override void _ExitTree()
    { if (_initialized && IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= OnMoved; }
}
