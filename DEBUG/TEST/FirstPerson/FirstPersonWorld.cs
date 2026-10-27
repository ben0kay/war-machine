// Test-only composition overrides. Terrain, props, zombies, robot, weather and damage stay in their shared owners.
using Godot;

public partial class FirstPersonWorld : WorldController
{
    protected override string ControlsHelp => "FIRST PERSON TEST · arrow-key walk · Shift run · Right Ctrl crouch\nMouse look · Left click/hold fire · Esc release mouse · Click recapture · F9 fog/render controls";

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
        var fog = new FirstPersonFog { Name = "FirstPersonFog" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(fog);
        fog.Initialize((FirstPersonCamera)camera, player, EnvironmentSettings, DebugControls);
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
    }

    // =========================================================
    // Grass uses its own close-range settings and camera-origin fade; overhead footprint expansion is bypassed.
    protected override void UpdateWeatherCoverage()
    { _weather.SetOrthographicCoverage(24); _pines.UpdateView(0); }
}
