// Reusable slow wander/chase defaults; each zombie receives an isolated runtime copy through EntityCore.
using Godot;

[GlobalClass]
public partial class ZombieSettings : EntitySettings
{
    [ExportGroup("Health")]
    [Export] public HealthSettings HealthProfile { get; set; } = null!;
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
