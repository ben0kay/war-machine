// Editable, world-independent dimensions and visual defaults for the reusable parking apron.
using Godot;

[Tool]
[GlobalClass]
public partial class ParkingLotSettings : Resource
{
    [ExportGroup("Asphalt foundation")]
    [Export(PropertyHint.Range, "34,60,0.5")] public float Width { get; set; } = 40f;
    [Export(PropertyHint.Range, "24,42,0.5")] public float Depth { get; set; } = 28f;
    [Export(PropertyHint.Range, "0.06,0.25,0.01")] public float AsphaltThickness { get; set; } = 0.12f;

    [ExportGroup("Parking bays")]
    [Export(PropertyHint.Range, "1,10,1")] public int BayCount { get; set; } = 8;
    [Export(PropertyHint.Range, "2.6,3.6,0.1")] public float BayWidth { get; set; } = 3.1f;
    [Export(PropertyHint.Range, "4.8,6.5,0.1")] public float BayDepth { get; set; } = 5.6f;
    [Export(PropertyHint.Range, "0.04,0.15,0.01")] public float LineWidth { get; set; } = 0.08f;

    [ExportGroup("Walkway and curbs")]
    [Export(PropertyHint.Range, "1.6,3.5,0.1")] public float SidewalkDepth { get; set; } = 2.4f;
    [Export(PropertyHint.Range, "0.1,0.24,0.01")] public float SidewalkHeight { get; set; } = 0.15f;
    [Export(PropertyHint.Range, "2.0,4.5,0.1")] public float EntranceGapWidth { get; set; } = 3.2f;

    [ExportGroup("Road approach")]
    [Export(PropertyHint.Range, "5.5,12,0.5")] public float DrivewayWidth { get; set; } = 8f;
    [Export(PropertyHint.Range, "3,12,0.5")] public float DrivewayLength { get; set; } = 6f;

    [ExportGroup("Colours")]
    [Export] public Color AsphaltColor { get; set; } = new(0.17f, 0.19f, 0.20f);
    [Export] public Color AsphaltWearColor { get; set; } = new(0.21f, 0.22f, 0.22f);
    [Export] public Color ConcreteColor { get; set; } = new(0.52f, 0.52f, 0.48f);
    [Export] public Color CurbColor { get; set; } = new(0.36f, 0.38f, 0.36f);
    [Export] public Color LineColor { get; set; } = new(0.72f, 0.70f, 0.55f);
}
