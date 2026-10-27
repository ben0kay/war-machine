// Configurable dimensions and muted rural-industrial colours for Building001; reused by its C# mesh generator.
using Godot;

[GlobalClass]
public partial class Building001Settings : StructureSettings
{
    [ExportGroup("Footprint")]
    [Export(PropertyHint.Range, "16,32,1")] public float Width { get; set; } = 20f;
    [Export(PropertyHint.Range, "10,24,1")] public float Depth { get; set; } = 14f;
    [Export(PropertyHint.Range, "4,8,0.1")] public float WallHeight { get; set; } = 5.2f;
    [Export(PropertyHint.Range, "1,5,0.1")] public float RoofRise { get; set; } = 2.6f;
    [ExportGroup("Palette")]
    [Export] public Color Siding { get; set; } = new(0.33f, 0.40f, 0.39f);
    [Export] public Color Roof { get; set; } = new(0.36f, 0.29f, 0.28f);
    [Export] public Color Trim { get; set; } = new(0.18f, 0.23f, 0.24f);
    [Export] public Color Concrete { get; set; } = new(0.52f, 0.51f, 0.46f);
    [Export] public Color Window { get; set; } = new(0.14f, 0.28f, 0.34f);
    [Export] public Color Accent { get; set; } = new(0.82f, 0.63f, 0.27f);
}
