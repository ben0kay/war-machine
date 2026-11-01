// STG5: one ground-cover master referencing existing grass, billboard and additional plants.
using Godot;

[Tool]
[GlobalClass]
public partial class GroundCoverSettings : Resource
{
    [ExportGroup("Existing ground cover")]
    [Export] public StylizedGrassSettings StylizedGrass { get; set; } = null!;
    [Export] public BillboardGrassSettings BillboardGrass { get; set; } = null!;
    [Export] public ForestGrass002Settings ForestGrass002 { get; set; } = null!;

    [ExportGroup("Additional plant species")]
    [Export] public Godot.Collections.Array<GroundCoverPlantSettings> Plants { get; set; } = new();
}
