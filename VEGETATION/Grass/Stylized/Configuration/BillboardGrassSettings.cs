// STG3: independent widespread billboard filler density; shares grass streaming and exclusion ownership.
using Godot;

[GlobalClass]
public partial class BillboardGrassSettings : Resource
{
    [ExportGroup("Activation / Distribution")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 90641;
    [Export(PropertyHint.Range, "0.9,5,0.1")] public float CellSize { get; set; } = 1.8f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float Coverage { get; set; } = 0.82f;

    [ExportGroup("Appearance / Terrain")]
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float ScaleMin { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0.3,2.5,0.05")] public float ScaleMax { get; set; } = 1.15f;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float EdgeClearance { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float WindMultiplier { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0,5,0.1")] public float WindSpeed { get; set; } = 1.1f;
}
