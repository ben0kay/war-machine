// Geometry-only streetlamp master. Real light settings are owned by ENVIRONMENT/Lighting.
using Godot;

[Tool]
[GlobalClass]
public partial class StreetLampSettings : Resource
{
    [ExportGroup("Fixture geometry")]
    [Export(PropertyHint.Range, "4.0,8.0,0.1")] public float PoleHeight { get; set; } = 5.8f;
    [Export(PropertyHint.Range, "0.6,1.7,0.05")] public float ArmReach { get; set; } = 1.1f;
    [Export] public Color MetalColor { get; set; } = new(0.17f, 0.21f, 0.23f);
    [Export] public Color HousingColor { get; set; } = new(0.25f, 0.28f, 0.29f);
}
