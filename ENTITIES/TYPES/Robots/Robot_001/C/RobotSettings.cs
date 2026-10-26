// Robot_001 art and gait defaults; runtime copies protect the saved master.
using Godot;

[GlobalClass]
public partial class RobotSettings : Resource
{
    [ExportGroup("Art")]
    [Export(PropertyHint.Range, "0.25,4,0.05")] public float ModelScale { get; set; } = 1;
    [Export] public Vector3 BodySize { get; set; } = new(4.6f, 2, 5.6f);
    [Export] public float HipHeight { get; set; } = 7;
    [Export] public float UpperLegLength { get; set; } = 4.5f;
    [Export] public float LowerLegLength { get; set; } = 4.5f;
    [Export] public float StanceWidth { get; set; } = 4.5f;
    [Export] public Color ArmourColor { get; set; } = new(0.075f, 0.095f, 0.11f);
    [Export] public Color JointColor { get; set; } = new(0.19f, 0.22f, 0.24f);
    [Export] public Color SensorColor { get; set; } = new(0.08f, 0.8f, 0.85f);
    [Export] public float EmissionEnergy { get; set; } = 1.2f;
    [Export] public PackedScene? ArtOverride { get; set; }
    [ExportGroup("Gait")]
    [Export] public float StrideLength { get; set; } = 2.4f;
    [Export] public float GaitSpeed { get; set; } = 0.55f;
    [Export] public float LegLift { get; set; } = 1.2f;
    [Export(PropertyHint.Range, "0.5,0.9,0.01")] public float StanceFraction { get; set; } = 0.65f;
    [Export] public float BodyBob { get; set; } = 0.14f;
    [Export] public float BodyPitchDegrees { get; set; } = 1.4f;
    [Export] public float SettleSeconds { get; set; } = 0.4f;
    [ExportGroup("Head")]
    [Export] public float HeadYawLimitDegrees { get; set; } = 60;
    [ExportGroup("Preview")]
    [Export] public bool PreviewWalking { get; set; }
}
