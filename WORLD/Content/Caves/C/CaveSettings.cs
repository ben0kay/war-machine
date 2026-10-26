// One editable, reproducible cave preview, positioned relative to TestWorld's spawn.
using Godot;
[GlobalClass]
public partial class CaveSettings : Resource
{
    [ExportGroup("Layout")]
    [Export] public Vector2 SpawnOffset { get; set; } = new(48, -28);
    [Export] public float Length { get; set; } = 120;
    [Export] public float Width { get; set; } = 3.5f;
    [Export] public float Height { get; set; } = 4.8f;
    [Export] public int Sections { get; set; } = 96;
    [ExportGroup("Seeded organic shape")]
    [Export] public int Seed { get; set; } = 7131;
    [Export] public float ShapeFrequency { get; set; } = 0.055f;
    [Export] public float RockFrequency { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float WidthNoise { get; set; } = 0.24f;
    [Export(PropertyHint.Range, "0,0.4,0.01")] public float HeightNoise { get; set; } = 0.28f;
    [Export] public float WallRelief { get; set; } = 0.35f;
    [Export] public float FloorRelief { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "12,40,1")] public int ArchSegments { get; set; } = 24;
    [Export] public float EntranceBlendRadius { get; set; } = 14;
    [Export] public float RidgeFeather { get; set; } = 32;
    [ExportGroup("Underground corridors")]
    [Export] public float DescentDepth { get; set; } = 30;
    [Export] public float MaximumGrade { get; set; } = 0.35f;
    [Export] public float CorridorWanderDegrees { get; set; } = 35;
    [Export] public float CorridorVerticalNoise { get; set; } = 0.5f;
    [Export] public float MinimumRoofCover { get; set; } = 3;
    [ExportGroup("Fork")]
    [Export] public float BranchLength { get; set; } = 75;
    [Export] public float BranchDescent { get; set; } = 12;
    [Export] public float JunctionLength { get; set; } = 14;
    [ExportGroup("Exterior hillside material")]
    [Export] public float ExteriorTextureScale { get; set; } = 0.25f;
    [Export] public float ExteriorNormalStrength { get; set; } = 1;
    [ExportGroup("Lighting")]
    [Export] public float LightEnergy { get; set; } = 2;
    [Export] public Color LightColor { get; set; } = new(0.35f, 0.65f, 1);
}
