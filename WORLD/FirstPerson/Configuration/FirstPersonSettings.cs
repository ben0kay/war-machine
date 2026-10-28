// Authored first-person view defaults. Shared gameplay resources remain independent.
using Godot;

[GlobalClass]
public partial class FirstPersonSettings : Resource
{
    [ExportGroup("Camera")]
    [Export] public float FieldOfView { get; set; } = 75;
    [Export] public float MouseSensitivity { get; set; } = 0.10f;
    [Export] public bool InvertMouseY { get; set; } = true;
    [Export] public float MaximumPitch { get; set; } = 85;
    [Export] public float EyeInset { get; set; } = 0.15f;
    [Export] public bool InterpolateTranslation { get; set; } = true;
    [Export] public float TeleportResetDistance { get; set; } = 8;
    [ExportGroup("Head bob (FPS only)")]
    [Export] public bool HeadBobEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.08,0.001")] public float WalkVerticalAmplitude { get; set; } = 0.025f;
    [Export(PropertyHint.Range, "0,0.08,0.001")] public float WalkHorizontalAmplitude { get; set; } = 0.012f;
    [Export(PropertyHint.Range, "0,0.08,0.001")] public float RunVerticalAmplitude { get; set; } = 0.04f;
    [Export(PropertyHint.Range, "0,0.08,0.001")] public float RunHorizontalAmplitude { get; set; } = 0.02f;
    [Export(PropertyHint.Range, "0.5,5,0.1")] public float WalkStrideLength { get; set; } = 1.6f;
    [Export(PropertyHint.Range, "0.5,5,0.1")] public float RunStrideLength { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.05,0.8,0.01")] public float HeadBobReturnSeconds { get; set; } = 0.18f;
    [ExportGroup("Fog and render distance")]
    [Export] public bool DistanceFogEnabled { get; set; } = true;
    [Export] public float FogStartDistance { get; set; } = 1;
    [Export] public float FogEndDistance { get; set; } = 115;
    [Export] public float FogCullBuffer { get; set; } = 120;
    [Export] public Color FogColor { get; set; } = new(0.39f, 0.46f, 0.48f);
    [ExportGroup("Static streaming")]
    [Export(PropertyHint.Range, "33,160,1")] public float StreamingHysteresisDistance { get; set; } = 64;
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 5;
    [Export] public float RunMultiplier { get; set; } = 1.6f;
    [Export] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export] public float Acceleration { get; set; } = 35;
    [Export] public float Deceleration { get; set; } = 45;
    [Export] public float DirectionChangeAcceleration { get; set; } = 50;
    [ExportGroup("Ground contact")]
    [Export] public float FloorSnapDistance { get; set; } = 0.4f;
    [Export] public float MaximumSlopeDegrees { get; set; } = 46;
    [Export] public float CollisionSafeMargin { get; set; } = 0.01f;
    [ExportGroup("Held weapon")]
    [Export] public Vector3 WeaponOffset { get; set; } = new(0.22f, -0.22f, -0.32f);
}
