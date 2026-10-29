// Reusable slow wander/chase defaults; each zombie receives an isolated runtime copy through EntityCore.
using Godot;

[GlobalClass]
public partial class ZombieSettings : EntitySettings
{
    [ExportGroup("Melee attacks (CM5)")]
    [Export] public bool AttacksEnabled { get; set; } = true;
    [Export] public MeleeSettings? AttackProfile { get; set; }
    [ExportGroup("Attack lunge pose (visual only)")]
    [Export(PropertyHint.Range, "0,30,1")] public float AttackTorsoRollDegrees { get; set; } = 14f;
    [Export(PropertyHint.Range, "0,25,1")] public float AttackTorsoTwistDegrees { get; set; } = 18f;
    [Export(PropertyHint.Range, "0,35,1")] public float AttackForwardLungeDegrees { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,20,1")] public float AttackHeadSwayDegrees { get; set; } = 7f;
    [Export(PropertyHint.Range, "0,0.18,0.01")] public float AttackSideShift { get; set; } = 0.075f;
    [Export(PropertyHint.Range, "30,140,5")] public float AttackHorizontalSwipeDegrees { get; set; } = 110f;
    [Export(PropertyHint.Range, "40,100,5")] public float AttackSwipeArmPitchDegrees { get; set; } = 78f;
    [ExportGroup("Injury locomotion")]
    [Export] public ZombieLocomotionSettings? LocomotionProfile { get; set; }
    [ExportGroup("Anatomy")]
    [Export] public AnatomySettings? AnatomyProfile { get; set; }
    [ExportGroup("Health")]
    [Export] public HealthSettings HealthProfile { get; set; } = null!;
    [ExportGroup("Appearance")]
    [Export] public bool ImportedArtworkEnabled { get; set; } = true;
    [Export] public bool AppearanceVariationEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float BloodChance { get; set; } = 0.45f;
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
    [ExportGroup("Shove stagger (CM2-B1)")]
    [Export] public bool ShoveReactionsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShoveResistance { get; set; }
    [Export(PropertyHint.Range, "0,10,0.05")] public float ShoveFlinchThreshold { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShoveMaxStaggerSeconds { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0,20,0.1")] public float ShovePushSpeed { get; set; } = 4f;
    [Export(PropertyHint.Range, "0,20,0.1")] public float ShoveMaxPushSpeed { get; set; } = 10f;
    [Export(PropertyHint.Range, "0,40,0.5")] public float ShoveBrakingAcceleration { get; set; } = 9f;
    [ExportGroup("Damaging melee backstep")]
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float MeleeMaxStaggerSeconds { get; set; } = 0.55f;
    [ExportGroup("Movement")]
    [Export] public float WanderSpeed { get; set; } = 0.78f;
    [Export] public float ChaseSpeed { get; set; } = 1.38f;
    [Export] public float WanderRadius { get; set; } = 10;
    [ExportGroup("Ground recovery")]
    [Export] public bool GroundRecoveryEnabled { get; set; } = true;
    [Export] public float GroundRecoveryDepth { get; set; } = 0.75f;
    [ExportGroup("Awareness")]
    [Export(PropertyHint.Range, "1,180,1")] public float VisionAngleDegrees { get; set; } = 160;
    [Export] public float EyeHeight { get; set; } = 1.6f;
    [Export] public float SightIntervalSeconds { get; set; } = 0.4f;
    [Export(PropertyHint.Layers3DPhysics)] public uint SightBlockerMask { get; set; } = 17;
    [ExportGroup("Visual mesh LOD (ZM-PERF2B)")]
    [Export] public bool MeshLodEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,4,0.1")] public float MeshLodBias { get; set; } = 1f;
    [ExportGroup("Gunshot hearing (HE1)")]
    [Export] public bool HearingEnabled { get; set; } = true;
    [Export] public float MinimumAudibleIntensity { get; set; } = 0.08f;
    [Export] public Vector2 SoundUncertaintyDistances { get; set; } = new(10, 110);
    [Export] public Vector2 SoundUncertaintyRadii { get; set; } = new(1, 18);
    [Export] public Vector3 SoundNearCommitmentWeights { get; set; } = new(0.05f, 0.15f, 0.80f);
    [Export] public Vector3 SoundFarCommitmentWeights { get; set; } = new(0.80f, 0.18f, 0.02f);
    [Export] public Vector2 SoundResponseChance { get; set; } = new(1, 0.25f);
    [Export] public Vector2 SoundReactionDelaySeconds { get; set; } = new(0.15f, 1.2f);
    [Export] public float SoundRetargetCooldownSeconds { get; set; } = 6;
    [Export(PropertyHint.Layers3DPhysics)] public uint SoundBlockerMask { get; set; } = 17;
    [Export(PropertyHint.Range, "0,1,0.05")] public float SoundBlockedEarlyBias { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float SoundBlockedTimeoutMultiplier { get; set; } = 0.35f;
    [Export] public float SoundStrongerRatio { get; set; } = 1.25f;
    [Export] public float SoundNewSourceDistance { get; set; } = 12;
    [ExportGroup("Investigation triggers")]
    [Export] public bool HitInvestigationEnabled { get; set; } = true;
    [ExportGroup("Last seen investigation")]
    [Export] public bool LastSeenInvestigationEnabled { get; set; } = true;
    // X early, Y moderate, Z thorough. Nonnegative weights are normalized per episode.
    [Export] public Vector3 InvestigationWeights { get; set; } = new(0.40f, 0.35f, 0.25f);
    [Export] public Vector2 EarlyInvestigationFraction { get; set; } = new(0.40f, 0.60f);
    [Export] public Vector2 ModerateInvestigationFraction { get; set; } = new(0.65f, 0.90f);
    [Export] public Vector2 InvestigationTimeScale { get; set; } = new(1.10f, 1.60f);
    [Export(PropertyHint.Range, "0.5,20,0.5")] public float InvestigationTimeAllowance { get; set; } = 3f;
    [Export(PropertyHint.Range, "1,120,1")] public float InvestigationMaxSeconds { get; set; } = 60f;
    [Export(PropertyHint.Range, "0.2,1.5,0.05")] public float InvestigationArrivalDistance { get; set; } = 0.45f;
    [Export] public Vector2 InvestigationLookSeconds { get; set; } = new(0.8f, 2f);
    [ExportGroup("Vision debug")]
    [Export] public bool ShowVisionCone { get; set; } = true;
    [ExportGroup("Awareness")]
    [Export] public float DetectionRange { get; set; } = 22.5f;
    [Export] public float LoseRange { get; set; } = 28.75f;
    [Export] public float StopDistance { get; set; } = 1.3f;
    // =========================================================
    // Preserve existing authored .tres property names/values while providing a species-independent runtime policy.
    public InvestigationSettings CreateInvestigationSettings() => new()
    {
        Weights = InvestigationWeights, EarlyFraction = EarlyInvestigationFraction,
        ModerateFraction = ModerateInvestigationFraction, TimeScale = InvestigationTimeScale,
        TimeAllowance = InvestigationTimeAllowance, MaximumSeconds = InvestigationMaxSeconds,
        LookSeconds = InvestigationLookSeconds
    };
}
