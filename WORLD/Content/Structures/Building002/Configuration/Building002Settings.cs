// Ranger lodge palette and dimensions for the one-shot procedural Building002 mesh.
using Godot;

[GlobalClass]
public partial class Building002Settings : Resource
{
    [ExportGroup("Footprint")]
    [Export(PropertyHint.Range, "14,24,0.5")] public float Width { get; set; } = 17f;
    [Export(PropertyHint.Range, "10,18,0.5")] public float Depth { get; set; } = 13f;
    [Export(PropertyHint.Range, "3.4,5.0,0.1")] public float WallHeight { get; set; } = 4.0f;
    [Export(PropertyHint.Range, "1.4,3.5,0.1")] public float RoofRise { get; set; } = 2.6f;
    [ExportGroup("Upper floor")]
    [Export(PropertyHint.Range, "2.5,3.4,0.1")] public float UpperWallHeight { get; set; } = 2.8f;
    [ExportGroup("Top-down visibility")]
    [Export] public float HiddenUpperOpacity { get; set; } = 0.12f;
    [Export] public float IndoorRoofOpacity { get; set; } = 0.1f;
    [Export] public float FloorFadeSeconds { get; set; } = 0.25f;
    [Export] public float LandingHysteresis { get; set; } = 0.3f;
    [ExportGroup("Colours")]
    [Export] public Color Timber { get; set; } = new(0.45f, 0.40f, 0.32f);
    [Export] public Color Roof { get; set; } = new(0.26f, 0.34f, 0.29f);
    [Export] public Color Trim { get; set; } = new(0.21f, 0.24f, 0.20f);
    [Export] public Color Stone { get; set; } = new(0.49f, 0.49f, 0.43f);
    [Export] public Color Glass { get; set; } = new(0.23f, 0.38f, 0.42f);
    [Export] public Color Accent { get; set; } = new(0.70f, 0.53f, 0.30f);
}
