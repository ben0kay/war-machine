// FPS presentation adapters; world, shared Environment scene and gameplay remain inherited.
using Godot;

public partial class FirstPersonWorld : WorldController
{
    private FirstPersonFog _fpsFog = null!;
    protected override string ControlsHelp => "FIRST PERSON TEST · arrows walk · Shift run · Right Ctrl crouch\nMouse look · Left click fire · Esc: use left debug controls · Click world: resume";
    protected override bool EnableHorde => false;
    protected override IDebugOverlayView? OverlayView => _fpsFog;

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
        base.InitializeControls(camera);
    }

    // =========================================================
    // Retain FPS-specific emitter coverage and close foliage profile.
    protected override void UpdateWeatherCoverage()
    { _weather.SetOrthographicCoverage(24); _pines.UpdateView(0); }
}
