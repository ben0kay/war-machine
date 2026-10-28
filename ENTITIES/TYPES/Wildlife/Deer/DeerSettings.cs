// Deer-specific wander, flight, health and art tuning; shared helpers own ground/visibility checks.
using Godot;

[GlobalClass]
public partial class DeerSettings : EntitySettings
{
    [ExportGroup("Health")]
    [Export] public HealthSettings HealthProfile { get; set; } = null!;

    [ExportGroup("Wandering")]
    [Export(PropertyHint.Range, "0,30,0.5")] public float WanderRadius { get; set; } = 9f;
    [Export(PropertyHint.Range, "0.2,4,0.1")] public float WanderSpeed { get; set; } = 1.2f;
    [Export(PropertyHint.Range, "0.5,8,0.1")] public float WaitMinSeconds { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.5,10,0.1")] public float WaitMaxSeconds { get; set; } = 3.5f;

    [ExportGroup("Flight")]
    [Export(PropertyHint.Range, "2,35,0.5")] public float DetectionRange { get; set; } = 13f;
    [Export(PropertyHint.Range, "3,40,0.5")] public float FleeDistance { get; set; } = 18f;
    [Export(PropertyHint.Range, "2,14,0.5")] public float FleeSpeed { get; set; } = 7.5f;
    [Export(PropertyHint.Range, "0,360,5")] public float VisionAngleDegrees { get; set; } = 300f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float EyeHeight { get; set; } = 1.75f;
    [Export(PropertyHint.Range, "0.15,2,0.05")] public float DecisionIntervalSeconds { get; set; } = 0.4f;
    [Export(PropertyHint.Layers3DPhysics)] public uint SightBlockerMask { get; set; } = 17;

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "1,20,0.5")] public float Acceleration { get; set; } = 9f;
    [Export(PropertyHint.Range, "1,30,0.5")] public float Braking { get; set; } = 14f;
    [Export(PropertyHint.Range, "45,720,5")] public float TurnDegreesPerSecond { get; set; } = 300f;

    [ExportGroup("Appearance")]
    [Export] public Color CoatColor { get; set; } = new(0.42f, 0.31f, 0.22f);
    [Export] public Color BellyColor { get; set; } = new(0.69f, 0.63f, 0.52f);
    [Export] public Color HoofColor { get; set; } = new(0.15f, 0.13f, 0.11f);
    [Export] public Color AntlerColor { get; set; } = new(0.43f, 0.33f, 0.22f);
    [Export] public bool HasAntlers { get; set; } = false;
}
