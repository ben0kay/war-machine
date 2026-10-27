// Roadside fuel/service station appearance and footprint settings for Building003.
using Godot;

[GlobalClass]
public partial class Building003Settings : Resource
{
    [ExportGroup("Outside obstruction")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float OutsideBuildingOpacity { get; set; } = 0.15f;
    [ExportGroup("Station")]
    [Export(PropertyHint.Range, "18,28,0.5")] public float Width { get; set; } = 22f;
    [Export(PropertyHint.Range, "10,17,0.5")] public float Depth { get; set; } = 12f;
    [Export(PropertyHint.Range, "3.5,5.5,0.1")] public float WallHeight { get; set; } = 4.4f;
    [ExportGroup("Colours")]
    [Export] public Color Walls { get; set; } = new(0.67f, 0.64f, 0.56f);
    [Export] public Color Roof { get; set; } = new(0.29f, 0.34f, 0.34f);
    [Export] public Color Trim { get; set; } = new(0.20f, 0.28f, 0.30f);
    [Export] public Color Glass { get; set; } = new(0.18f, 0.33f, 0.36f);
    [Export] public Color Accent { get; set; } = new(0.76f, 0.29f, 0.23f);
    [Export] public Color Concrete { get; set; } = new(0.46f, 0.48f, 0.45f);
}
