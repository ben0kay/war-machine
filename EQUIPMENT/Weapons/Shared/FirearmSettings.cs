// Shared firearm definition. Duplicate at equip time; all per-weapon balance lives in a .tres.
using Godot;

[GlobalClass]
public partial class FirearmSettings : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Firearm";
    [ExportGroup("Firing")]
    [Export(PropertyHint.Range, "0.03,5,0.01")] public float ShotInterval { get; set; } = 0.65f;
    [Export] public bool Automatic { get; set; } = true;
    [Export(PropertyHint.Range, "0,20,0.1")] public float StaminaCostPerShot { get; set; } = 0.5f;
    [Export] public float Damage { get; set; } = 95;
    [Export(PropertyHint.Range, "0,10,0.05")] public float SeverPower { get; set; }
    [Export] public DamageType DamageKind { get; set; } = DamageType.Physical;
    [Export] public float Range { get; set; } = 90;
    [ExportGroup("Ammunition & reload (AM0)")]
    [Export(PropertyHint.Range, "1,1000,1")] public int MagazineCapacity { get; set; } = 30;
    [Export(PropertyHint.Range, "0.1,20,0.1,suffix:s")] public float ReloadSeconds { get; set; } = 2.2f;
    [Export] public bool AutoReloadWhenEmpty { get; set; } = true;
    // Debug means unlimited RESERVE, not unlimited magazine: reload timing still applies.
    [Export] public bool DebugUnlimitedReserveAmmo { get; set; } = true;
    [Export(PropertyHint.Range, "0,1000000,1")] public int InitialReserveRounds { get; set; }
    [ExportGroup("Accuracy")]
    // Maximum half-angle in degrees. Zero means perfect accuracy; shot-only deviation.
    [Export(PropertyHint.Range, "0,10,0.05")] public float BaseSpreadDegrees { get; set; } = 0f;
    [ExportGroup("Gameplay recoil (RC0)")]
    // These values are independent of per-shot random BaseSpreadDegrees.
    [Export(PropertyHint.Range, "0,15,0.05")] public float RecoilPitchPerShotDegrees { get; set; }
    [Export(PropertyHint.Range, "0,30,0.1")] public float RecoilMaxPitchDegrees { get; set; }
    [Export(PropertyHint.Range, "0,5,0.01")] public float RecoilYawStepDegrees { get; set; }
    [Export(PropertyHint.Range, "0,15,0.05")] public float RecoilMaxYawDegrees { get; set; }
    [Export(PropertyHint.Range, "0,180,0.5")] public float RecoilRecoveryDegreesPerSecond { get; set; } = 12f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float RecoilRecoveryDelaySeconds { get; set; } = 0.1f;
    [ExportGroup("Visual recoil (RC1/RC2)")]
    [Export(PropertyHint.Range, "0,0.2,0.005")] public float VisualRecoilKickMeters { get; set; }
    [Export(PropertyHint.Range, "0,20,0.1")] public float VisualRecoilPitchDegrees { get; set; }
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float VisualRecoilRecoverySeconds { get; set; } = 0.2f;
    [ExportGroup("Firing collision")]
    [Export(PropertyHint.Layers3DPhysics)] public uint HitMask { get; set; } = 21;
    [ExportGroup("Bullet impact")]
    [Export(PropertyHint.Range, "0,3,0.05")] public float BulletImpactStrength { get; set; }
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float BulletStaggerSeconds { get; set; }
    [ExportGroup("Tracer visuals")]
    // Visual cap only; hitscan impact is immediate and unaffected by tracer travel.
    [Export(PropertyHint.Range, "0.03,0.6,0.01")] public float EffectDuration { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "1,16,1")] public int TracerPoolSize { get; set; } = 1;
    [Export(PropertyHint.Range, "80,1000,10,suffix:m/s")] public float TracerSpeed { get; set; } = 300f;
    [Export(PropertyHint.Range, "0.2,3,0.05,suffix:m")] public float TracerLength { get; set; } = 1.2f;
    [Export(PropertyHint.Range, "0.005,0.08,0.001")] public float TracerWidth { get; set; } = 0.014f;
    // Render one tracer per N accepted shots; every shot still damages and flashes.
    [Export(PropertyHint.Range, "1,8,1")] public int TracerEveryNShots { get; set; } = 1;
    [Export] public Color ShotColor { get; set; } = new(1, 0.8f, 0.35f);

    [ExportGroup("Muzzle feedback")]
    [Export(PropertyHint.Range, "0.01,0.25,0.01")] public float FlashDuration { get; set; } = 0.07f;
    [Export] public float FlashRadius { get; set; } = 0.08f;
    [Export] public bool EmitMuzzleLight { get; set; }
    [Export] public AudioCue? ShotCue { get; set; }
    // Optional shared emission profile; absent preserves legacy playback without adding gameplay noise.
    [Export] public SoundEmissionSettings? ShotEmission { get; set; }
    // Legacy stream remains available for unmigrated weapons; pooled playback is required when ShotCue is set.
    [Export] public AudioStream? ShotSound { get; set; }
    [Export] public float ShotVolumeDb { get; set; } = -12;

    [ExportGroup("First-person presentation")]
    // FPS-only local adjustment relative to FirstPersonMaster. Does not change overhead weapon mounts.
    [Export] public Vector3 FirstPersonOffset { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "0.4,1.5,0.01")] public float FirstPersonScale { get; set; } = 1f;
    [ExportGroup("Carried weapon movement (FPS only, ADS1)")]
    // Additional movement penalty while the firearm is equipped, even outside ADS.
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float EquippedMovementMultiplier { get; set; } = 1f;
    [ExportGroup("Aim down sights (FPS only, ADS0)")]
    // Local offset added to the hip-fire root. Moves the real Muzzle together with the model.
    [Export] public Vector3 FpsAdsOffset { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "-15,15,0.1,suffix:°")] public float FpsAdsPitchDegrees { get; set; }
    // 1x uses the common FirstPersonMaster base duration; >1 raises/lowers faster.
    [Export(PropertyHint.Range, "0.25,3,0.05")] public float FpsAdsAnimationSpeedMultiplier { get; set; } = 1f;
    // Multiplies the equipped-weapon factor while ADS is held, not a replacement for it.
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float FpsAdsMovementMultiplier { get; set; } = 1f;
    [Export(PropertyHint.Range, "40,110,1,suffix:°")] public float FpsAdsFieldOfView { get; set; } = 75f;
    [ExportGroup("FPS camera recoil (FPS only)")]
    // The recoil offset is physically applied to the FPS camera, NOT applied again to the bullet ray.
    [Export(PropertyHint.Range, "1,90,1")] public float FpsCameraRecoilFollowDegreesPerSecond { get; set; } = 16f;
    // Total one-way camera rise allowed in a continuous FPS burst; does not modify top-down recoil.
    [Export(PropertyHint.Range, "0,85,0.5,suffix:°")] public float FpsMaxCameraRecoilBurstDegrees { get; set; } = 10f;
    // A gap this long between accepted shots resets the burst budget without moving the camera.
    [Export(PropertyHint.Range, "0.05,2,0.05,suffix:s")] public float FpsRecoilBurstResetSeconds { get; set; } = 0.4f;
    // Additional purely visual sustained model rise, on top of ordinary shot kick.
    [Export(PropertyHint.Range, "0,2,0.05")] public float FpsVisualClimbMultiplier { get; set; } = 0.6f;
}
