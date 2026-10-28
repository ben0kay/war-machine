// Shared haze tuning widgets for both camera views; edits only their runtime settings and existing haze owner.
using Godot;
using System;

public static class GroundHazeControls
{
    // =========================================================
    // Same controls and bindings in both debug UIs; no UI polling or duplicate atmosphere implementation.
    public static void Build(VBoxContainer parent, EnvironmentConfig settings, GroundHaze haze, Action changed)
    {
        var toggle = new Button { Text = "+ Ground haze", ToggleMode = true, FocusMode = Control.FocusModeEnum.None,
            Alignment = HorizontalAlignment.Left };
        var rows = new VBoxContainer { Visible = false };
        parent.AddChild(toggle); parent.AddChild(rows);
        toggle.Toggled += open => { rows.Visible = open; toggle.Text = (open ? "− " : "+ ") + "Ground haze"; };
        var enabled = new CheckButton { Text = "Ground haze enabled", ButtonPressed = settings.GroundHazeEnabled,
            FocusMode = Control.FocusModeEnum.None };
        rows.AddChild(enabled);
        enabled.Toggled += value => { settings.GroundHazeEnabled = value; haze.ApplySettings(); changed(); };
        Slider(rows, "Density", 0, 0.5f, 0.01f, settings.GroundHazeDensity, value => settings.GroundHazeDensity = value, haze, changed);
        Slider(rows, "Height (m)", 0.25f, 3, 0.05f, settings.GroundHazeHeight, value => settings.GroundHazeHeight = value, haze, changed);
        Slider(rows, "Noise scale", 0.05f, 1, 0.01f, settings.GroundHazeNoiseScale, value => settings.GroundHazeNoiseScale = value, haze, changed);
        Slider(rows, "Drift (m/s)", 0, 2, 0.01f, settings.GroundHazeDriftSpeed, value => settings.GroundHazeDriftSpeed = value, haze, changed);
        Slider(rows, "Wisp definition", 0, 1, 0.01f, settings.GroundHazeContrast, value => settings.GroundHazeContrast = value, haze, changed);
        Slider(rows, "Turbulence", 0, 1, 0.01f, settings.GroundHazeTurbulence, value => settings.GroundHazeTurbulence = value, haze, changed);
        Slider(rows, "Building edge fade (m) · release to apply", 0.1f, 5, 0.05f, settings.GroundHazeExclusionFadeMeters,
            value => settings.GroundHazeExclusionFadeMeters = value, haze, changed, deferred: true);
        Slider(rows, "Coverage (m) · release to apply", 6, 48, 1, settings.GroundHazeCoverageRadius,
            value => settings.GroundHazeCoverageRadius = value, haze, changed, deferred: true);
        var color = new ColorPickerButton { Text = "Haze colour", Color = settings.GroundHazeColor, EditAlpha = false,
            FocusMode = Control.FocusModeEnum.None };
        rows.AddChild(color);
        color.ColorChanged += value => { settings.GroundHazeColor = value; haze.ApplySettings(); changed(); };
    }

    // =========================================================
    // Expensive coverage resize commits on release; ordinary settings apply only on explicit input.
    private static void Slider(VBoxContainer rows, string title, float min, float max, float step, float value,
        Action<float> store, GroundHaze haze, Action changed, bool deferred = false)
    {
        var label = new Label { Text = $"{title}: {value:0.##}" }; rows.AddChild(label);
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step, Scrollable = false,
            FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(300, 20) };
        slider.SetValueNoSignal(value); rows.AddChild(slider);
        slider.ValueChanged += next =>
        {
            label.Text = $"{title}: {next:0.##}"; store((float)next); changed();
            if (!deferred) haze.ApplySettings();
        };
        if (deferred) slider.DragEnded += edited => { if (edited) haze.ApplySettings(); };
    }
}
