// DOOR1 shared physical-door master. Independent from pickup/props interaction and building masters.
using Godot;

[GlobalClass]
public partial class HingedDoorSettings : Resource
{
    [ExportGroup("Door geometry")]
    [Export(PropertyHint.Range, "0.65,2,0.01")] public float Width { get; set; } = 1.38f;
    [Export(PropertyHint.Range, "1.85,2.85,0.01")] public float Height { get; set; } = 2.60f;
    [Export(PropertyHint.Range, "0.035,0.16,0.005")] public float Thickness { get; set; } = 0.075f;
    [Export(PropertyHint.Range, "0.04,0.3,0.01")] public float BottomClearance { get; set; } = 0.18f;

    [Export(PropertyHint.Range, "0.001,0.02,0.001")] public float CollisionMargin { get; set; } = 0.005f;

    [ExportGroup("Hinge physics")]
    [Export(PropertyHint.Range, "1,50,0.5")] public float MassKg { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,12,0.05")] public float AngularDamping { get; set; } = 0.40f;
    [Export(PropertyHint.Range, "25,120,5")] public float SwingLimitDegrees { get; set; } = 110f;
    [Export(PropertyHint.Range, "0,150,1")] public float CharacterContactForce { get; set; } = 65f;

    [ExportGroup("Contact speed response")]
    [Export(PropertyHint.Range, "1,10,0.25")] public float ContactSpeedReference { get; set; } = 3.5f;
    [Export(PropertyHint.Range, "1,3,0.05")] public float ContactSpeedExponent { get; set; } = 1.65f;
    [Export(PropertyHint.Range, "1,4,0.1")] public float MaxContactSpeedMultiplier { get; set; } = 3f;

    [ExportGroup("Handle & finish")]
    [Export(PropertyHint.Range, "0.65,1.55,0.05")] public float HandleHeight { get; set; } = 1.15f;
    [Export] public Color PanelColor { get; set; } = new(0.26f, 0.33f, 0.36f);
    [Export] public Color HandleColor { get; set; } = new(0.58f, 0.59f, 0.54f);
}
