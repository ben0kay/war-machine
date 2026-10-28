// Reusable point-investigation policy. Species owns triggers, movement, target selection and authored configuration.
using Godot;

[GlobalClass]
public partial class InvestigationSettings : Resource
{
    [Export] public Vector3 Weights { get; set; } = new(0.40f, 0.35f, 0.25f);
    [Export] public Vector2 EarlyFraction { get; set; } = new(0.40f, 0.60f);
    [Export] public Vector2 ModerateFraction { get; set; } = new(0.65f, 0.90f);
    [Export] public Vector2 TimeScale { get; set; } = new(1.10f, 1.60f);
    [Export] public float TimeAllowance { get; set; } = 3;
    [Export] public float MaximumSeconds { get; set; } = 60;
    [Export] public Vector2 LookSeconds { get; set; } = new(0.8f, 2);
}
