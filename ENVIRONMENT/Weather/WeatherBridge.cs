// C# owner for addon weather preview; dormant when clear, with camera-local rain/snow and cave shelter.
using Godot;

public partial class WeatherBridge : Node
{
    private Node3D _effects = null!;
    private ExplorationController _explorer = null!;
    private Camera3D _camera = null!;
    private WildernessCave _cave = null!;
    private EnvironmentConfig _settings = null!;
    private bool _sheltered, _initialized;

    // =========================================================
    // Instantiate the existing addon components with project-owned configuration and environment ownership.
    public void Initialize(ExplorationController explorer, EnvironmentConfig settings, WildernessCave cave)
    {
        _camera = explorer.GetNode<Camera3D>("Camera3D");
        _explorer = explorer; _settings = settings; _cave = cave;
        _effects = GD.Load<PackedScene>("res://ENVIRONMENT/Weather/TestWeather.tscn").Instantiate<Node3D>();
        _effects.Set("camera", _camera);
        _effects.Set("effect_target", _camera);
        AddChild(_effects); _initialized = true;
        _explorer.ViewPositionChanged += OnMoved;
        ApplySettings(); SetProcess(false); SetPhysicsProcess(false);
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
