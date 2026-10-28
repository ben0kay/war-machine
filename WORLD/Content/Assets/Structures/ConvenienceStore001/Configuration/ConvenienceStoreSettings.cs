// Inspector-editable dimensions and palette for the independent convenience-store prefab.
using Godot;

[Tool]
[GlobalClass]
public partial class ConvenienceStoreSettings : StructureSettings
{
    [ExportGroup("Dimensions")]
    [Export(PropertyHint.Range, "16,32,0.5")] public float Width { get; set; } = 24f;
    [Export(PropertyHint.Range, "12,24,0.5")] public float Depth { get; set; } = 16f;
    [Export(PropertyHint.Range, "3.8,5.2,0.1")] public float WallHeight { get; set; } = 4.2f;
    [Export(PropertyHint.Range, "4.5,8,0.1")] public float StockroomDepth { get; set; } = 5.2f;

    [ExportGroup("Interior")]
    [Export] public bool ShowFurnishings { get; set; } = true;
    [Export(PropertyHint.Range, "0.8,1.8,0.1")] public float ShelfHeight { get; set; } = 1.45f;

    [ExportGroup("Wall finish")]
    // False restores the original plain StandardMaterial3D, without touching geometry.
    [Export] public bool UseTexturedWalls { get; set; } = true;
    [Export(PropertyHint.Range, "0.15,2.0,0.05")] public float WallTextureRepeatsPerMeter { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float WallBumpStrength { get; set; } = 0.30f;
    [Export(PropertyHint.Range, "0.6,1,0.02")] public float WallRoughness { get; set; } = 0.94f;

    [ExportGroup("Appearance")]
    [Export] public Color Walls { get; set; } = new(0.73f, 0.71f, 0.66f);
    [Export] public Color Trim { get; set; } = new(0.18f, 0.25f, 0.28f);
    [Export] public Color Accent { get; set; } = new(0.69f, 0.29f, 0.21f);
    [Export] public Color Floor { get; set; } = new(0.43f, 0.44f, 0.40f);
    [Export] public Color Roofing { get; set; } = new(0.24f, 0.27f, 0.29f);
    [Export] public Color Shelves { get; set; } = new(0.40f, 0.43f, 0.41f);
}
