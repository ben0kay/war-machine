// Authored vitality/region maxima only. Runtime values never write back into this profile.
using Godot;

[GlobalClass]
public partial class PlayerVitalsSettings : Resource
{
    [Export] public HealthSettings Health { get; set; } = new();
    [ExportGroup("Body condition maxima")]
    [Export(PropertyHint.Range, "1,1000,1")] public float Head { get; set; } = 100;
    [Export(PropertyHint.Range, "1,1000,1")] public float Torso { get; set; } = 100;
    [Export(PropertyHint.Range, "1,1000,1")] public float LeftArm { get; set; } = 100;
    [Export(PropertyHint.Range, "1,1000,1")] public float RightArm { get; set; } = 100;
    [Export(PropertyHint.Range, "1,1000,1")] public float LeftLeg { get; set; } = 100;
    [Export(PropertyHint.Range, "1,1000,1")] public float RightLeg { get; set; } = 100;

    // =========================================================
    public float Maximum(BodyRegion region) => region switch
    { BodyRegion.Head => Head, BodyRegion.LeftArm => LeftArm, BodyRegion.RightArm => RightArm,
        BodyRegion.LeftLeg => LeftLeg, BodyRegion.RightLeg => RightLeg, _ => Torso };
}
