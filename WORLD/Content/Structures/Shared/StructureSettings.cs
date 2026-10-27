// Common visibility settings inherited by building-specific art/dimension masters.
using Godot;

[GlobalClass]
public partial class StructureSettings : Resource
{
    [ExportGroup("Outside obstruction")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float OutsideBuildingOpacity { get; set; } = 0.15f;
    // Smaller outside-house corridor than the camera's foliage radius, independently tunable per building.
    [Export(PropertyHint.Range, "0,1,0.01")] public float OutsideObstructionRadius { get; set; } = 0.18f;
    [ExportGroup("Indoor visibility")]
    [Export] public float HiddenUpperOpacity { get; set; } = 0.12f;
    [Export] public float IndoorRoofOpacity { get; set; } = 0.1f;
    [Export] public float FloorFadeSeconds { get; set; } = 0.25f;
    [Export] public float LandingHysteresis { get; set; } = 0.3f;
}
