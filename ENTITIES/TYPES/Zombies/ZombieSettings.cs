// Reusable slow wander/chase defaults; each zombie receives an isolated runtime copy through EntityCore.
using Godot;

[GlobalClass]
public partial class ZombieSettings : EntitySettings
{
    [ExportGroup("Health")]
    [Export] public HealthSettings HealthProfile { get; set; } = null!;
    [ExportGroup("Bullet hit reaction")]
    [Export] public bool BulletReactionsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ImpactResistance { get; set; }
    [Export] public float FlinchThreshold { get; set; } = 0.2f;
    [Export] public float MaxStaggerSeconds { get; set; } = 0.3f;
    [Export] public float MinFlinchInterval { get; set; } = 0.45f;
    [Export] public float MinRecoverySeconds { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float StaggerMoveMultiplier { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ImpactVelocityRetention { get; set; } = 0.1f;
    [Export] public float StaggerBrakingAcceleration { get; set; } = 60;
    [Export] public float RecoveryAcceleration { get; set; } = 12;
    [Export] public float RecoverySeconds { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,20,0.5")] public float FlinchDegrees { get; set; } = 10;
    [ExportGroup("Movement")]
    [Export] public float WanderSpeed { get; set; } = 0.65f;
    [Export] public float ChaseSpeed { get; set; } = 1.15f;
    [Export] public float WanderRadius { get; set; } = 10;
    [ExportGroup("Ground recovery")]
    [Export] public bool GroundRecoveryEnabled { get; set; } = true;
    [Export] public float GroundRecoveryDepth { get; set; } = 0.75f;
    [ExportGroup("Awareness")]
    [Export(PropertyHint.Range, "1,180,1")] public float VisionAngleDegrees { get; set; } = 160;
    [Export] public float EyeHeight { get; set; } = 1.6f;
    [Export] public float SightIntervalSeconds { get; set; } = 0.4f;
    [Export(PropertyHint.Layers3DPhysics)] public uint SightBlockerMask { get; set; } = 17;
    [ExportGroup("Vision debug")]
    [Export] public bool ShowVisionCone { get; set; } = true;
    [ExportGroup("Awareness")]
    [Export] public float DetectionRange { get; set; } = 18;
    [Export] public float LoseRange { get; set; } = 23;
    [Export] public float StopDistance { get; set; } = 1.3f;
}
