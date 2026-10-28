// CM2-A: reusable melee attack profile. The attacker supplies aim and controls active timing.
// This resource never processes frames or depends on a player/camera controller.
using Godot;

[GlobalClass]
public partial class MeleeSettings : Resource
{
    [ExportGroup("Contact")]
    [Export(PropertyHint.Range, "0.25,5,0.05")] public float Range { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "10,180,1")] public float ArcDegrees { get; set; } = 65f;
    [Export(PropertyHint.Range, "0.25,3,0.05")] public float VerticalTolerance { get; set; } = 1.25f;
    [Export(PropertyHint.Layers3DPhysics)] public uint HitMask { get; set; } = 21;
    [ExportGroup("Sweep")]
    [Export(PropertyHint.Range, "1,8,1")] public int MaximumTargets { get; set; } = 3;
    [Export(PropertyHint.Range, "0.01,0.5,0.01")] public float SweepThickness { get; set; } = 0.20f;
    [Export] public float SeverPower { get; set; }
    [ExportGroup("Melee presentation")]
    [Export] public Vector3 FirstPersonOffset { get; set; } = new(0.20f, -0.28f, -0.38f);
    [Export(PropertyHint.Range, "0,85,1")] public float HoldPitchDegrees { get; set; } = 49f;
    [Export(PropertyHint.Range, "-90,90,1")] public float HoldYawDegrees { get; set; } = -14f;
    [ExportGroup("First-person blunt sweep")]
    [Export(PropertyHint.Range, "0.1,1.2,0.05")] public float FirstPersonWindupLeft { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.1,1.2,0.05")] public float FirstPersonFollowRight { get; set; } = 0.70f;
    [Export(PropertyHint.Range, "0,135,1")] public float FirstPersonSideTiltDegrees { get; set; } = 90f;
    [ExportGroup("Attack timing")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float WindupSeconds { get; set; } = 0.10f;
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float ActiveSeconds { get; set; } = 0.12f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RecoverySeconds { get; set; } = 0.20f;
    [Export(PropertyHint.Range, "0.05,3,0.01")] public float CooldownSeconds { get; set; } = 0.55f;
    [ExportGroup("Future hit dispatch")]
    [Export] public float Damage { get; set; } = 0f;
    [Export] public DamageType DamageKind { get; set; } = DamageType.Physical;
    [Export] public CombatImpactKind ImpactKind { get; set; } = CombatImpactKind.Shove;
    [Export(PropertyHint.Range, "0,10,0.05")] public float ImpactStrength { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ImpactDuration { get; set; } = 0.40f;
    [ExportGroup("Quick shove pose")]
    [Export(PropertyHint.Range, "60,100,1")] public float ShoveSidewaysDegrees { get; set; } = 90f;
    [Export(PropertyHint.Range, "0,0.25,0.01")] public float ShoveExtension { get; set; } = 0.12f;
    [ExportGroup("Quick shove momentum")]
    [Export(PropertyHint.Range, "0,5,0.05")] public float StationaryStrengthMultiplier { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0,5,0.05")] public float RunningStrengthMultiplier { get; set; } = 2.25f;
    [Export(PropertyHint.Range, "0.1,20,0.1")] public float FullMomentumSpeed { get; set; } = 8f;
}
