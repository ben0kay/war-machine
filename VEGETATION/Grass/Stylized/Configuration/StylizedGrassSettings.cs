// STG1: editor master for sparse, seeded imported grass patch placement; no GPU particles.
using Godot;

[GlobalClass]
public partial class StylizedGrassSettings : Resource
{
    [ExportGroup("Activation / Seeds")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 49117;
    [Export(PropertyHint.Range, "0,12,1")] public int PatchesPerChunk { get; set; } = 8;
    [Export(PropertyHint.Range, "0,1,0.05")] public float PatchChance { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "1,30,1")] public int ClumpsPerPatch { get; set; } = 15;
    [Export(PropertyHint.Range, "0.6,4,0.2")] public float PatchRadiusMin { get; set; } = 2.1f;
    [Export(PropertyHint.Range, "0.6,6,0.2")] public float PatchRadiusMax { get; set; } = 4.7f;

    [ExportGroup("Appearance / Terrain")]
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float ScaleMin { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.3,2.5,0.05")] public float ScaleMax { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    // Clearance includes grass mesh half-width; building/road authored rectangles provide hard exclusions.
    [Export(PropertyHint.Range, "0.6,3,0.1")] public float EdgeClearance { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float WindMultiplier { get; set; } = 0.06f;
    [Export(PropertyHint.Range, "0,5,0.1")] public float WindSpeed { get; set; } = 1.1f;

    [ExportGroup("Streaming / LOD")]
    [Export(PropertyHint.Range, "16,64,16")] public int ChunkSize { get; set; } = 32;
    [Export(PropertyHint.Range, "24,128,4")] public float RenderDistance { get; set; } = 80f;
    [Export(PropertyHint.Range, "8,96,4")] public float LodDistance { get; set; } = 32f;
}
