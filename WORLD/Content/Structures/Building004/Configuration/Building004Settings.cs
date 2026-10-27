// Abandoned two-storey field office with flat roof, balcony, and internal crosswise stairs.
using Godot;

[GlobalClass]
public partial class Building004Settings : StructureSettings
{
    [ExportGroup("Building dimensions")]
    [Export(PropertyHint.Range, "17,23,0.5")] public float Width { get; set; } = 18f;
    [Export(PropertyHint.Range, "13,17,0.5")] public float Depth { get; set; } = 14f;
    [Export(PropertyHint.Range, "3.1,3.8,0.1")] public float GroundFloorHeight { get; set; } = 3.35f;
    [Export(PropertyHint.Range, "2.8,3.5,0.1")] public float UpperFloorHeight { get; set; } = 3f;

    [ExportGroup("Colours")]
    [Export] public Color Brick { get; set; } = new(0.49f, 0.46f, 0.41f);
    [Export] public Color Concrete { get; set; } = new(0.57f, 0.56f, 0.51f);
    [Export] public Color Roof { get; set; } = new(0.26f, 0.29f, 0.31f);
    [Export] public Color Metal { get; set; } = new(0.19f, 0.25f, 0.27f);
    [Export] public Color Glass { get; set; } = new(0.23f, 0.38f, 0.43f);
    [Export] public Color Accent { get; set; } = new(0.66f, 0.39f, 0.23f);
}
