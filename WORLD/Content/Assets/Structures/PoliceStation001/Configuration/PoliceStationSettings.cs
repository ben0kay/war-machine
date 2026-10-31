// PS0 police-station shell: heights and restrained structural palette; footprint/layout stay fixed at 48x64m.
using Godot;

[Tool]
[GlobalClass]
public partial class PoliceStationSettings : StructureSettings
{
    [ExportGroup("Floor elevations")]
    [Export(PropertyHint.Range, "3.8,5.2,0.1")] public float GroundFloorHeight { get; set; } = 4.2f;
    [Export(PropertyHint.Range, "3.2,4.8,0.1")] public float UpperFloorHeight { get; set; } = 3.8f;

    [ExportGroup("Basement prototype")]
    [Export] public bool BasementEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "3.8,5.2,0.1")] public float BasementLevelDepth { get; set; } = 4.2f;

    [ExportGroup("Basic ceiling lighting")]
    [Export] public bool CeilingLightsEnabled { get; set; } = true;
    [Export] public bool CorridorLightsEnabled { get; set; } = true;
    [Export] public bool RoomLightsEnabled { get; set; } = true;

    [ExportGroup("Ground support")]
    [Export] public bool UseTerrainSupport { get; set; } = false;

    [ExportGroup("Shell appearance")]
    [Export] public Color Exterior { get; set; } = new(0.58f, 0.62f, 0.64f);
    [Export] public Color Partitions { get; set; } = new(0.75f, 0.77f, 0.77f);
    [Export] public Color Frames { get; set; } = new(0.18f, 0.24f, 0.30f);
    [Export] public Color WindowGlass { get; set; } = new(0.24f, 0.37f, 0.42f);
    // Basement wall concrete is deliberately lighter than either finished floor.
    [Export] public Color Concrete { get; set; } = new(0.65f, 0.69f, 0.71f);
    [Export] public Color FloorSurface { get; set; } = new(0.38f, 0.43f, 0.45f);
    [Export] public Color BasementFloor { get; set; } = new(0.33f, 0.38f, 0.41f);
    [Export] public Color CeilingSurface { get; set; } = new(0.84f, 0.86f, 0.86f);
    [Export] public Color Roof { get; set; } = new(0.31f, 0.35f, 0.37f);
    [Export] public Color DoorPanels { get; set; } = new(0.31f, 0.36f, 0.40f);
    [Export] public Color StairTreads { get; set; } = new(0.25f, 0.30f, 0.33f);
    [Export] public Color StairRisers { get; set; } = new(0.51f, 0.56f, 0.58f);
    // Plain muted navy-blue safety nosing (not yellow or emissive).
    [Export] public Color StairEdgeStrips { get; set; } = new(0.20f, 0.35f, 0.51f);
}
