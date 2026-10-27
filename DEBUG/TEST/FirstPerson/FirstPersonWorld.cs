// Test-only composition overrides. Terrain, props, zombies, robot, weather and damage stay in their shared owners.
using Godot;

public partial class FirstPersonWorld : WorldController
{
    protected override string ControlsHelp => "FIRST PERSON TEST · WASD walk · Shift run · Right Ctrl crouch\nMouse look · Left click/hold fire · Esc release mouse · Click recapture";

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
    // Keep overhead tuning controls out of mouse-look input; a tiny non-interactive crosshair is sufficient here.
    protected override void InitializeControls(Camera camera)
    {
        var crosshair = new Label { Text = "+", MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        GetNode<CanvasLayer>("Controls").AddChild(crosshair);
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.OffsetLeft = -10; crosshair.OffsetRight = 10; crosshair.OffsetTop = -14; crosshair.OffsetBottom = 14;
    }

    // =========================================================
    // Grass uses its own close-range settings and camera-origin fade; overhead footprint expansion is bypassed.
    protected override void UpdateWeatherCoverage()
    { _weather.SetOrthographicCoverage(24); _pines.UpdateView(0); }
}
