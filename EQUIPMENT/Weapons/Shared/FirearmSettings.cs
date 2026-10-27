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
    [Export] public float Damage { get; set; } = 95;
    [Export] public DamageType DamageKind { get; set; } = DamageType.Physical;
    [Export] public float Range { get; set; } = 90;
    [ExportGroup("Accuracy")]
    // Maximum half-angle in degrees. Zero means perfect accuracy; shot-only deviation.
    [Export(PropertyHint.Range, "0,10,0.05")] public float BaseSpreadDegrees { get; set; } = 0f;
    [ExportGroup("Firing collision")]
    [Export(PropertyHint.Layers3DPhysics)] public uint HitMask { get; set; } = 21;
    [ExportGroup("Bullet impact")]
    [Export(PropertyHint.Range, "0,3,0.05")] public float BulletImpactStrength { get; set; }
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float BulletStaggerSeconds { get; set; }
    [ExportGroup("Feedback")]
    [Export] public float EffectDuration { get; set; } = 0.07f;
    [Export(PropertyHint.Range, "1,16,1")] public int TracerPoolSize { get; set; } = 1;
    [Export(PropertyHint.Range, "0.01,0.25,0.01")] public float FlashDuration { get; set; } = 0.07f;
    [Export] public float FlashRadius { get; set; } = 0.08f;
    [Export] public float TracerWidth { get; set; } = 0.035f;
    [Export] public Color ShotColor { get; set; } = new(1, 0.8f, 0.35f);
    [Export] public bool EmitMuzzleLight { get; set; }
    [Export] public AudioCue? ShotCue { get; set; }
    // Legacy stream remains available for unmigrated weapons; pooled playback is required when ShotCue is set.
    [Export] public AudioStream? ShotSound { get; set; }
    [Export] public float ShotVolumeDb { get; set; } = -12;

    [ExportGroup("First-person presentation")]
    // FPS-only local adjustment relative to FirstPersonMaster. Does not change overhead weapon mounts.
    [Export] public Vector3 FirstPersonOffset { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "0.4,1.5,0.01")] public float FirstPersonScale { get; set; } = 1f;
}
