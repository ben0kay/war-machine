// Event-driven bridge into installed Sky3D 2.1; never computes a competing sun or clock.
using Godot;

public partial class SkyController : Node
{
    [Export] public EnvironmentConfig Settings { get; set; } = null!;
    [Export] public Node Sky { get; set; } = null!;
    private bool _paused, _preview;
    private float _lastDaylight = -1;
    private Node _clock = null!;
    private DirectionalLight3D _sun = null!;
    private Callable _timeChanged, _refreshAtmosphere;
    public event System.Action? AtmosphereSettingsApplied;
    // One event bridge for shared consumers of the authoritative Sky3D in-game clock.
    public event System.Action<float>? TimeChanged;
    public float CurrentHour => Sky.Get("current_time").AsSingle();

    // =========================================================
    // Apply configuration once after Sky3D and its timer have entered the tree.
    public override void _Ready()
    {
        Sky.Set("current_time", Settings.StartHour);
        Sky.Set("minutes_per_day", Mathf.Max(1, Settings.DayLengthMinutes));
        Sky.Set("update_interval", Mathf.Max(0.016f, Settings.UpdateInterval));
        Sky.Set("editor_time_enabled", false);
        _clock = Sky.GetNode("TimeOfDay"); _sun = Sky.GetNode<DirectionalLight3D>("SunLight");
        _refreshAtmosphere = Callable.From(UpdateAtmosphere);
        // Sky3D emits time_changed before updating celestial transforms; defer until that setter finishes.
        _timeChanged = Callable.From<float>(hour => { _refreshAtmosphere.CallDeferred(); TimeChanged?.Invoke(hour); });
        _clock.Connect("time_changed", _timeChanged);
        ApplyMist(); ApplyAtmosphere();
        SetPaused(Settings.StartPaused);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Set the existing SkyDome fog once; explicit calls can reapply edited config without polling.
    public void ApplyMist()
    {
        var dome = Sky.GetNode<Node>("SkyDome");
        dome.Set("fog_visible", Settings.FogEnabled);
        dome.Set("fog_density", Mathf.Max(0, Settings.FogDensity));
        float start = Mathf.Max(0, Settings.FogStart);
        dome.Set("fog_start", start);
        dome.Set("fog_end", Mathf.Max(start + 1, Settings.FogEnd));
        dome.Set("fog_falloff", Mathf.Max(0, Settings.FogFalloff));
    }

    // =========================================================
    // Direct Sky3D setters immediately update its existing sun and timer.
    public void ApplyTime() { Sky.Set("current_time", Mathf.PosMod(Settings.StartHour, 24)); UpdateAtmosphere(); }

    // =========================================================
    // Explicit controls refresh lighting and the shared volumetric baseline, even with solar time paused.
    public void ApplyAtmosphere()
    {
        Sky.GetNode("SkyDome").Set("moon_light_energy", Mathf.Max(0, Settings.MoonlightEnergy));
        _lastDaylight = -1; UpdateAtmosphere(); AtmosphereSettingsApplied?.Invoke();
    }

    // =========================================================
    // Use Sky3D's actual solar elevation (including its date/location), rather than another clock calculation.
    private void UpdateAtmosphere()
    {
        if (!IsInsideTree() || !IsInstanceValid(_sun)) return;
        float daylight = Mathf.SmoothStep(0, 1, Mathf.Clamp((_sun.Position.Y + 0.18f) / 0.43f, 0, 1));
        bool changed = Mathf.Abs(daylight - _lastDaylight) >= 0.0005f;
        var environment = ((WorldEnvironment)Sky).Environment;
        Color ambient = Settings.NightAmbientColor.Lerp(Settings.DayAmbientColor, daylight);
        float energy = Mathf.Lerp(Mathf.Max(0, Settings.NightAmbientEnergy), Mathf.Max(0, Settings.AmbientLightEnergy), daylight);
        if (environment.AmbientLightColor != ambient) environment.AmbientLightColor = ambient;
        if (!Mathf.IsEqualApprox(environment.AmbientLightEnergy, energy)) environment.AmbientLightEnergy = energy;
        if (changed) { _lastDaylight = daylight; Settings.SetDaylight(daylight); }
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_clock) && _clock.IsConnected("time_changed", _timeChanged))
            _clock.Disconnect("time_changed", _timeChanged);
    }
    // =========================================================
    public void ApplyCycle()
    {
        _preview = false;
        Sky.Set("minutes_per_day", Mathf.Max(1, Settings.DayLengthMinutes));
        SetPaused(Settings.StartPaused);
    }

    // =========================================================
    // P pauses only solar time; F8 previews a full day in one configured minute.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.P) SetPaused(!_paused);
        if (key.Keycode == Key.F8)
        {
            _preview = !_preview;
            Sky.Set("minutes_per_day", Mathf.Max(0.1f, _preview ? Settings.PreviewDayMinutes : Settings.DayLengthMinutes));
            GD.Print($"Accelerated sky preview: {_preview}");
        }
    }

    // =========================================================
    // Sky3D stops its timer while paused; cloud and grass shader motion remain intentional.
    public void SetPaused(bool paused)
    {
        _paused = paused; Settings.StartPaused = paused; Sky.Set("game_time_enabled", !paused);
        GD.Print($"Solar time paused: {paused}");
    }
}
