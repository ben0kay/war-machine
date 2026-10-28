// Inspector-editable dimensions and palette for the independent convenience-store prefab.
using Godot;

[GlobalClass]
public partial class ConvenienceStoreSettings : StructureSettings
{
    [ExportGroup("Dimensions")]
    [Export(PropertyHint.Range, "14,20,0.5")] public float Width { get; set; } = 16f;
    [Export(PropertyHint.Range, "11,16,0.5")] public float Depth { get; set; } = 12f;
    [Export(PropertyHint.Range, "3.8,5.2,0.1")] public float WallHeight { get; set; } = 4.2f;
    [Export(PropertyHint.Range, "3.3,5.2,0.1")] public float StockroomDepth { get; set; } = 4f;

    [ExportGroup("Interior")]
    [Export] public bool ShowFurnishings { get; set; } = true;
    [Export(PropertyHint.Range, "0.8,1.8,0.1")] public float ShelfHeight { get; set; } = 1.45f;

    [ExportGroup("Appearance")]
    [Export] public Color Walls { get; set; } = new(0.64f, 0.62f, 0.55f);
    [Export] public Color Trim { get; set; } = new(0.18f, 0.25f, 0.28f);
    [Export] public Color Accent { get; set; } = new(0.69f, 0.29f, 0.21f);
    [Export] public Color Floor { get; set; } = new(0.43f, 0.44f, 0.40f);
    [Export] public Color Roofing { get; set; } = new(0.24f, 0.27f, 0.29f);
    [Export] public Color Shelves { get; set; } = new(0.40f, 0.43f, 0.41f);
}
