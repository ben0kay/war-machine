// FPS presentation adapters; world, shared Environment scene and gameplay remain inherited.
using Godot;

public partial class FirstPersonWorld : WorldController
{
    private FirstPersonFog _fpsFog = null!;
    protected override string ControlsHelp => "FIRST PERSON TEST · arrows walk · Shift run · Right Ctrl crouch\nMouse look · Left click fire · Esc: use left debug controls · Click world: resume";
    protected override bool EnableHorde => false;

    // =========================================================
    protected override void InitializeEquipment(PlayerController player, Camera camera)
    {
        var weapon = new FirstPersonWeapon { Name = "FirstPersonEquipment" }; player.AddChild(weapon);
        weapon.Initialize(player, (FirstPersonCamera)camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"));
    }

    // =========================================================
    // FPS keeps physical scenery opaque; atmosphere is shared through Environment.tscn.
    protected override void InitializeOcclusion(Camera camera, PlayerController player) { }

    // =========================================================
    // Only distance-fog framing/culling is view-specific; weather and ground haze are shared.
    protected override void InitializeDistanceFog(Camera camera, PlayerController player)
    {
        _fpsFog = new FirstPersonFog { Name = "FirstPersonFog" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(_fpsFog);
        _fpsFog.Initialize((FirstPersonCamera)camera, player, EnvironmentSettings);
    }

    // =========================================================
    // One always-visible left panel with nested main buttons. Escape releases mouse via the existing camera.
    protected override void InitializeControls(Camera camera)
    {
        var crosshair = new Label { Text = "+", MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        DebugControls.AddChild(crosshair);
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.OffsetLeft = -10; crosshair.OffsetRight = 10; crosshair.OffsetTop = -14; crosshair.OffsetBottom = 14;
        var panel = new PanelContainer { Name = "FPSDebugControls", Position = new Vector2(20, 90) };
        DebugControls.AddChild(panel);
        var margin = new MarginContainer(); panel.AddChild(margin);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 10);
        var outer = new VBoxContainer(); margin.AddChild(outer);
        outer.AddChild(new Label { Text = "Debug controls · Esc to use · click world to resume" });
        var root = FirstPersonTuningSections.AddScroll(outer,
            Mathf.Clamp(GetViewport().GetVisibleRect().Size.Y - 220, 180, 580));
        var weatherPanel = new FirstPersonWeatherPanel { Name = "FirstPersonWeatherPanel" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(weatherPanel);
        weatherPanel.Initialize(_environmentScene.SkyController, _weather, _grass, _pines, root, _groundHaze!);
        _fpsFog.BuildControls(weatherPanel.EnvironmentRows, root);
    }

    // =========================================================
    // Retain FPS-specific emitter coverage and close foliage profile.
    protected override void UpdateWeatherCoverage()
    { _weather.SetOrthographicCoverage(24); _pines.UpdateView(0); }
}
