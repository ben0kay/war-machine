// WorldSettings authoring defaults; runtime owners duplicate resources before editing.
using Godot;

[GlobalClass]
public partial class WorldSettings : Resource
{
    public const string TerrainDirectory = "res://WORLD/Terrain/Gameplay";
    public const int TerrainSize = 1024;
    [ExportGroup("Terrain generation")]
    [Export] public int Seed { get; set; } = 48193;
    [Export] public Vector2 SpawnXZ { get; set; } = new(512, 512);
    [Export] public float GroundHeight { get; set; } = 4;
    [Export] public float Relief { get; set; } = 0.15f;
    [Export] public Vector2 HillOffset { get; set; } = new(30, -10);
    [Export] public float HillHeight { get; set; } = 5;
    [Export] public float HillRadius { get; set; } = 24;
    [ExportGroup("Structures")]
    [Export] public Vector2 Building001Offset { get; set; } = new(4, -32);
    [Export] public Vector2 Building002Offset { get; set; } = new(-34, -42);
    [Export] public Vector2 Building003Offset { get; set; } = new(42, 32);
    [Export] public Vector2 Building004Offset { get; set; } = new(-34, -70);
    [Export] public Vector2 Car001Offset { get; set; } = new(24, 42);
    [ExportGroup("Terrain3D mountain test")]
    [Export] public bool CliffEnabled { get; set; } = true;
    [Export] public Vector2 CliffOffset { get; set; } = new(-6, 76);
    [Export] public float CliffHeight { get; set; } = 12;
    [Export] public float CliffWidth { get; set; } = 44;
    [Export] public float CliffDepth { get; set; } = 36;
    [Export] public float CliffRampLength { get; set; } = 38;
    [Export] public float CliffPathWidth { get; set; } = 10;
    [ExportGroup("Horde preview (top-down only)")]
    [Export(PropertyHint.Range, "0,60,1")] public int HordeCount { get; set; } = 20;
    [Export(PropertyHint.Range, "0,60,1")] public int HordeRunnerCount { get; set; } = 6;
    [Export] public Vector2 HordeCenterOffset { get; set; } = new(-38, -16);
    [Export(PropertyHint.Range, "1.8,4,0.1")] public float HordeSpacing { get; set; } = 2.5f;
    [ExportGroup("Runner placement")]
    [Export] public Vector2[] RunnerSpawnOffsets { get; set; } = new Vector2[]
    { new(18, 22), new(-15, 30), new(32, -18) };
    [ExportGroup("Zombie placement")]
    // Twelve preview zombies, spread around spawn and clear of the robot's initial patrol route.
    [Export] public Vector2[] ZombieSpawnOffsets { get; set; } = new Vector2[]
    {
        new(8, 6), new(12, 10), new(-8, -10), new(-13, -8), new(4, -15), new(16, 16),
        new(20, -20), new(26, 4), new(8, 29), new(-9, 23), new(-20, -22), new(-29, -7)
    };
}
