// Read-only first-person experiment defaults. Shared gameplay resources remain independent.
using Godot;

[GlobalClass]
public partial class FirstPersonSettings : Resource
{
    [ExportGroup("Camera")]
    [Export] public float FieldOfView { get; set; } = 75;
    [Export] public float MouseSensitivity { get; set; } = 0.12f;
    [Export] public bool InvertMouseY { get; set; } = true;
    [Export] public float MaximumPitch { get; set; } = 85;
    [Export] public float EyeInset { get; set; } = 0.15f;
    [Export] public bool InterpolateTranslation { get; set; } = true;
    [Export] public float TeleportResetDistance { get; set; } = 8;
    [ExportGroup("Fog and render distance")]
    [Export] public bool DistanceFogEnabled { get; set; } = true;
    [Export] public float FogStartDistance { get; set; } = 25;
    [Export] public float FogEndDistance { get; set; } = 60;
    [Export] public float FogCullBuffer { get; set; } = 12;
    [Export] public Color FogColor { get; set; } = new(0.39f, 0.46f, 0.48f);
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
