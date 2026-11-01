// One editable foliage master: generation recipes, fog visibility and chunk scheduling in one resource.
using Godot;

[Tool]
[GlobalClass]
public partial class FoliageSettings : Resource
{
    [ExportGroup("Shared chunk lifecycle")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "32,32,1")] public int ChunkSize { get; set; } = 32;
    [Export(PropertyHint.Range, "1,6,1")] public int OperationsPerFrame { get; set; } = 2;
    // Soft CPU budget: an individual legacy mesh generator may still exceed this duration.
    [Export(PropertyHint.Range, "1,12,0.5")] public float WorkBudgetMs { get; set; } = 3f;
    [Export(PropertyHint.Range, "1,3,0.5")] public float LoadChunkMargin { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "2,5,0.5")] public float UnloadChunkMargin { get; set; } = 2.5f;

    [ExportGroup("What grows and how often — per-family recipes")]
    [Export] public GroundCoverSettings GroundCover { get; set; } = null!;
    [Export] public PineSettings Pines { get; set; } = null!;
    [Export] public ForestZonesSettings ForestZones { get; set; } = null!;
    [Export] public ContentScatterSettings Rocks { get; set; } = null!;
    [Export] public VegetationConfig GpuGrass { get; set; } = null!;

    [ExportGroup("Shared presentation")]
    [Export] public VegetationVisibilitySettings Visibility { get; set; } = null!;
    [Export] public FoliageTintSettings Tint { get; set; } = null!;
}
