// PlayerSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class PlayerSettings : Resource
{
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 5;
    [Export] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export] public float RunMultiplier { get; set; } = 1.6f;
    [Export] public float Acceleration { get; set; } = 24;
    [Export] public float Gravity { get; set; } = 20;
    [Export] public float GaitRate { get; set; } = 1.8f;
    [ExportGroup("Equipment pose")]
    [Export] public Vector3 WeaponProbeOffset { get; set; } = new(0, 0.38f, 0);
    [Export] public Vector3 WeaponMountOffset { get; set; } = new(0.24f, 0.40f, -0.45f);
    [Export] public Vector3 LeftWeaponArmDegrees { get; set; } = new(72, -25, 23);
    [Export] public Vector3 RightWeaponArmDegrees { get; set; } = new(72, 0, 8);
}
