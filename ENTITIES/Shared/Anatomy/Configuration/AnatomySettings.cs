// Shared anatomical hit detection tuning; no injury, severing or vitality multipliers in ELD0.
using Godot;

[GlobalClass]
public partial class AnatomySettings : Resource
{
    [Export(PropertyHint.Range, "0,0.02,0.001")] public float Padding { get; set; } = 0.005f;
    [Export] public bool LogRegionHits { get; set; } = true;
}
