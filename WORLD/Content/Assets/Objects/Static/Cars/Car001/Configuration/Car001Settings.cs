// Shared Car001 dimensions, colour and default closed driver-door pose; per-instance overrides live on Car001Mesh.
using Godot;

[Tool]
[GlobalClass]
public partial class Car001Settings : Resource
{
    [ExportGroup("Body")]
    [Export(PropertyHint.Range, "3.6,5.5,0.1")] public float Length { get; set; } = 4.35f;
    [Export(PropertyHint.Range, "1.55,2.3,0.05")] public float Width { get; set; } = 1.83f;
    [Export(PropertyHint.Range, "1.7,2.2,0.05")] public float RoofHeight { get; set; } = 1.81f;
    [ExportGroup("Driver door")]
    [Export(PropertyHint.Range, "0,82,1,suffix:°")] public float DoorAngle { get; set; } = 0f;
    [ExportGroup("Colours")]
    [Export] public Color Paint { get; set; } = new(0.37f, 0.43f, 0.36f);
    [Export] public Color Glass { get; set; } = new(0.18f, 0.29f, 0.33f);
}
