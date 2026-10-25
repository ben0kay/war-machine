// Reusable free-flight exploration tuning, independent of world and sky ownership.
using Godot;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [ExportGroup("Flight")]
    [Export] public float MoveSpeed { get; set; } = 18f;
    [Export] public float BoostMultiplier { get; set; } = 4f;
    [ExportGroup("View")]
    [Export] public float MouseSensitivity { get; set; } = 0.002f;
    [Export] public float FieldOfView { get; set; } = 75f;
    [Export] public float FarDistance { get; set; } = 5000f;
}
