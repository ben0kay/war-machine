// Shared anatomy tuning; species masters select condition and sever resistance independently of overall health.
using Godot;

[GlobalClass]
public partial class AnatomySettings : Resource
{
    [ExportGroup("Limb condition (ELD1)")]
    [Export(PropertyHint.Range, "1,10000,1")] public float RegionCondition { get; set; } = 80;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DamagedFraction { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DisabledFraction { get; set; } = 0.25f;
    [Export(PropertyHint.Range, "1,100000,1")] public float SeverThreshold { get; set; } = 65;
    [Export(PropertyHint.Range, "0.01,100,0.05")] public float SeverResistance { get; set; } = 1;
    [ExportGroup("Detection & diagnostics")]
    [Export] public bool DeferredHitDetection { get; set; } = true;
    [Export(PropertyHint.Range, "0.5,10,0.05")] public float BroadRadius { get; set; } = 1.6f;
    [Export] public Vector3 BroadCentre { get; set; } = new(0, 0.8f, 0);
    [Export(PropertyHint.Range, "0,0.02,0.001")] public float Padding { get; set; } = 0.005f;
    [Export] public bool LogRegionHits { get; set; } = true;
}
