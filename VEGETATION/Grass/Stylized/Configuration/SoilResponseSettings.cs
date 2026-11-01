// DTP2: tunable ground-cover response to actual Terrain3D painted dirt.
using Godot;

[Tool]
[GlobalClass]
public partial class SoilResponseSettings : Resource
{
    [ExportGroup("Soil thinning")]
    [Export] public bool Enabled { get; set; } = true;
    // Separate deterministic hash, independent of all vegetation placement RNG streams.
    [Export] public int Seed { get; set; } = 184213;
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float EdgeExponent { get; set; } = 1.5f;

    [ExportGroup("Grass sensitivity (0 none, 1 strongest)")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ForestGrass001 { get; set; } = 0.88f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ForestGrass002 { get; set; } = 0.90f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float BillboardGrass { get; set; } = 0.94f;
}
