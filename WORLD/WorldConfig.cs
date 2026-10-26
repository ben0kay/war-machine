// Shared, saved terrain authoring settings. Baking is explicit; gameplay never regenerates land.
using Godot;

[GlobalClass]
public partial class WorldConfig : Resource
{
    [ExportGroup("Saved Terrain")]
    [Export] public string DataDirectory { get; set; } = "res://WORLD/Terrain/Data";
    [Export] public int Seed { get; set; } = 78341;
    [Export(PropertyHint.Range, "256,2048,256")] public int Size { get; set; } = 2048;
    [Export] public float Elevation { get; set; } = 155f;
    [Export] public float NoiseFrequency { get; set; } = 0.0035f;
    [ExportGroup("Valley / Meadows")]
    [Export] public float ValleyWidth { get; set; } = 310f;
    [Export] public float MeadowBlend { get; set; } = 0.9f;
    [Export] public float MeadowRelief { get; set; } = 4f;
    [ExportGroup("Grass Bake")]
    [Export(PropertyHint.Range, "2,16,1")] public int GrassSpacing { get; set; } = 4;
    [Export] public float GrassSlopeLimit { get; set; } = 0.6f;
    [Export] public float GrassCoverage { get; set; } = 0.85f;
    [ExportGroup("Exploration")]
    [Export] public Vector2 SpawnXZ { get; set; } = new(512, 620);
    [Export] public float SpawnClearance { get; set; } = 4f;
}
