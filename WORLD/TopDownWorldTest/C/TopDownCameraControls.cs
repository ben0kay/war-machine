// Preview-only camera tuning: input-driven sliders and an explicit clipboard snapshot, with no idle polling.
using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class TopDownCameraControls : PanelContainer
{
    private TopDownCamera _camera = null!;
    private TopDownTestSettings _settings = null!;
    private HSlider _pitch = null!, _zoom = null!;
    private Label _pitchLabel = null!, _zoomLabel = null!;
    private Button _copy = null!;

    // =========================================================
    // Runtime copy belongs to this preview; slider edits never write resources or affect the original world.
    public void Initialize(TopDownCamera camera, TopDownTestSettings settings)
    {
        _camera = camera; _settings = settings;
        SetProcess(false); SetPhysicsProcess(false);
        Position = new Vector2(16, 90);
        CustomMinimumSize = new Vector2(330, 0);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 12);
        AddChild(margin);
        var rows = new VBoxContainer(); rows.AddThemeConstantOverride("separation", 8); margin.AddChild(rows);
        rows.AddChild(new Label { Text = "Camera · live preview" });
        _pitchLabel = new Label(); rows.AddChild(_pitchLabel);
        _pitch = AddSlider(rows, 25, 80, 0.5);
        rows.AddChild(new Label { Text = "25° low angle → 80° overhead", Modulate = new Color(0.75f, 0.8f, 0.85f) });
        _zoomLabel = new Label(); rows.AddChild(_zoomLabel);
        _zoom = AddSlider(rows, Mathf.Max(5, Mathf.Min(settings.MinimumZoom, settings.MaximumZoom)),
            Mathf.Max(5, Mathf.Max(settings.MinimumZoom, settings.MaximumZoom)), 0.5);
        _copy = new Button { Text = "Copy settings", FocusMode = FocusModeEnum.None }; rows.AddChild(_copy);
        _pitch.ValueChanged += OnPitchChanged;
        _zoom.ValueChanged += OnZoomChanged;
        _copy.Pressed += CopySettings;
        _camera.SettingsChanged += Refresh;
        Refresh();
    }

    // =========================================================
    // Controls receive mouse input without capturing keyboard focus from walking.
    private static HSlider AddSlider(VBoxContainer rows, double minimum, double maximum, double step)
    {
        var slider = new HSlider { MinValue = minimum, MaxValue = maximum, Step = step,
            FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(300, 20) };
        rows.AddChild(slider); return slider;
    }

    private void OnPitchChanged(double value) => _camera.SetPitch((float)value);
    private void OnZoomChanged(double value) => _camera.SetZoom((float)value);

    // =========================================================
    // Camera signals also cover wheel zoom; silent value updates prevent recursive slider callbacks.
    private void Refresh()
    {
        _pitch.SetValueNoSignal(_settings.CameraPitchDegrees);
        _zoom.SetValueNoSignal(_settings.CameraSize);
        _pitchLabel.Text = $"Tilt downward: {_settings.CameraPitchDegrees:0.0}°";
        _zoomLabel.Text = $"Zoom / view size: {_settings.CameraSize:0.0} m";
        _copy.Text = "Copy settings";
    }

    // =========================================================
    // Export numeric JSON using the authoring property names; copy the chosen zoom target, not a mid-smoothing frame.
    private void CopySettings()
    {
        var values = new Dictionary<string, float>
        {
            [nameof(_settings.CameraPitchDegrees)] = _settings.CameraPitchDegrees,
            [nameof(_settings.CameraYawDegrees)] = _settings.CameraYawDegrees,
            [nameof(_settings.CameraSize)] = _settings.CameraSize,
            [nameof(_settings.CameraDistance)] = _settings.CameraDistance,
            [nameof(_settings.MinimumZoom)] = _settings.MinimumZoom,
            [nameof(_settings.MaximumZoom)] = _settings.MaximumZoom,
            [nameof(_settings.ZoomStep)] = _settings.ZoomStep,
            [nameof(_settings.FollowSmoothing)] = _settings.FollowSmoothing
        };
        DisplayServer.ClipboardSet("War Machine · TopDownWorldTest camera settings\n"
            + JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
        _copy.Text = "Copied! Paste these settings into chat";
    }

    // =========================================================
    // Remove the external subscription when the scene or panel unloads.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_camera)) _camera.SettingsChanged -= Refresh;
    }
}
