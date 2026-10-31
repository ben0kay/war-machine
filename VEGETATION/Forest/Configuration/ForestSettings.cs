// DF0/DF2: single authored forest footprint and bounded streamed-tree placement settings.
using Godot;

[GlobalClass]
public partial class ForestSettings : Resource
{
    [ExportGroup("DF0 test footprint")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public bool ShowDebugOutline { get; set; } = false;
    // Absolute world X/Z coordinates, independent of the moving player spawn.
    [Export] public Vector2 CenterXZ { get; set; } = new(667f, 487f);
    [Export] public Vector2 SizeXZ { get; set; } = new(96f, 256f);
    [Export(PropertyHint.Range, "0,24,1")] public float TransitionWidth { get; set; } = 12f;
    [Export(PropertyHint.Range, "2,12,1")] public float SurveyStep { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float MaximumSlope { get; set; } = 0.75f;

    [ExportGroup("DF2 tree layout")]
    [Export] public int Seed { get; set; } = 72199;
    [Export(PropertyHint.Range, "5,12,0.25")] public float MatureSpacing { get; set; } = 7f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float MatureDensity { get; set; } = 0.95f;
    [Export(PropertyHint.Range, "3,9,0.25")] public float MatureMinSeparation { get; set; } = 5.2f;
    [Export(PropertyHint.Range, "5,16,0.25")] public float YoungSpacing { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float YoungDensity { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "2,7,0.25")] public float YoungMinSeparation { get; set; } = 2.8f;
    [Export(PropertyHint.Range, "0.7,1.3,0.05")] public float MatureScaleMin { get; set; } = 0.9f;
    [Export(PropertyHint.Range, "0.7,1.4,0.05")] public float MatureScaleMax { get; set; } = 1.12f;
    [Export(PropertyHint.Range, "0.4,0.85,0.05")] public float YoungScaleMin { get; set; } = 0.56f;
    [Export(PropertyHint.Range, "0.4,0.9,0.05")] public float YoungScaleMax { get; set; } = 0.74f;
    [Export(PropertyHint.Range, "4,10,0.5")] public float TrailWidth { get; set; } = 7f;
    // Fixed southern road entry; extending the zone north must not move the entrance.
    [Export] public float TrailEntryZ { get; set; } = 567f;

    [ExportGroup("DF2 chunk rendering / collision")]
    [Export(PropertyHint.Range, "32,64,16")] public int ChunkSize { get; set; } = 32;
    // Safe visible palette mode uses native StandardMaterial3D rather than unverified custom shaders.
    // Turn OFF only after checking DF2 shader compilation; shader mode retains top-down leaf fade.
    [Export] public bool UseSafePaletteMaterials { get; set; } = true;
    [Export] public bool NearCastShadows { get; set; } = true;
    [Export] public bool FarCastShadows { get; set; } = false;
    [Export] public bool TrunkCollisionEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "2,8,0.5")] public float TrunkCollisionHeight { get; set; } = 4f;
    [Export(PropertyHint.Range, "0.6,1.8,0.05")] public float TrunkRadiusMultiplier { get; set; } = 1f;
}
