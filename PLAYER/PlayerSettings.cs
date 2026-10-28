// PlayerSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class PlayerSettings : Resource
{
    [ExportGroup("Vitality and body condition")]
    [Export] public PlayerVitalsSettings Vitals { get; set; } = new();
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 5;
    [Export] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export] public float RunMultiplier { get; set; } = 1.6f;
    [Export] public float Acceleration { get; set; } = 24;
    [Export] public float Gravity { get; set; } = 20;
    [Export] public float GaitRate { get; set; } = 1.8f;
    [ExportGroup("Walking gait")]
    [Export(PropertyHint.Range, "0,60,1")] public float KneeBendDegrees { get; set; } = 35f;
    [Export(PropertyHint.Range, "0,20,1")] public float FootRollDegrees { get; set; } = 8f;
    [ExportGroup("Upper-body gait")]
    [Export(PropertyHint.Range, "0,8,0.5")] public float TorsoTwistDegrees { get; set; } = 4f;
    [Export(PropertyHint.Range, "0,5,0.5")] public float TorsoRollDegrees { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0,6,0.5")] public float ShoulderSwayDegrees { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0,35,1")] public float ElbowFlexDegrees { get; set; } = 14f;
    [ExportGroup("Equipment pose")]
    [Export] public Vector3 WeaponProbeOffset { get; set; } = new(0, 0.38f, 0);
    [Export] public Vector3 WeaponMountOffset { get; set; } = new(0.24f, 0.40f, -0.45f);
    [Export] public Vector3 LeftWeaponArmDegrees { get; set; } = new(72, -25, 23);
    [Export] public Vector3 RightWeaponArmDegrees { get; set; } = new(72, 0, 8);
}
