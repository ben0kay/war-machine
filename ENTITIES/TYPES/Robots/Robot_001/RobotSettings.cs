// Robot_001 art and gait defaults; runtime copies protect the saved master.
using Godot;

[GlobalClass]
public partial class RobotSettings : Resource
{
    [ExportGroup("Art")]
    [Export(PropertyHint.Range, "0.25,4,0.05")] public float ModelScale { get; set; } = 1;
    [Export] public Vector3 BodySize { get; set; } = new(4.8f, 2.2f, 3.2f);
    [Export] public float HipHeight { get; set; } = 8.0f;
    [Export] public float UpperLegLength { get; set; } = 4.4f;
    [Export] public float LowerLegLength { get; set; } = 4.4f;
    [Export] public float StanceWidth { get; set; } = 2.5f;
    [Export] public Color ArmourColor { get; set; } = new(0.14f, 0.18f, 0.21f);
    [Export] public Color JointColor { get; set; } = new(0.28f, 0.33f, 0.36f);
    [Export] public Color SensorColor { get; set; } = new(0.04f, 0.85f, 1.0f);
    [Export] public float EmissionEnergy { get; set; } = 1.8f;
    [ExportGroup("Mesh detail")]
    [Export] public bool ExtraMeshDetail { get; set; } = true;
    // Relative to existing sensor emission; small fixed highlights should remain much dimmer.
    [Export(PropertyHint.Range, "0,1.0,0.05")] public float DetailGlowMultiplier { get; set; } = 0.30f;
    [ExportGroup("MAT1 surfaces")]
    [Export] public bool ProceduralMaterialsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ArmourWear { get; set; } = 0.38f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ArmourDirt { get; set; } = 0.28f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ArmourScratches { get; set; } = 0.30f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ArmourMetallic { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ArmourRoughness { get; set; } = 0.60f;
    [Export] public PackedScene? ArtOverride { get; set; }
    [ExportGroup("Gait")]
    [Export] public float StrideLength { get; set; } = 2.4f;
    [Export] public float GaitSpeed { get; set; } = 0.55f;
    [Export] public float LegLift { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.5,0.9,0.01")] public float StanceFraction { get; set; } = 0.65f;
    [Export] public float BodyBob { get; set; } = 0.09f;
    [Export] public float BodyPitchDegrees { get; set; } = 0.8f;
    [Export] public float SettleSeconds { get; set; } = 0.4f;
    [ExportGroup("Head")]
    [Export] public float HeadYawLimitDegrees { get; set; } = 60;
    [ExportGroup("Patrol")]
    [Export] public PatrolSettings Patrol { get; set; } = new();
    [ExportGroup("Scanning")]
    [Export] public ScanSensorSettings Sensor { get; set; } = new();
    [Export] public float ScanSeconds { get; set; } = 6;
    [Export] public float RecoverySeconds { get; set; } = 0.8f;
    [Export] public float SweepDegrees { get; set; } = 120;
    [Export] public float SweepCyclesPerSecond { get; set; } = 0.16f;
    [Export] public float ScanPitchDegrees { get; set; } = -25;
    [Export] public float BeamOpacity { get; set; } = 0.3f;
    [Export] public float BeamBrightness { get; set; } = 2.0f;
    [Export] public float LineSpeed { get; set; } = 0.8f;
    [Export] public float BandContrast { get; set; } = 1.5f;
    [Export] public bool BeamEnabled { get; set; } = true;
    [Export] public bool AnimateLegs { get; set; } = true;
    [ExportGroup("Audio")]
    [Export] public bool AudioEnabled { get; set; } = true;
    [Export] public float AudioVolumeDb { get; set; } = -18;
    [ExportGroup("Relevance")]
    [Export] public bool EncounterEnabled { get; set; } = true;
    [Export] public float WakeDistance { get; set; } = 180;
    [Export] public float SleepDistance { get; set; } = 240;
    [ExportGroup("Preview")]
    [Export] public bool PreviewWalking { get; set; }
}
