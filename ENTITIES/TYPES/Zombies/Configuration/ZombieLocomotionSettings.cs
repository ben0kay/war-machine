// Species/variant movement response to injury. Existing healthy speeds remain on ZombieSettings.
using Godot;

[GlobalClass]
public partial class ZombieLocomotionSettings : Resource
{
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float LimpMultiplier { get; set; } = 0.60f;
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float HopMultiplier { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.05,3,0.05")] public float MaximumHopSpeed { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float CrawlSpeed { get; set; } = 0.45f;
}
