// Data-driven ribbon grass quality and appearance. Placement stays on the GPU, independent of saved terrain edits.
using Godot;

[GlobalClass]
public partial class VegetationConfig : Resource
{
    public enum GrassQuality { Low, Medium, High }
    [ExportGroup("Activation / Quality")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public GrassQuality Quality { get; set; } = GrassQuality.Medium;
    [Export(PropertyHint.Range, "0,2,0.05")] public float Density { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "10000,1000000,10000")] public int ParticleBudget { get; set; } = 120000;
    [Export(PropertyHint.Range, "10,60,1")] public int SimulationFps { get; set; } = 20;
    [Export] public bool CastShadows { get; set; }
    [Export] public float WindMultiplier { get; set; } = 0.4f;
    [ExportGroup("Coverage")]
    [Export(PropertyHint.Range, "8,128,1")] public float RenderDistance { get; set; } = 64f;
    [Export(PropertyHint.Range, "16,64,1")] public float TileWidth { get; set; } = 32f;
    [Export(PropertyHint.Range, "0.1,0.95,0.01")] public float FadeStartFraction { get; set; } = 0.66f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float MinimumNormalY { get; set; } = 0.87f;
    [Export] public bool FilterPaintedRock { get; set; }
    [ExportGroup("Ribbon Shape / Patches")]
    [Export] public int BladeSections { get; set; } = 2;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float Height { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0.03,0.3,0.005")] public float BladeWidth { get; set; } = 0.16f;
    [Export] public float ClumpHeightBoost { get; set; } = 0.8f;
    [Export] public float PatchNoiseScale { get; set; } = 0.01f;
    [Export] public Vector2 PatchThresholds { get; set; } = new(0.025f, 0.2f);
    [Export] public Vector2 ClumpThresholds { get; set; } = new(0.2f, 0.8f);
    [Export] public Color BladeColor { get; set; } = new(0.20f, 0.22f, 0.05f);
    [Export] public uint Seed { get; set; } = 78341;
}
