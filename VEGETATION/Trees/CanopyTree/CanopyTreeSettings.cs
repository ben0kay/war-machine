// DF1: authoring values for one tall, high-crowned broadleaf tree; independent of world placement.
using Godot;

[GlobalClass]
public partial class CanopyTreeSettings : Resource
{
    [ExportGroup("Silhouette / seed")]
    [Export] public int Seed { get; set; } = 90817;
    [Export(PropertyHint.Range, "12,28,0.5")] public float MinimumHeight { get; set; } = 17f;
    [Export(PropertyHint.Range, "12,28,0.5")] public float MaximumHeight { get; set; } = 22f;
    [Export(PropertyHint.Range, "0.35,0.75,0.01")] public float CrownStartFraction { get; set; } = 0.49f;
    [Export(PropertyHint.Range, "2,5,0.1")] public float CrownRadius { get; set; } = 3.2f;
    [Export(PropertyHint.Range, "0.2,0.9,0.05")] public float TrunkBaseRadius { get; set; } = 0.39f;
    [Export(PropertyHint.Range, "7,20,1")] public int BranchCount { get; set; } = 13;
    [Export(PropertyHint.Range, "2,5,1")] public int CrownClustersPerBranch { get; set; } = 4;
    [Export(PropertyHint.Range, "0.5,1.8,0.05")] public float FoliageSize { get; set; } = 1.0f;
    // DF2.5: near-only micro-facets. Set zero to recover the original rounded clump mesh.
    [Export(PropertyHint.Range, "0,0.25,0.01")] public float LeafEdgeDetail { get; set; } = 0.12f;

    [ExportGroup("Surface palette")]
    [Export] public Color BarkColor { get; set; } = new(0.53f, 0.51f, 0.43f);
    [Export] public Color BarkScarColor { get; set; } = new(0.25f, 0.26f, 0.22f);
    [Export] public Color LeafColor { get; set; } = new(0.13f, 0.29f, 0.12f);
    [Export] public Color LeafAccentColor { get; set; } = new(0.31f, 0.43f, 0.16f);

    [ExportGroup("Preview / LOD")]
    // Far leaves reuse the same skeleton but skip alternating small foliage clusters.
    [Export(PropertyHint.Range, "1,1.6,0.05")] public float FarFoliageScale { get; set; } = 1.2f;
}
