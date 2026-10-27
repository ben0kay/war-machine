// Data-driven conifer preview settings; imported Mesh resources can replace generated art without changing placement.
using Godot;

[GlobalClass]
public partial class PineSettings : Resource
{
    [ExportGroup("Activation / Distribution")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 78341;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Density { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "8,40,1")] public float Spacing { get; set; } = 20f;
    [Export(PropertyHint.Range, "64,512,16")] public float RenderDistance { get; set; } = 96f;
    [Export(PropertyHint.Range, "32,192,16")] public int ChunkSize { get; set; } = 32;
    [Export(PropertyHint.Range, "-1,1,0.01")] public float ForestThreshold { get; set; } = -1f;
    [Export(PropertyHint.Range, "0.0001,0.03,0.0001")] public float ForestNoiseFrequency { get; set; } = 0.003f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float MaximumSlope { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,100,1")] public float SpawnClearingRadius { get; set; } = 12f;
    [Export] public bool CastShadows { get; set; } = false;

    [ExportGroup("Generated Pine Shape")]
    [Export(PropertyHint.Range, "1,12,1")] public int Variants { get; set; } = 3;
    [Export(PropertyHint.Range, "4,20,0.5")] public float MinimumHeight { get; set; } = 12f;
    [Export(PropertyHint.Range, "4,24,0.5")] public float MaximumHeight { get; set; } = 18f;
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float TrunkRadius { get; set; } = 0.42f;
    [Export(PropertyHint.Range, "0.5,4,0.05")] public float TrunkThicknessMultiplier { get; set; } = 2;
    [Export(PropertyHint.Range, "0.5,1.5,0.05")] public float MinimumTrunkThickness { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.5,1.5,0.05")] public float MaximumTrunkThickness { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.4,1.5,0.05")] public float MinimumScale { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0.4,2,0.05")] public float MaximumScale { get; set; } = 1.25f;

    [Export(PropertyHint.Range, "128,4000,64")] public int LeafBudget { get; set; } = 320;
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float NeedleClusterLength { get; set; } = 0.95f;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float NeedleClusterThickness { get; set; } = 0.19f;
    [ExportGroup("Pine Silhouette")]
    [Export(PropertyHint.Range, "0.5,2,0.05")] public float HeightMultiplier { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.5,0.99,0.01")] public float TrunkTaper { get; set; } = 0.94f;
    [Export(PropertyHint.Range, "3,24,1")] public int BranchTierCount { get; set; } = 7;
    [Export(PropertyHint.Range, "0.5,3,0.05")] public float BranchLengthFalloff { get; set; } = 1.1f;
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float FoliageDensity { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.08,0.35,0.01")] public float ConicalWidth { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float ColourVariation { get; set; } = 0.22f;
    [ExportGroup("Distance / Quality")]
    [Export] public bool UseGeneratedLod { get; set; } = true;
    [Export(PropertyHint.Range, "32,256,8")] public float LodDistance { get; set; } = 48f;
    [Export(PropertyHint.Range, "4,48,2")] public float DistanceFadeMargin { get; set; } = 8f;
    [Export(PropertyHint.Range, "2,8,1")] public int FarLeafStride { get; set; } = 6;
    [Export] public bool FarCastShadows { get; set; }

    [ExportGroup("Individual Leaves / GPU Wind")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float LeafWindStrength { get; set; } = 0.15f;
    [Export(PropertyHint.Range, "0,6,0.1")] public float LeafWindSpeed { get; set; } = 1.8f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float LeafFlutter { get; set; } = 0f;

    [ExportGroup("Future Model Replacement")]
    [Export] public Godot.Collections.Array<Mesh> ImportedMeshes { get; set; } = new();
}
