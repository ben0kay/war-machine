// STG3B: configurable irregular billboard patch placement; shares grass streaming and exclusion ownership.
using Godot;

[GlobalClass]
public partial class BillboardGrassSettings : Resource
{
    [ExportGroup("Activation / Patch Distribution")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 90641;
    [Export(PropertyHint.Range, "0,16,1")] public int PatchesPerChunk { get; set; } = 9;
    [Export(PropertyHint.Range, "0,1,0.05")] public float PatchChance { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "1,100,1")] public int MinPerPatch { get; set; } = 28;
    [Export(PropertyHint.Range, "1,120,1")] public int MaxPerPatch { get; set; } = 64;
    [Export(PropertyHint.Range, "0.8,10,0.1")] public float PatchRadiusMin { get; set; } = 2.2f;
    [Export(PropertyHint.Range, "0.8,12,0.1")] public float PatchRadiusMax { get; set; } = 7.2f;

    [ExportGroup("Appearance / Terrain")]
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float ScaleMin { get; set; } = 0.6f;
    [Export(PropertyHint.Range, "0.3,2.5,0.05")] public float ScaleMax { get; set; } = 1.55f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float TiltChance { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,8,0.5")] public float MaxTiltDegrees { get; set; } = 5f;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float EdgeClearance { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float WindMultiplier { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0,5,0.1")] public float WindSpeed { get; set; } = 1.1f;
}
