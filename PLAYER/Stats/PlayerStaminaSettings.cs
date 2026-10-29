// PH2 authored exertion defaults; PlayerStamina copies/sanitizes these without changing the master.
using Godot;

[GlobalClass]
public partial class PlayerStaminaSettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Maximum { get; set; } = 100;
    [Export] public float SprintDrainPerSecond { get; set; } = 10;
    [Export] public float JumpCost { get; set; } = 15;
    [Export] public float RecoveryPerSecond { get; set; } = 15;
    [Export] public float RecoveryDelaySeconds { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float SprintResumeFraction { get; set; } = 0.2f;
}
