// WorldSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    public const string TerrainDirectory = "res://WORLD/Terrain/Gameplay";
    public const int TerrainSize = 1024;
    [ExportGroup("World sections (WS0 diagnostics)")]
    [Export] public WorldSectionSettings Sections { get; set; } = new();
    [ExportGroup("Static building streaming (WS2)")]
    [Export] public bool StreamVillageBuildings { get; set; } = true;
    [Export] public WorldStreamingSettings Streaming { get; set; } = new();
    [ExportGroup("Fog-aware rendering (WS1)")]
    [Export] public WorldRenderSettings RenderPolicy { get; set; } = new();
    [ExportGroup("Dense canopy forest zones (DF2)")]
    // Backward compatibility: a world without ForestZones falls back to its DenseForest.
    [Export] public ForestZonesSettings? ForestZones { get; set; }
    [Export] public ForestSettings DenseForest { get; set; } = new();
    [ExportGroup("Village layout")]
    [Export] public bool VillageEnabled { get; set; } = true;
    [Export] public Vector2 VillageOriginXZ { get; set; } = new(512, 512);
    [Export(PropertyHint.Range, "0.5,12,0.5")] public float VillageMaxCutFill { get; set; } = 8;
    [Export(PropertyHint.Range, "6,30,1")] public float VillageBlendWidth { get; set; } = 18;
    [Export(PropertyHint.Range, "4,8,0.5")] public float VillageRoadWidth { get; set; } = 6;
    [Export(PropertyHint.Range, "0,2,0.1")] public float VillageHillScale { get; set; } = 1;
    // VW1: extra hillside relief and shallow basins, independent of the three existing hills.
    [Export] public bool VillageOutskirtsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,2,0.1")] public float VillageOutskirtsScale { get; set; } = 1f;
    [Export(PropertyHint.Range, "4,12,0.5")] public float VillageZombieWanderRadius { get; set; } = 8;
    [ExportGroup("Terrain generation")]
    [Export] public int Seed { get; set; } = 48193;
    [Export] public Vector2 SpawnXZ { get; set; } = new(512, 550);
    [Export] public float GroundHeight { get; set; } = 4;
    [Export] public float Relief { get; set; } = 0.15f;
    [Export] public Vector2 HillOffset { get; set; } = new(30, -10);
    [Export] public float HillHeight { get; set; } = 5;
    [Export] public float HillRadius { get; set; } = 24;
    [ExportGroup("Structures")]
    [Export] public Vector2 Building001Offset { get; set; } = new(-35, -90);
    [Export] public Vector2 Building002Offset { get; set; } = new(-35, -145);
    [Export] public Vector2 Building003Offset { get; set; } = new(80, -75);
    [Export] public Vector2 Building004Offset { get; set; } = new(45, -190);
    [Export] public Vector2 Car001Offset { get; set; } = new(24, 42);
    [ExportGroup("Terrain3D mountain test")]
    [Export] public bool CliffEnabled { get; set; } = false;
    [Export] public Vector2 CliffOffset { get; set; } = new(-6, 76);
    [Export] public float CliffHeight { get; set; } = 12;
    [Export] public float CliffWidth { get; set; } = 44;
    [Export] public float CliffDepth { get; set; } = 36;
    [Export] public float CliffRampLength { get; set; } = 38;
    [Export] public float CliffPathWidth { get; set; } = 10;
    [ExportGroup("Horde preview (top-down only)")]
    [Export(PropertyHint.Range, "0,60,1")] public int HordeCount { get; set; } = 0;
    [Export(PropertyHint.Range, "0,60,1")] public int HordeRunnerCount { get; set; } = 0;
    [Export] public Vector2 HordeCenterOffset { get; set; } = new(-38, -16);
    [Export(PropertyHint.Range, "1.8,4,0.1")] public float HordeSpacing { get; set; } = 2.5f;
    [ExportGroup("Runner placement")]
    [Export] public Vector2[] RunnerSpawnOffsets { get; set; } = new Vector2[]
    { new(20, -43) };
    [ExportGroup("Wildlife preview")]
    [Export] public bool DeerPreviewEnabled { get; set; } = true;
    // One AI deer south of the village approach; offset is in world X/Z metres from player spawn.
    [Export] public Vector2 DeerPreviewOffset { get; set; } = new(-18f, 50f);
    [ExportGroup("Store population stress test — 20 additional zombies")]
    // Opt-in to the authored extra population independently of the top-down-only horde preview.
    // All coordinates are relative to VillageOriginXZ and shared by top-down and FPS.
    [Export] public bool StoreStressZombiesEnabled { get; set; } = true;
    [Export] public Vector2[] StoreStressSlowOffsets { get; set; } = new Vector2[]
    {
        // Open cross-aisles between the three parking rows, clear of parked cars and central lamp pads.
        new(-19, -6), new(-11, -6.5f), new(11, -6), new(19, -6),
        new(-17, 5.3f), new(-9, 5.3f), new(9, 5.3f), new(17, 5.3f),
        // Open central corridor, store flanks and service/rear approach.
        new(0, -3), new(0, 11), new(-29, -25), new(29, -25),
        new(-10, -44), new(0, -46), new(10, -46), new(-4, -54)
    };
    [Export] public Vector2[] StoreStressRunnerOffsets { get; set; } = new Vector2[]
    {
        // Two lateral approaches, two behind the store.
        new(-25, -20), new(25, -22), new(-13, -52), new(14, -55)
    };

    [ExportGroup("Zombie placement")]
    // Village-relative safe candidates; actual solid surface and body clearance are checked before activation.
    [Export] public Vector2[] ZombieSpawnOffsets { get; set; } = new Vector2[]
    {
        new(-17, -4), new(17, -4), new(-10, 3), new(10, 3), new(-15, 10),
        new(15, 10), new(-18, -42), new(18, -42), new(0, 0)
    };
}
