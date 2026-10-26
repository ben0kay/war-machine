// Reusable free-flight exploration tuning, independent of world and sky ownership.
using Godot;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [ExportGroup("Flight")]
    [Export] public float MoveSpeed { get; set; } = 18f;
    [Export] public float BoostMultiplier { get; set; } = 4f;
    [ExportGroup("First Person")]
    [Export] public float WalkSpeed { get; set; } = 5f;
    [Export] public float SprintMultiplier { get; set; } = 1.7f;
    [Export] public float Gravity { get; set; } = 20f;
    [Export] public float JumpSpeed { get; set; } = 6f;
    [Export] public float BodyHeight { get; set; } = 1.8f;
    [Export] public float EyeHeight { get; set; } = 1.65f;
    [Export] public float CapsuleRadius { get; set; } = 0.32f;
    [ExportGroup("View")]
    [Export] public float MouseSensitivity { get; set; } = 0.002f;
    [Export] public float FieldOfView { get; set; } = 75f;
    [Export] public float FarDistance { get; set; } = 5000f;
}
