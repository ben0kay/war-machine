// Data-driven oak preview settings; imported Mesh resources can replace generated art without changing placement.
using Godot;

[GlobalClass]
public partial class TreeSettings : Resource
{
    [ExportGroup("Activation / Distribution")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 78341;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Density { get; set; } = 0.72f;
    [Export(PropertyHint.Range, "8,40,1")] public float Spacing { get; set; } = 16f;
    [Export(PropertyHint.Range, "64,512,16")] public float RenderDistance { get; set; } = 240f;
    [Export(PropertyHint.Range, "32,192,16")] public int ChunkSize { get; set; } = 96;
    [Export(PropertyHint.Range, "-1,1,0.01")] public float ForestThreshold { get; set; } = -0.25f;
    [Export(PropertyHint.Range, "0.0001,0.03,0.0001")] public float ForestNoiseFrequency { get; set; } = 0.003f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float MaximumSlope { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,100,1")] public float SpawnClearingRadius { get; set; } = 34f;
    [Export] public bool CastShadows { get; set; } = true;

    [ExportGroup("Generated Oak Shape")]
    [Export(PropertyHint.Range, "1,12,1")] public int Variants { get; set; } = 4;
    [Export(PropertyHint.Range, "4,20,0.5")] public float MinimumHeight { get; set; } = 9f;
    [Export(PropertyHint.Range, "4,24,0.5")] public float MaximumHeight { get; set; } = 13f;
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float TrunkRadius { get; set; } = 0.52f;
    [Export(PropertyHint.Range, "3,16,1")] public int BranchCount { get; set; } = 9;
    [Export(PropertyHint.Range, "0.5,4,0.1")] public float LeafClusterSize { get; set; } = 1.85f;
    [Export(PropertyHint.Range, "0.4,1.5,0.05")] public float MinimumScale { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0.4,2,0.05")] public float MaximumScale { get; set; } = 1.25f;

    [ExportGroup("Future Model Replacement")]
    [Export] public Godot.Collections.Array<Mesh> ImportedMeshes { get; set; } = new();
}
