// WorldSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    public const string TerrainDirectory = "res://WORLD/Terrain/Gameplay";
    public const int TerrainSize = 1024;
    [ExportGroup("Village layout")]
    [Export] public bool VillageEnabled { get; set; } = true;
    [Export] public Vector2 VillageOriginXZ { get; set; } = new(512, 512);
    [Export(PropertyHint.Range, "0.5,12,0.5")] public float VillageMaxCutFill { get; set; } = 8;
    [Export(PropertyHint.Range, "6,30,1")] public float VillageBlendWidth { get; set; } = 18;
    [Export(PropertyHint.Range, "4,8,0.5")] public float VillageRoadWidth { get; set; } = 6;
    [Export(PropertyHint.Range, "0,2,0.1")] public float VillageHillScale { get; set; } = 1;
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
    { new(19, -34) };
    [ExportGroup("Wildlife preview")]
    [Export] public bool DeerPreviewEnabled { get; set; } = true;
    // One AI deer south of the village approach; offset is in world X/Z metres from player spawn.
    [Export] public Vector2 DeerPreviewOffset { get; set; } = new(-18f, 50f);
    [ExportGroup("Zombie placement")]
    // Village-relative safe candidates; actual solid surface and body clearance are checked before activation.
    [Export] public Vector2[] ZombieSpawnOffsets { get; set; } = new Vector2[]
    {
        new(-17, -4), new(17, -4), new(-10, 3), new(10, 3), new(-15, 10),
        new(15, 10), new(-11, -33), new(11, -33), new(0, 0)
    };
}
