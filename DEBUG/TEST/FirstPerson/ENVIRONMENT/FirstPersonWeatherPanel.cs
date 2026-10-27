// FPS-only Sky3D/weather controls. Live edits reuse existing runtime environment and owners.
// UI callbacks + Sky3D time_changed drive updates; no idle frame processing or second simulation.
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class FirstPersonWeatherPanel : Node
{
    private SkyController _sky = null!;
    private WeatherBridge _weather = null!;
    private TerrainGrass _grass = null!;
    private PinePlacement _pines = null!;
    private EnvironmentConfig _settings = null!;
    private Node _clock = null!;
    private Callable _timeChanged;
    private PanelContainer _panel = null!;
    private HSlider _hour = null!;
    private Label _hourLabel = null!;
    private Button _copy = null!;

    public bool IsOpen => IsInstanceValid(_panel) && _panel.Visible;

    // =========================================================
    // The FPS test uses the same already-duplicated world environment, never writes a master .tres.
    public void Initialize(SkyController sky, WeatherBridge weather, TerrainGrass grass,
        PinePlacement pines, CanvasLayer controls)
    {
        _sky = sky; _weather = weather; _grass = grass; _pines = pines; _settings = sky.Settings;
        SetProcess(false); SetPhysicsProcess(false);
        BuildPanel(controls);
        _clock = _sky.Sky.GetNode("TimeOfDay");
        _timeChanged = Callable.From<float>(OnTimeChanged);
        _clock.Connect("time_changed", _timeChanged);
        OnTimeChanged(_sky.Sky.Get("current_time").AsSingle());
    }

    // =========================================================
    public void SetPanelVisible(bool visible) { if (IsInstanceValid(_panel)) _panel.Visible = visible; }

    // =========================================================
    // FirstPersonWorld owns F8/F9 and mouse capture. This panel only exposes environment controls.
    private void BuildPanel(CanvasLayer controls)
    {
        _panel = new PanelContainer { Name = "FPSWeatherControls", Position = new Vector2(20, 80), Visible = false };
        controls.AddChild(_panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 10);
        _panel.AddChild(margin);
        var outer = new VBoxContainer { CustomMinimumSize = new Vector2(360, 0) }; margin.AddChild(outer);
        outer.AddChild(new Label { Text = "FPS weather + time of day · F8 to close" });
        var root = FirstPersonTuningSections.AddScroll(outer,
            Mathf.Clamp(GetViewport().GetVisibleRect().Size.Y - 220, 180, 580));
        var environmentRows = FirstPersonTuningSections.Add(root, "Environment");
        var rows = FirstPersonTuningSections.Add(environmentRows, "Day / night");
        _hourLabel = new Label(); rows.AddChild(_hourLabel);
        _hour = AddSlider(rows, 0, 23.99, 0.01);
        _hour.SetValueNoSignal(_settings.StartHour);
        _hour.ValueChanged += value => { _settings.StartHour = (float)value; _sky.ApplyTime(); OnTimeChanged((float)value); MarkChanged(); };
        AddBoundSlider(rows, "Day length (minutes)", 1, 1440, 1, _settings.DayLengthMinutes,
            value => { _settings.DayLengthMinutes = value; _sky.ApplyCycle(); });
        var pause = new CheckButton { Text = "Pause day / night", ButtonPressed = _settings.StartPaused,
            FocusMode = Control.FocusModeEnum.None };
        rows.AddChild(pause);
        pause.Toggled += value => { _sky.SetPaused(value); MarkChanged(); };

        rows = FirstPersonTuningSections.Add(environmentRows, "Weather");
        var enabled = new CheckButton { Text = "Enable weather", ButtonPressed = _settings.WeatherEnabled,
            FocusMode = Control.FocusModeEnum.None };
        rows.AddChild(enabled);
        enabled.Toggled += value => { _settings.WeatherEnabled = value; _weather.ApplySettings(); MarkChanged(); };
        var weatherRows = rows;
        rows = FirstPersonTuningSections.Add(weatherRows, "Rain / snow");
        AddBoundSlider(rows, "Rain intensity", 0, 1, 0.01, _settings.RainIntensity,
            value => { _settings.RainIntensity = value; _weather.ApplySettings(); });
        AddBoundSlider(rows, "Snow intensity", 0, 1, 0.01, _settings.SnowIntensity,
            value => { _settings.SnowIntensity = value; _weather.ApplySettings(); });
        rows = FirstPersonTuningSections.Add(weatherRows, "Mist / motes");
        AddBoundSlider(rows, "Local mist", 0, 1, 0.01, _settings.LocalMistIntensity,
            value => { _settings.LocalMistIntensity = value; _weather.ApplySettings(); });
        AddBoundSlider(rows, "Airborne motes", 0, 1, 0.01, _settings.AirborneIntensity,
            value => { _settings.AirborneIntensity = value; _weather.ApplySettings(); });
        rows = FirstPersonTuningSections.Add(environmentRows, "Wind");
        AddBoundSlider(rows, "Wind strength", 0, 2, 0.01, _settings.GrassWindStrength,
            value => { _settings.GrassWindStrength = value; ApplyWind(); });
        AddBoundSlider(rows, "Wind heading (degrees)", 0, 360, 1, _settings.WindHeading,
            value => { _settings.WindHeading = value; float angle = Mathf.DegToRad(value);
                _settings.WindDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); ApplyWind(); });
        AddBoundSlider(rows, "Gusts", 0, 1, 0.01, _settings.WeatherGusts,
            value => { _settings.WeatherGusts = value; _weather.ApplySettings(); });
        AddBoundSlider(rows, "Turbulence", 0, 2, 0.01, _settings.WeatherTurbulence,
            value => { _settings.WeatherTurbulence = value; _weather.ApplySettings(); });
        rows = FirstPersonTuningSections.Add(weatherRows, "Particle appearance / quality");
        AddBoundSlider(rows, "Particle size", 0.1, 3, 0.05, _settings.WeatherParticleSize,
            value => { _settings.WeatherParticleSize = value; _weather.ApplySettings(); });
        AddBoundSlider(rows, "Fall speed", 0.1, 3, 0.05, _settings.WeatherFallSpeed,
            value => { _settings.WeatherFallSpeed = value; _weather.ApplySettings(); });
        AddBoundSlider(rows, "Weather quality (0 / 1 / 2)", 0, 2, 1, _settings.WeatherQuality,
            value => { _settings.WeatherQuality = value; _weather.ApplySettings(); });

        _copy = new Button { Text = "Copy weather & time settings", FocusMode = Control.FocusModeEnum.None };
        outer.AddChild(_copy);
        _copy.Pressed += CopySettings;
    }

    // =========================================================
    // Existing weather, grass and pine owners all consume this shared wind state, on explicit changes only.
    private void ApplyWind()
    { _weather.ApplySettings(); _grass.ApplyAppearance(); _pines.ApplyWind(); }

    // =========================================================
    private static HSlider AddSlider(VBoxContainer rows, double min, double max, double step)
    {
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step,
            FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(300, 20), Scrollable = false };
        rows.AddChild(slider); return slider;
    }

    // =========================================================
    private void AddBoundSlider(VBoxContainer rows, string label, double min, double max, double step,
        float initial, Action<float> apply)
    {
        var name = new Label { Text = $"{label}: {initial:0.##}" }; rows.AddChild(name);
        var slider = AddSlider(rows, min, max, step); slider.SetValueNoSignal(initial);
        slider.ValueChanged += value => { name.Text = $"{label}: {value:0.##}"; apply((float)value); MarkChanged(); };
    }

    // =========================================================
    // The existing Sky3D clock emits the time; no new timer or per-frame label polling.
    private void OnTimeChanged(float hour)
    {
        _settings.StartHour = hour;
        _hour.SetValueNoSignal(hour);
        int minutes = Mathf.FloorToInt(hour * 60);
        _hourLabel.Text = $"Time: {minutes / 60:00}:{minutes % 60:00}";
    }

    private void MarkChanged() { if (IsInstanceValid(_copy)) _copy.Text = "Copy weather & time settings"; }

    // =========================================================
    // Copy runtime settings for later authoring; no persistent changes to top-down/FPS master configs.
    private void CopySettings()
    {
        var values = new Dictionary<string, object>
        {
            [nameof(_settings.StartHour)] = _sky.Sky.Get("current_time").AsSingle(),
            [nameof(_settings.StartPaused)] = _settings.StartPaused,
            [nameof(_settings.DayLengthMinutes)] = _settings.DayLengthMinutes,
            [nameof(_settings.WeatherEnabled)] = _settings.WeatherEnabled,
            [nameof(_settings.RainIntensity)] = _settings.RainIntensity,
            [nameof(_settings.SnowIntensity)] = _settings.SnowIntensity,
            [nameof(_settings.LocalMistIntensity)] = _settings.LocalMistIntensity,
            [nameof(_settings.AirborneIntensity)] = _settings.AirborneIntensity,
            [nameof(_settings.GrassWindStrength)] = _settings.GrassWindStrength,
            [nameof(_settings.WindHeading)] = _settings.WindHeading,
            [nameof(_settings.WeatherGusts)] = _settings.WeatherGusts,
            [nameof(_settings.WeatherTurbulence)] = _settings.WeatherTurbulence,
            [nameof(_settings.WeatherParticleSize)] = _settings.WeatherParticleSize,
            [nameof(_settings.WeatherFallSpeed)] = _settings.WeatherFallSpeed,
            [nameof(_settings.WeatherQuality)] = _settings.WeatherQuality
        };
        DisplayServer.ClipboardSet("War Machine · FPS weather and time settings\n"
            + JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
        _copy.Text = "Copied!";
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_clock) && _clock.IsConnected("time_changed", _timeChanged))
            _clock.Disconnect("time_changed", _timeChanged);
        if (IsInstanceValid(_panel)) _panel.QueueFree();
    }
}
