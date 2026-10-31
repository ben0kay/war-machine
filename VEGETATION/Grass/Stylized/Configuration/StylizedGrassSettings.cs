// STG1: editor master for sparse, seeded imported grass patch placement; no GPU particles.
using Godot;

[GlobalClass]
public partial class StylizedGrassSettings : Resource
{
    [ExportGroup("Activation / Seeds")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 49117;
    [Export(PropertyHint.Range, "0,12,1")] public int PatchesPerChunk { get; set; } = 6;
    [Export(PropertyHint.Range, "0,1,0.05")] public float PatchChance { get; set; } = 0.82f;
    [Export(PropertyHint.Range, "1,30,1")] public int ClumpsPerPatch { get; set; } = 12;
    [Export(PropertyHint.Range, "0.6,4,0.2")] public float PatchRadiusMin { get; set; } = 1.7f;
    [Export(PropertyHint.Range, "0.6,6,0.2")] public float PatchRadiusMax { get; set; } = 3.6f;

    [ExportGroup("Appearance / Terrain")]
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float ScaleMin { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0.3,2.5,0.05")] public float ScaleMax { get; set; } = 1.15f;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    // Clearance includes grass mesh half-width; building/road authored rectangles provide hard exclusions.
    [Export(PropertyHint.Range, "0.6,3,0.1")] public float EdgeClearance { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float WindMultiplier { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0,5,0.1")] public float WindSpeed { get; set; } = 1.1f;

    [ExportGroup("Streaming / LOD")]
    [Export(PropertyHint.Range, "16,64,16")] public int ChunkSize { get; set; } = 32;
    [Export(PropertyHint.Range, "24,128,4")] public float RenderDistance { get; set; } = 64f;
    [Export(PropertyHint.Range, "8,96,4")] public float LodDistance { get; set; } = 28f;
}
