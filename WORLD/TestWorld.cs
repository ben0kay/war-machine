// Places the reusable explorer above saved Terrain3D data; never generates terrain on launch.
using Godot;

public partial class TestWorld : Node3D
{
    [Export] public WorldConfig Settings { get; set; } = null!;

    // =========================================================
    // Select the saved data from the world master before Terrain3D initializes.
    public override void _EnterTree()
    {
        var environment = GetNode<SkyController>("Environment");
        environment.Settings = (EnvironmentConfig)environment.Settings.Duplicate();
        var grass = GetNode<TerrainGrass>("Vegetation");
        grass.Settings = (VegetationConfig)grass.Settings.Duplicate(); grass.EnvironmentSettings = environment.Settings;
        var trees = GetNode<TreePlacement>("Trees");
        trees.Settings = (TreeSettings)trees.Settings.Duplicate(); trees.EnvironmentSettings = environment.Settings;
        GetNode<Node>("Terrain3D").Set("data_directory", Settings.DataDirectory);
    }

    // =========================================================
    // Defer terrain sampling until Terrain3D has completed initialization.
    public override void _Ready() => CallDeferred(nameof(PlaceExplorer));

    // =========================================================
    // Fail visibly on missing baked terrain rather than silently regenerating authored data.
    public void PlaceExplorer()
    {
        var terrain = GetNode<Node3D>("Terrain3D");
        var data = terrain.Get("data").AsGodotObject();
        var position = new Vector3(Settings.SpawnXZ.X, 0, Settings.SpawnXZ.Y);
        float height = (float)data.Call("get_height", position).AsDouble();
        if (!float.IsFinite(height))
        { GD.PushError("Saved terrain missing at spawn. Run the explicit terrain bake; gameplay does not regenerate it."); return; }
        var explorer = GetNode<Node3D>("Explorer");
        explorer.Position = new Vector3(position.X, height + Settings.SpawnClearance, position.Z);
        explorer.LookAt(new Vector3(Settings.Size * 0.5f, height + 10, Settings.Size * 0.3f));
        terrain.Call("set_camera", explorer.GetNode<Camera3D>("Camera3D"));
        GetNode<TerrainGrass>("Vegetation").Initialize(terrain, (ExplorationController)explorer);
        GetNode<TreePlacement>("Trees").Initialize(terrain, (ExplorationController)explorer, Settings);
        SetProcess(false); SetPhysicsProcess(false);
    }
}
