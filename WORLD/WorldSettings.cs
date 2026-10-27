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
    [Export] public Vector2 Car001Offset { get; set; } = new(24, 42);
    [ExportGroup("Cliff test")]
    [Export] public bool CliffEnabled { get; set; } = true;
    [Export] public Vector2 CliffOffset { get; set; } = new(-4, 56);
    [Export] public float CliffHeight { get; set; } = 9;
    [Export] public float CliffWidth { get; set; } = 26;
    [Export] public float CliffDepth { get; set; } = 24;
    [Export] public float CliffRampLength { get; set; } = 24;
    [Export] public float CliffPathWidth { get; set; } = 6;
    [ExportGroup("Zombie placement")]
    // Twelve preview zombies, spread around spawn and clear of the robot's initial patrol route.
    [Export] public Vector2[] ZombieSpawnOffsets { get; set; } = new Vector2[]
    {
        new(8, 6), new(12, 10), new(-8, -10), new(-13, -8), new(4, -15), new(16, 16),
        new(20, -20), new(26, 4), new(8, 29), new(-9, 23), new(-20, -22), new(-29, -7)
    };
}
