// FPS-only fog profile and tuning panel. Settings/input events link opaque fog to the camera far plane.
// No frame callbacks or per-object polling; shared shader follows renderer camera matrices automatically.
using Godot;

public partial class FirstPersonFog : Node, IDebugOverlayView
{
    private FirstPersonCamera _camera = null!;
    private FirstPersonSettings _settings = null!;
    private EnvironmentConfig _fogSettings = null!;
    public PlayerDistanceFog Fog => _fog;
    private PlayerDistanceFog _fog = null!;
    private Label _readout = null!;
    private float _originalFar;

    // =========================================================
    // Local profile copies isolate FPS edits from top-down masters and the existing sky/weather owner.
    public void Initialize(FirstPersonCamera camera, PlayerController player, EnvironmentConfig environment)
    {
        _camera = camera; _originalFar = camera.Far;
        _settings = (FirstPersonSettings)camera.ViewSettings.Duplicate(true);
        _fogSettings = (EnvironmentConfig)environment.Duplicate(true);
        _fog = new PlayerDistanceFog { Name = "DistanceFog" }; AddChild(_fog);
        _fogSettings.DistanceFogEnabled = _settings.DistanceFogEnabled;
        _fog.Initialize(camera, player, _fogSettings, cameraDistance: true, atmosphere: environment);
        SetProcess(false); SetPhysicsProcess(false);
        Apply();
    }

    // =========================================================
    // Spherical fully opaque fog conceals the far plane in every direction, including looking up/down.
    private void Apply()
    {
        float start = Mathf.Clamp(_settings.FogStartDistance, 1, 250);
        float end = Mathf.Clamp(_settings.FogEndDistance, start + 1, 300);
        float buffer = Mathf.Clamp(_settings.FogCullBuffer, 8, 200);
        _fogSettings.DistanceFogEnabled = _settings.DistanceFogEnabled;
        _fogSettings.DistanceFogStart = start; _fogSettings.DistanceFogEnd = end;
        _fogSettings.DistanceFogOpacity = 1; _fogSettings.DistanceFogEdgeVariation = 0;
        _fogSettings.DistanceFogColor = _settings.FogColor;
        _fog.ApplySettings();
        float far = _settings.DistanceFogEnabled ? end + buffer : _originalFar;
        bool rangeChanged = !Mathf.IsEqualApprox(_camera.Far, far);
        _camera.Far = far;
        if (rangeChanged) _camera.NotifyRenderRangeChanged();
        if (IsInstanceValid(_readout)) _readout.Text = $"Clear: {start:0}m · opaque: {end:0}m · render far: {_camera.Far:0}m";
    }

    // =========================================================
    // Explicit slider events apply immediately; no Inspector/resource polling while idle.
    public void BuildControls(VBoxContainer environmentRows, VBoxContainer root)
    {
        var box = environmentRows;
        var rows = environmentRows;
        var fogRows = FirstPersonTuningSections.Add(rows, "Distance fog");
        var enabled = new CheckButton { Text = "Distance fog + linked rendering", ButtonPressed = _settings.DistanceFogEnabled, FocusMode = Control.FocusModeEnum.None };
        enabled.Toggled += value => { _settings.DistanceFogEnabled = value; Apply(); }; fogRows.AddChild(enabled);
        Slider(fogRows, "Fog starts (m)", 1, 150, _settings.FogStartDistance, value => _settings.FogStartDistance = value);
        Slider(fogRows, "Fog fully opaque (m)", 10, 250, _settings.FogEndDistance, value => _settings.FogEndDistance = value);
        var color = new ColorPickerButton { Color = _settings.FogColor, EditAlpha = false, Text = "Fog colour", FocusMode = Control.FocusModeEnum.None };
        color.ColorChanged += value => { _settings.FogColor = value; Apply(); }; fogRows.AddChild(color);
        var renderRows = FirstPersonTuningSections.Add(root, "Rendering / culling");
        Slider(renderRows, "Hidden render buffer (m)", 8, 200, _settings.FogCullBuffer, value => _settings.FogCullBuffer = value);
        _readout = new Label(); box.AddChild(_readout);
        var copy = new Button { Text = "Copy fog settings", FocusMode = Control.FocusModeEnum.None };
        copy.Pressed += () => DisplayServer.ClipboardSet(System.FormattableString.Invariant(
            $"DistanceFogEnabled = {_settings.DistanceFogEnabled}\nFogStartDistance = {_fogSettings.DistanceFogStart}\nFogEndDistance = {_fogSettings.DistanceFogEnd}\nFogCullBuffer = {Mathf.Clamp(_settings.FogCullBuffer, 8, 200)}\nFogColor = Color({_settings.FogColor.R}, {_settings.FogColor.G}, {_settings.FogColor.B}, 1)"));
        box.AddChild(copy); Apply();
    }

    // =========================================================
    private void Slider(VBoxContainer box, string name, float min, float max, float value, System.Action<float> set)
    {
        box.AddChild(new Label { Text = name });
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = 1, Value = value, FocusMode = Control.FocusModeEnum.None, Scrollable = false };
        slider.ValueChanged += next => { set((float)next); Apply(); }; box.AddChild(slider);
    }

    // =========================================================
    // Replace overhead-only snapshot fields with this adapter's effective FPS profile.
    public void WriteSnapshot(System.Collections.Generic.Dictionary<string, object> values)
    {
        foreach (string key in new[] { "DistanceFogEnabled", "DistanceFogStart", "DistanceFogEnd", "DistanceFogOpacity",
            "DistanceFogEdgeVariation", "DistanceFogColor", "UsePerspective", "PerspectiveFov", "CameraPitchDegrees",
            "CameraYawDegrees", "CameraSize", "CameraDistance", "MinimumZoom", "MaximumZoom", "MinimumPerspectiveDistance",
            "MaximumPerspectiveDistance", "ZoomStep", "FollowSmoothing", "ObstructionFadeEnabled", "ObstructedOpacity",
            "ObstructionRadius", "ObstructionChecksPerSecond", "ObstructionFadeSeconds" }) values.Remove(key);
        values["View"] = "FirstPerson";
        values["DistanceFogEnabled"] = _settings.DistanceFogEnabled;
        values["FogStartDistance"] = _fogSettings.DistanceFogStart;
        values["FogEndDistance"] = _fogSettings.DistanceFogEnd;
        values["FogCullBuffer"] = Mathf.Clamp(_settings.FogCullBuffer, 8, 200);
        values["FogColor"] = new[] { _settings.FogColor.R, _settings.FogColor.G, _settings.FogColor.B, _settings.FogColor.A };
        values["FieldOfView"] = _camera.Fov;
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_camera)) _camera.Far = _originalFar;
    }
}
