// STG5: reusable data-only ground-cover species; the grass controller owns all placement.
using Godot;

[Tool]
[GlobalClass]
public partial class GroundCoverPlantSettings : Resource
{
    [ExportGroup("Plant art")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public string Label { get; set; } = "Forest shrubs";
    [Export] public string ModelDirectory { get; set; } = "res://ASSETS/Vegetation/Plants/ForestShrub001/Models";
    [Export] public string ModelPrefix { get; set; } = "forest_shrub_001_";
    [Export] public string MaterialPath { get; set; } =
        "res://ASSETS/Vegetation/Plants/ForestShrub001/Materials/ForestShrub001Wind.tres";
    [Export(PropertyHint.Range, "1,12,1")] public int VariantCount { get; set; } = 6;

    [ExportGroup("Detailed cover selection")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float Share { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0.25,2,0.05")] public float ScaleMin { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0.25,2,0.05")] public float ScaleMax { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0,5,0.25")] public float MinimumSpacing { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float EdgeClearance { get; set; } = 1.2f;

    [ExportGroup("Shared wind")]
    [Export(PropertyHint.Range, "0,2,0.05")] public float WindMultiplier { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0,5,0.1")] public float WindSpeed { get; set; } = 1.1f;
}
