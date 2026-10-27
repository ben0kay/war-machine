// Reusable slow wander/chase defaults; each zombie receives an isolated runtime copy through EntityCore.
using Godot;

[GlobalClass]
public partial class ZombieSettings : EntitySettings
{
    [ExportGroup("Health")]
    [Export] public float MaxHealth { get; set; } = 50;
    [ExportGroup("Movement")]
    [Export] public float WanderSpeed { get; set; } = 0.65f;
    [Export] public float ChaseSpeed { get; set; } = 1.15f;
    [Export] public float WanderRadius { get; set; } = 10;
    [ExportGroup("Awareness")]
    [Export] public float DetectionRange { get; set; } = 18;
    [Export] public float LoseRange { get; set; } = 23;
    [Export] public float StopDistance { get; set; } = 1.3f;
}
