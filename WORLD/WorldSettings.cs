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
    [ExportGroup("Zombie placement")]
    // Twelve preview zombies, spread around spawn and clear of the robot's initial patrol route.
    [Export] public Vector2[] ZombieSpawnOffsets { get; set; } = new Vector2[]
    {
        new(8, 6), new(12, 10), new(-8, -10), new(-13, -8), new(4, -15), new(16, 16),
        new(20, -20), new(26, 4), new(8, 29), new(-9, 23), new(-20, -22), new(-29, -7)
    };
}
