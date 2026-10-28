// Active world tuning: input-driven sliders and an explicit clipboard snapshot, with no idle polling.
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class DebugOverlay : PanelContainer
{
    private IDebugOverlayView? _view;
    private WorldRenderCulling? _renderCulling;
    private Camera _camera = null!;
    private CameraSettings _settings = null!;
    private HSlider _pitch = null!, _yaw = null!, _zoom = null!, _fov = null!;
    private Label _fovLabel = null!;
    private OptionButton _projection = null!;
    private Label _pitchLabel = null!, _yawLabel = null!, _zoomLabel = null!;
    private Button _copy = null!;
    private SkyController _sky = null!;
    private PinePlacement _pines = null!;
    private TerrainGrass _grass = null!;
    private EnvironmentConfig _environment = null!;
    private Node _clock = null!;
    private Callable _timeChanged;
    private HSlider _hour = null!;
    private Label _hourLabel = null!;
    private ZombieVisionDebug? _visionDebug;
    private CheckButton? _visionToggle;

    // =========================================================
    // Runtime copy belongs to this preview; slider edits never write resources or affect the original world.
    public void Initialize(Camera camera, CameraSettings settings, SkyController sky, WeatherBridge weather, TerrainGrass grass, PinePlacement pines, PlayerDistanceFog? distanceFog = null, GroundHaze? groundHaze = null, ZombieVisionDebug? visionDebug = null, IDebugOverlayView? view = null, WorldRenderCulling? renderCulling = null)
    {
        _view = view; _renderCulling = renderCulling;
        _camera = camera; _settings = settings; _sky = sky; _environment = sky.Settings; _grass = grass; _pines = pines;
        SetProcess(false); SetPhysicsProcess(false);
        _copy = GetNode<Button>("Margin/Outer/Copy");
        GetNode<Label>("Margin/Outer/Hint").Text = view == null ? "Debug controls" : "Debug controls · Esc to use · click world to resume";
        float maximumPanelHeight = Mathf.Clamp(GetViewportRect().Size.Y - 180, 180, 520);
        var scroll = GetNode<ScrollContainer>("Margin/Outer/Scroll");
        var rows = GetNode<VBoxContainer>("Margin/Outer/Scroll/Rows");
        var rootRows = rows;
        rootRows.MinimumSizeChanged += () => scroll.CustomMinimumSize = new Vector2(350,
            Mathf.Clamp(rootRows.GetCombinedMinimumSize().Y, 120, maximumPanelHeight));
        if (_view == null)
        {
            rows = AddSection(rootRows, "Camera");
            rows.AddChild(new Label { Text = "Camera · live preview" });
            _projection = new OptionButton { FocusMode = FocusModeEnum.None };
            _projection.AddItem("Orthographic"); _projection.AddItem("Perspective"); rows.AddChild(_projection);
            _projection.ItemSelected += index => _camera.SetProjectionMode(index == 1);
            _fovLabel = new Label(); rows.AddChild(_fovLabel);
            _fov = AddSlider(rows, 25, 90, 0.5);
            _fov.ValueChanged += value => _camera.SetPerspectiveFov((float)value);
            _pitchLabel = new Label(); rows.AddChild(_pitchLabel);
            _pitch = AddSlider(rows, 25, 80, 0.5);
            rows.AddChild(new Label { Text = "25° low angle → 80° overhead", Modulate = new Color(0.75f, 0.8f, 0.85f) });
            _yawLabel = new Label(); rows.AddChild(_yawLabel);
            _yaw = AddSlider(rows, 0, 360, 1);
            _zoomLabel = new Label(); rows.AddChild(_zoomLabel);
            _zoom = AddSlider(rows, Mathf.Max(5, Mathf.Min(settings.MinimumZoom, settings.MaximumZoom)),
                Mathf.Max(5, Mathf.Max(settings.MinimumZoom, settings.MaximumZoom)), 0.5);
        }
        var environmentRows = AddSection(rootRows, "Environment");
        if (_view != null) _view.BuildControls(environmentRows, rootRows);
        else
        {
            rows = AddSection(environmentRows, "Distance fog");
            if (distanceFog != null)
            {
                rows.AddChild(new Label { Text = "Visibility fog · distance from player" });
                var enabled = new CheckButton { Text = "Edge fog enabled", ButtonPressed = _environment.DistanceFogEnabled, FocusMode = FocusModeEnum.None };
                rows.AddChild(enabled);
                enabled.Toggled += value => { _environment.DistanceFogEnabled = value; distanceFog.ApplySettings(); _copy.Text = "Copy settings"; };
                AddBoundSlider(rows, "Clear radius (m)", 5, 100, 1, _environment.DistanceFogStart,
                    value => { _environment.DistanceFogStart = value; distanceFog.ApplySettings(); });
                AddBoundSlider(rows, "Full fog radius (m)", 6, 160, 1, _environment.DistanceFogEnd,
                    value => { _environment.DistanceFogEnd = value; distanceFog.ApplySettings(); });
                AddBoundSlider(rows, "Fog opacity", 0, 1, 0.01, _environment.DistanceFogOpacity,
                    value => { _environment.DistanceFogOpacity = value; distanceFog.ApplySettings(); });
                AddBoundSlider(rows, "Fog edge variation (m)", 0, 8, 0.1, _environment.DistanceFogEdgeVariation,
                    value => { _environment.DistanceFogEdgeVariation = value; distanceFog.ApplySettings(); });
                var tint = new ColorPickerButton { Text = "Fog color", Color = _environment.DistanceFogColor, EditAlpha = false, FocusMode = FocusModeEnum.None };
                rows.AddChild(tint);
                tint.ColorChanged += color => { _environment.DistanceFogColor = color; distanceFog.ApplySettings(); _copy.Text = "Copy settings"; };
            }
        }
        rows = AddSection(environmentRows, "Day / night");
        _hourLabel = new Label(); rows.AddChild(_hourLabel);
        _hour = AddSlider(rows, 0, 23.99, 0.01);
        _hour.SetValueNoSignal(_environment.StartHour);
        _hour.ValueChanged += value => { _environment.StartHour = (float)value; _sky.ApplyTime(); };
        AddBoundSlider(rows, "Day length (minutes)", 1, 1440, 1, _environment.DayLengthMinutes,
            value => { _environment.DayLengthMinutes = value; _sky.ApplyCycle(); });
        var pause = new CheckButton { Text = "Pause day / night", ButtonPressed = _environment.StartPaused, FocusMode = FocusModeEnum.None };
        rows.AddChild(pause); pause.Toggled += value => { _sky.SetPaused(value); _copy.Text = "Copy settings"; };
        rows = AddSection(environmentRows, "Lighting / atmosphere");
        var responsiveFog = new CheckButton { Text = "Fog follows day / night", ButtonPressed = _environment.TimeResponsiveFog, FocusMode = FocusModeEnum.None };
        rows.AddChild(responsiveFog);
        responsiveFog.Toggled += value => { _environment.TimeResponsiveFog = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        var rays = new CheckButton { Text = "Sun / moon rays (Forward+)", ButtonPressed = _environment.LightRaysEnabled, FocusMode = FocusModeEnum.None };
        rows.AddChild(rays);
        rays.Toggled += value => { _environment.LightRaysEnabled = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        AddBoundSlider(rows, "Day ambient energy", 0, 2, 0.01, _environment.AmbientLightEnergy,
            value => { _environment.AmbientLightEnergy = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Night ambient energy", 0, 2, 0.01, _environment.NightAmbientEnergy,
            value => { _environment.NightAmbientEnergy = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Moonlight energy", 0, 2, 0.01, _environment.MoonlightEnergy,
            value => { _environment.MoonlightEnergy = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "FPS sky fog opacity", 0, 1, 0.01, _environment.FpsSkyFogOpacity,
            value => { _environment.FpsSkyFogOpacity = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Ray fog density", 0, 0.01, 0.0001, _environment.LightRaysDensity,
            value => { _environment.LightRaysDensity = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Ray fog range (m)", 20, 160, 1, _environment.LightRaysLength,
            value => { _environment.LightRaysLength = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Sun ray strength", 0, 8, 0.1, _environment.SunRayEnergy,
            value => { _environment.SunRayEnergy = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Moon ray strength", 0, 8, 0.1, _environment.MoonRayEnergy,
            value => { _environment.MoonRayEnergy = value; _sky.ApplyAtmosphere(); });
        AddBoundSlider(rows, "Ray forward scattering", -0.9, 0.9, 0.01, _environment.RayAnisotropy,
            value => { _environment.RayAnisotropy = value; _sky.ApplyAtmosphere(); });
        var NightFogColorPicker = new ColorPickerButton { Text = "Night fog colour", Color = _environment.NightFogColor, EditAlpha = false, FocusMode = FocusModeEnum.None };
        rows.AddChild(NightFogColorPicker);
        NightFogColorPicker.ColorChanged += value => { _environment.NightFogColor = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        var DuskFogColorPicker = new ColorPickerButton { Text = "Dusk fog colour", Color = _environment.DuskFogColor, EditAlpha = false, FocusMode = FocusModeEnum.None };
        rows.AddChild(DuskFogColorPicker);
        DuskFogColorPicker.ColorChanged += value => { _environment.DuskFogColor = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        var NightAmbientColorPicker = new ColorPickerButton { Text = "Night ambient colour", Color = _environment.NightAmbientColor, EditAlpha = false, FocusMode = FocusModeEnum.None };
        rows.AddChild(NightAmbientColorPicker);
        NightAmbientColorPicker.ColorChanged += value => { _environment.NightAmbientColor = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        var DayAmbientColorPicker = new ColorPickerButton { Text = "Day ambient colour", Color = _environment.DayAmbientColor, EditAlpha = false, FocusMode = FocusModeEnum.None };
        rows.AddChild(DayAmbientColorPicker);
        DayAmbientColorPicker.ColorChanged += value => { _environment.DayAmbientColor = value; _sky.ApplyAtmosphere(); _copy.Text = "Copy settings"; };
        rows = AddSection(environmentRows, "Weather and shared wind");
        var weatherEnabled = new CheckButton { Text = "Weather enabled", ButtonPressed = _environment.WeatherEnabled, FocusMode = FocusModeEnum.None };
        rows.AddChild(weatherEnabled);
        weatherEnabled.Toggled += value => { _environment.WeatherEnabled = value; weather.ApplySettings(); _copy.Text = "Copy settings"; };
        AddBoundSlider(rows, "Rain intensity", 0, 1, 0.01, _environment.RainIntensity,
            value => { _environment.RainIntensity = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Snow intensity", 0, 1, 0.01, _environment.SnowIntensity,
            value => { _environment.SnowIntensity = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Local mist", 0, 1, 0.01, _environment.LocalMistIntensity,
            value => { _environment.LocalMistIntensity = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Airborne motes", 0, 1, 0.01, _environment.AirborneIntensity,
            value => { _environment.AirborneIntensity = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Wind strength", 0, 2, 0.01, _environment.GrassWindStrength,
            value => { _environment.GrassWindStrength = value; weather.ApplySettings(); groundHaze?.ApplySettings(); grass.ApplyAppearance(); pines.ApplyWind(); });
        AddBoundSlider(rows, "Wind heading (degrees)", 0, 360, 1, _environment.WindHeading,
            value => { _environment.WindHeading = value; float angle = Mathf.DegToRad(value);
                _environment.WindDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); weather.ApplySettings(); groundHaze?.ApplySettings(); grass.ApplyAppearance(); pines.ApplyWind(); });
        AddBoundSlider(rows, "Gusts", 0, 1, 0.01, _environment.WeatherGusts,
            value => { _environment.WeatherGusts = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Turbulence", 0, 2, 0.01, _environment.WeatherTurbulence,
            value => { _environment.WeatherTurbulence = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Particle size", 0.1, 3, 0.05, _environment.WeatherParticleSize,
            value => { _environment.WeatherParticleSize = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Fall speed", 0.1, 3, 0.05, _environment.WeatherFallSpeed,
            value => { _environment.WeatherFallSpeed = value; weather.ApplySettings(); });
        AddBoundSlider(rows, "Weather quality (0 / 1 / 2)", 0, 2, 1, _environment.WeatherQuality,
            value => { _environment.WeatherQuality = value; weather.ApplySettings(); });
        if (groundHaze != null)
            GroundHazeControls.Build(environmentRows, _environment, groundHaze, () => _copy.Text = "Copy settings");
        if (renderCulling != null)
        {
            var renderRows = AddSection(rootRows, "Fog-aware rendering (WS1)");
            var toggle = new CheckButton { Text = "Cull scenery behind opaque fog", ButtonPressed = renderCulling.Settings.Enabled, FocusMode = FocusModeEnum.None };
            renderRows.AddChild(toggle);
            toggle.Toggled += value => { renderCulling.Settings.Enabled = value; renderCulling.ApplySettings(); _copy.Text = "Copy settings"; };
            AddBoundSlider(renderRows, "Hidden buffer (m)", 4, 64, 1, renderCulling.Settings.HiddenBuffer,
                value => { renderCulling.Settings.HiddenBuffer = value; renderCulling.ApplySettings(); });
            AddBoundSlider(renderRows, "Shadow buffer (m)", 0, 160, 1, renderCulling.Settings.ShadowBuffer,
                value => { renderCulling.Settings.ShadowBuffer = value; renderCulling.ApplySettings(); });
        }
        var foliageRows = AddSection(rootRows, "Foliage");
        rows = AddSection(foliageRows, "Grass");
        rows.AddChild(new Label { Text = "Release density/height to apply" });
        var grassEnabled = new CheckButton { Text = "Grass enabled", ButtonPressed = grass.Settings.Enabled, FocusMode = FocusModeEnum.None };
        rows.AddChild(grassEnabled); grassEnabled.Toggled += value => { grass.Settings.Enabled = value; grass.ApplySettings(); _copy.Text = "Copy settings"; };
        AddDeferredSlider(rows, "Grass density", 0, 2, 0.05, grass.Settings.Density,
            value => grass.Settings.Density = value, grass.ApplySettings);
        AddDeferredSlider(rows, "Grass height", 0.1, 1.5, 0.05, grass.Settings.Height,
            value => grass.Settings.Height = value, grass.ApplyAppearance);
        AddBoundSlider(rows, "Grass wind multiplier", 0, 2, 0.05, grass.Settings.WindMultiplier,
            value => { grass.Settings.WindMultiplier = value; grass.ApplyAppearance(); });
        rows = AddSection(foliageRows, "Pine trees");
        rows.AddChild(new Label { Text = "Release density/size to apply" });
        var pineEnabled = new CheckButton { Text = "Pine trees enabled", ButtonPressed = pines.Settings.Enabled, FocusMode = FocusModeEnum.None };
        rows.AddChild(pineEnabled); pineEnabled.Toggled += value => { pines.Settings.Enabled = value; pines.ApplySettings(); _copy.Text = "Copy settings"; };
        AddDeferredSlider(rows, "Pine density", 0, 1, 0.05, pines.Settings.Density,
            value => pines.Settings.Density = value, pines.ApplySettings);
        AddDeferredSlider(rows, "Pine size multiplier", 0.5, 1.5, 0.05, pines.Settings.HeightMultiplier,
            value => pines.Settings.HeightMultiplier = value, pines.ApplySettings);
        AddBoundSlider(rows, "Pine wind", 0, 1, 0.05, pines.Settings.LeafWindStrength,
            value => { pines.Settings.LeafWindStrength = value; pines.ApplyWind(); });
        if (visionDebug != null)
        {
            rows = AddSection(rootRows, "Debug");
            _visionDebug = visionDebug;
            _visionToggle = new CheckButton { Text = "Show zombie sight cones",
                ButtonPressed = visionDebug.ConesVisible, FocusMode = FocusModeEnum.None };
            rows.AddChild(_visionToggle);
            _visionToggle.Toggled += visionDebug.SetConesVisible;
            _visionDebug.VisibilityChanged += OnVisionDebugChanged;
        }
        _clock = _sky.Sky.GetNode("TimeOfDay");
        _timeChanged = Callable.From<float>(OnTimeChanged);
        _clock.Connect("time_changed", _timeChanged);
        OnTimeChanged(_sky.Sky.Get("current_time").AsSingle());
        if (_view == null)
        {
            _pitch.ValueChanged += OnPitchChanged;
            _yaw.ValueChanged += OnYawChanged;
            _zoom.ValueChanged += OnZoomChanged;
        }
        _copy.Pressed += CopySettings;
        if (_view == null) { _camera.SettingsChanged += Refresh; Refresh(); }
    }

    // =========================================================
    // Nested disclosure buttons only change visibility on input; no polling or keyboard focus capture.
    private static VBoxContainer AddSection(VBoxContainer parent, string title)
    {
        var button = new Button { Text = "+ " + title, ToggleMode = true, FocusMode = FocusModeEnum.None,
            Alignment = HorizontalAlignment.Left };
        var body = new VBoxContainer { Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 6);
        parent.AddChild(button); parent.AddChild(body);
        button.Toggled += open => { body.Visible = open; button.Text = (open ? "− " : "+ ") + title; };
        return body;
    }

    // =========================================================
    // F7 can change visibility while the panel is open; silent synchronization prevents recursive callbacks.
    private void OnVisionDebugChanged(bool visible)
    { if (IsInstanceValid(_visionToggle)) _visionToggle!.SetPressedNoSignal(visible); }

    // =========================================================
    // Controls receive mouse input without capturing keyboard focus from walking.
    private static HSlider AddSlider(VBoxContainer rows, double minimum, double maximum, double step)
    {
        var slider = new HSlider { MinValue = minimum, MaxValue = maximum, Step = step,
            FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(300, 20) };
        rows.AddChild(slider); return slider;
    }

    // =========================================================
    // One binding per control: labels and resources change only on input, without reflection or polling.
    private void AddBoundSlider(VBoxContainer rows, string title, double minimum, double maximum, double step,
        float initial, Action<float> apply)
    {
        var label = new Label { Text = $"{title}: {initial:0.##}" }; rows.AddChild(label);
        var slider = AddSlider(rows, minimum, maximum, step); slider.SetValueNoSignal(initial);
        slider.ValueChanged += value => { label.Text = $"{title}: {value:0.##}"; apply((float)value); _copy.Text = "Copy settings"; };
    }

    // =========================================================
    // Rebuild/reseed only on drag release, avoiding a fresh GPU pool for every mouse movement.
    private void AddDeferredSlider(VBoxContainer rows, string title, double minimum, double maximum, double step,
        float initial, Action<float> store, Action apply)
    {
        var label = new Label { Text = $"{title}: {initial:0.##}" }; rows.AddChild(label);
        var slider = AddSlider(rows, minimum, maximum, step); slider.Scrollable = false; slider.SetValueNoSignal(initial);
        slider.ValueChanged += value => { label.Text = $"{title}: {value:0.##}"; store((float)value); _copy.Text = "Copy settings"; };
        slider.DragEnded += changed => { if (changed) apply(); };
    }

    // =========================================================
    // Sky3D owns the clock; its existing signal keeps the hour slider current during an unpaused day.
    private void OnTimeChanged(float hour)
    {
        _environment.StartHour = hour;
        _hour.SetValueNoSignal(hour);
        int minutes = Mathf.FloorToInt(hour * 60);
        _hourLabel.Text = $"Time: {minutes / 60:00}:{minutes % 60:00}";
    }

    private void OnPitchChanged(double value) => _camera.SetPitch((float)value);
    private void OnYawChanged(double value) => _camera.SetYaw((float)value);
    private void OnZoomChanged(double value) => _camera.SetZoom((float)value);

    // =========================================================
    // Camera signals also cover wheel zoom; silent value updates prevent recursive slider callbacks.
    private void Refresh()
    {
        _pitch.SetValueNoSignal(_settings.CameraPitchDegrees);
        _yaw.SetValueNoSignal(_settings.CameraYawDegrees);
        _projection.Select(_settings.UsePerspective ? 1 : 0);
        _fov.Visible = _fovLabel.Visible = _settings.UsePerspective;
        _fov.SetValueNoSignal(_settings.PerspectiveFov);
        _fovLabel.Text = $"Perspective FOV: {_settings.PerspectiveFov:0.0}°";
        _zoom.MinValue = _settings.UsePerspective
            ? Mathf.Max(1, Mathf.Min(_settings.MinimumPerspectiveDistance, _settings.MaximumPerspectiveDistance))
            : Mathf.Max(5, Mathf.Min(_settings.MinimumZoom, _settings.MaximumZoom));
        _zoom.MaxValue = _settings.UsePerspective
            ? Mathf.Max(1, Mathf.Max(_settings.MinimumPerspectiveDistance, _settings.MaximumPerspectiveDistance))
            : Mathf.Max(5, Mathf.Max(_settings.MinimumZoom, _settings.MaximumZoom));
        _zoom.SetValueNoSignal(_settings.UsePerspective ? _settings.CameraDistance : _settings.CameraSize);
        _pitchLabel.Text = $"Tilt downward: {_settings.CameraPitchDegrees:0.0}°";
        _yawLabel.Text = $"Camera rotation: {_settings.CameraYawDegrees:0}°";
        _zoomLabel.Text = _settings.UsePerspective ? $"Camera distance: {_settings.CameraDistance:0.0} m" : $"Zoom / view size: {_settings.CameraSize:0.0} m";
        _copy.Text = "Copy settings";
    }

    // =========================================================
    // Export numeric JSON using the authoring property names; copy the chosen zoom target, not a mid-smoothing frame.
    private void CopySettings()
    {
        var values = new Dictionary<string, object>
        {
            [nameof(_environment.TimeResponsiveFog)] = _environment.TimeResponsiveFog,
            [nameof(_environment.NightFogColor)] = new[] { _environment.NightFogColor.R, _environment.NightFogColor.G, _environment.NightFogColor.B, _environment.NightFogColor.A },
            [nameof(_environment.DuskFogColor)] = new[] { _environment.DuskFogColor.R, _environment.DuskFogColor.G, _environment.DuskFogColor.B, _environment.DuskFogColor.A },
            [nameof(_environment.DayAmbientColor)] = new[] { _environment.DayAmbientColor.R, _environment.DayAmbientColor.G, _environment.DayAmbientColor.B, _environment.DayAmbientColor.A },
            [nameof(_environment.NightAmbientColor)] = new[] { _environment.NightAmbientColor.R, _environment.NightAmbientColor.G, _environment.NightAmbientColor.B, _environment.NightAmbientColor.A },
            [nameof(_environment.AmbientLightEnergy)] = _environment.AmbientLightEnergy,
            [nameof(_environment.NightAmbientEnergy)] = _environment.NightAmbientEnergy,
            [nameof(_environment.MoonlightEnergy)] = _environment.MoonlightEnergy,
            [nameof(_environment.FpsSkyFogOpacity)] = _environment.FpsSkyFogOpacity,
            [nameof(_environment.LightRaysEnabled)] = _environment.LightRaysEnabled,
            [nameof(_environment.LightRaysDensity)] = _environment.LightRaysDensity,
            [nameof(_environment.LightRaysLength)] = _environment.LightRaysLength,
            [nameof(_environment.SunRayEnergy)] = _environment.SunRayEnergy,
            [nameof(_environment.MoonRayEnergy)] = _environment.MoonRayEnergy,
            [nameof(_environment.RayAnisotropy)] = _environment.RayAnisotropy,
            [nameof(_environment.DistanceFogEnabled)] = _environment.DistanceFogEnabled,
            [nameof(_environment.DistanceFogStart)] = _environment.DistanceFogStart,
            [nameof(_environment.DistanceFogEnd)] = _environment.DistanceFogEnd,
            [nameof(_environment.DistanceFogOpacity)] = _environment.DistanceFogOpacity,
            [nameof(_environment.DistanceFogEdgeVariation)] = _environment.DistanceFogEdgeVariation,
            [nameof(_environment.DistanceFogColor)] = new[] { _environment.DistanceFogColor.R, _environment.DistanceFogColor.G, _environment.DistanceFogColor.B, _environment.DistanceFogColor.A },
            [nameof(_environment.GroundHazeEnabled)] = _environment.GroundHazeEnabled,
            [nameof(_environment.GroundHazeDensity)] = _environment.GroundHazeDensity,
            [nameof(_environment.GroundHazeHeight)] = _environment.GroundHazeHeight,
            [nameof(_environment.GroundHazeNoiseScale)] = _environment.GroundHazeNoiseScale,
            [nameof(_environment.GroundHazeDriftSpeed)] = _environment.GroundHazeDriftSpeed,
            [nameof(_environment.GroundHazeContrast)] = _environment.GroundHazeContrast,
            [nameof(_environment.GroundHazeTurbulence)] = _environment.GroundHazeTurbulence,
            [nameof(_environment.GroundHazeCoverageRadius)] = _environment.GroundHazeCoverageRadius,
            [nameof(_environment.GroundHazeColor)] = new[] { _environment.GroundHazeColor.R, _environment.GroundHazeColor.G, _environment.GroundHazeColor.B, _environment.GroundHazeColor.A },
            [nameof(_environment.WindDirection)] = new[] { _environment.WindDirection.X, _environment.WindDirection.Y },
            ["PineEnabled"] = _pines.Settings.Enabled,
            ["PineDensity"] = _pines.Settings.Density,
            ["PineHeightMultiplier"] = _pines.Settings.HeightMultiplier,
            ["PineWindStrength"] = _pines.Settings.LeafWindStrength,
            ["GrassEnabled"] = _grass.Settings.Enabled,
            ["GrassDensity"] = _grass.Settings.Density,
            ["GrassHeight"] = _grass.Settings.Height,
            ["GrassWindMultiplier"] = _grass.Settings.WindMultiplier,
            [nameof(_settings.ObstructionFadeEnabled)] = _settings.ObstructionFadeEnabled,
            [nameof(_settings.ObstructedOpacity)] = _settings.ObstructedOpacity,
            [nameof(_settings.ObstructionRadius)] = _settings.ObstructionRadius,
            [nameof(_settings.ObstructionChecksPerSecond)] = _settings.ObstructionChecksPerSecond,
            [nameof(_settings.ObstructionFadeSeconds)] = _settings.ObstructionFadeSeconds,
            [nameof(_settings.UsePerspective)] = _settings.UsePerspective,
            [nameof(_settings.PerspectiveFov)] = _settings.PerspectiveFov,
            [nameof(_settings.CameraPitchDegrees)] = _settings.CameraPitchDegrees,
            [nameof(_settings.CameraYawDegrees)] = _settings.CameraYawDegrees,
            [nameof(_settings.CameraSize)] = _settings.CameraSize,
            [nameof(_settings.CameraDistance)] = _settings.CameraDistance,
            [nameof(_settings.MinimumZoom)] = _settings.MinimumZoom,
            [nameof(_settings.MaximumZoom)] = _settings.MaximumZoom,
            [nameof(_settings.MinimumPerspectiveDistance)] = _settings.MinimumPerspectiveDistance,
            [nameof(_settings.MaximumPerspectiveDistance)] = _settings.MaximumPerspectiveDistance,
            [nameof(_settings.ZoomStep)] = _settings.ZoomStep,
            [nameof(_settings.FollowSmoothing)] = _settings.FollowSmoothing,
            [nameof(_environment.StartHour)] = _sky.Sky.Get("current_time").AsSingle(),
            [nameof(_environment.StartPaused)] = _environment.StartPaused,
            [nameof(_environment.DayLengthMinutes)] = _environment.DayLengthMinutes,
            [nameof(_environment.WeatherEnabled)] = _environment.WeatherEnabled,
            [nameof(_environment.AirborneIntensity)] = _environment.AirborneIntensity,
            [nameof(_environment.RainIntensity)] = _environment.RainIntensity,
            [nameof(_environment.SnowIntensity)] = _environment.SnowIntensity,
            [nameof(_environment.LocalMistIntensity)] = _environment.LocalMistIntensity,
            [nameof(_environment.GrassWindStrength)] = _environment.GrassWindStrength,
            [nameof(_environment.WindHeading)] = _environment.WindHeading,
            [nameof(_environment.WeatherGusts)] = _environment.WeatherGusts,
            [nameof(_environment.WeatherTurbulence)] = _environment.WeatherTurbulence,
            [nameof(_environment.WeatherParticleSize)] = _environment.WeatherParticleSize,
            [nameof(_environment.WeatherFallSpeed)] = _environment.WeatherFallSpeed,
            [nameof(_environment.WeatherQuality)] = _environment.WeatherQuality
        };
        if (_renderCulling != null)
        {
            values["WS1Enabled"] = _renderCulling.Settings.Enabled;
            values["WS1HiddenBuffer"] = _renderCulling.Settings.HiddenBuffer;
            values["WS1ShadowBuffer"] = _renderCulling.Settings.ShadowBuffer;
        }
        _view?.WriteSnapshot(values);
        DisplayServer.ClipboardSet("War Machine · World camera and environment settings\n"
            + JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
        _copy.Text = "Copied! Paste these settings into chat";
    }

    // =========================================================
    // Remove the external subscription when the scene or panel unloads.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_clock) && _clock.IsConnected("time_changed", _timeChanged))
            _clock.Disconnect("time_changed", _timeChanged);
        if (_visionDebug != null && IsInstanceValid(_visionDebug)) _visionDebug.VisibilityChanged -= OnVisionDebugChanged;
        if (_view == null && GodotObject.IsInstanceValid(_camera)) _camera.SettingsChanged -= Refresh;
    }
}
