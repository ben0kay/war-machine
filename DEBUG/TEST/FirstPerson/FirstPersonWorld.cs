// Test-only composition overrides. Terrain, props, zombies, robot, weather and damage stay in their shared owners.
using Godot;

public partial class FirstPersonWorld : WorldController
{
    private FirstPersonFog? _fpsFog;
    private FirstPersonWeatherPanel? _weatherPanel;
    private FirstPersonCamera? _fpsCamera;
    private bool _restoreCapture;
    protected override string ControlsHelp => "FIRST PERSON TEST · arrow-key walk · Shift run · Right Ctrl crouch\nMouse look · Left click/hold fire · Esc release mouse · Click recapture · F8 weather/time · F9 fog/render";
    protected override bool EnableHorde => false; // Extra population belongs only to the top-down horde test.

    // =========================================================
    protected override void InitializeEquipment(PlayerController player, Camera camera)
    {
        var weapon = new FirstPersonWeapon { Name = "FirstPersonEquipment" }; player.AddChild(weapon);
        weapon.Initialize(player, (FirstPersonCamera)camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"));
    }

    // =========================================================
    // No camera obstruction queries or scenery fading component exists in the FPS test.
    protected override void InitializeOcclusion(Camera camera, PlayerController player) { }

    // =========================================================
    // FPS uses camera-distance fog and a linked render cutoff, without changing the overhead profile.
    protected override void InitializeDistanceFog(Camera camera, PlayerController player)
    {
        _fpsFog = new FirstPersonFog { Name = "FirstPersonFog" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(_fpsFog);
        _fpsFog.Initialize((FirstPersonCamera)camera, player, EnvironmentSettings, DebugControls);
    }

    // =========================================================
    // Keep overhead tuning controls out of mouse-look input; a tiny non-interactive crosshair is sufficient here.
    protected override void InitializeControls(Camera camera)
    {
        var crosshair = new Label { Text = "+", MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        DebugControls.AddChild(crosshair);
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.OffsetLeft = -10; crosshair.OffsetRight = 10; crosshair.OffsetTop = -14; crosshair.OffsetBottom = 14;
        _fpsCamera = (FirstPersonCamera)camera;
        _weatherPanel = new FirstPersonWeatherPanel { Name = "FirstPersonWeatherPanel" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(_weatherPanel);
        _weatherPanel.Initialize(GetNode<SkyController>("SystemsRoot/EnvironmentRoot/Environment"),
            _weather, _grass, _pines, DebugControls);
    }

    // =========================================================
    // F8/F9 are mutually exclusive; switching preserves the original mouse-capture state.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (_weatherPanel == null || _fpsFog == null || _fpsCamera == null
            || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.PhysicalKeycode != Key.F8 && key.PhysicalKeycode != Key.F9) return;
        bool weather = key.PhysicalKeycode == Key.F8;
        bool wasOpen = _weatherPanel.IsOpen || _fpsFog.IsOpen;
        bool open = !(weather ? _weatherPanel.IsOpen : _fpsFog.IsOpen);
        if (!wasOpen && open) _restoreCapture = Input.MouseMode == Input.MouseModeEnum.Captured;
        ShowDebugPanel(weather && open, !weather && open);
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Prevent panel overlap, outside-click recapture and accidental shots while tuning.
    private void ShowDebugPanel(bool weather, bool fog)
    {
        _weatherPanel!.SetPanelVisible(weather);
        _fpsFog!.SetPanelVisible(fog);
        bool open = weather || fog;
        _fpsCamera!.DebugControlsOpen = open;
        if (open) Input.MouseMode = Input.MouseModeEnum.Visible;
        else
        {
            if (_restoreCapture && GetWindow().HasFocus()) Input.MouseMode = Input.MouseModeEnum.Captured;
            _restoreCapture = false;
        }
    }

    // =========================================================
    // Escape closes the current panel without recapturing; focus loss also cancels recapture.
    public override void _Input(InputEvent input)
    {
        if ((_weatherPanel?.IsOpen != true && _fpsFog?.IsOpen != true)
            || input is not InputEventKey key || !key.Pressed || key.Echo || key.PhysicalKeycode != Key.Escape) return;
        _restoreCapture = false;
        ShowDebugPanel(false, false);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    public override void _Notification(int what)
    { if (what == NotificationApplicationFocusOut) _restoreCapture = false; }

    // =========================================================
    // Grass uses its own close-range settings and camera-origin fade; overhead footprint expansion is bypassed.
    protected override void UpdateWeatherCoverage()
    { _weather.SetOrthographicCoverage(24); _pines.UpdateView(0); }
}
