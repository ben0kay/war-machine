// Standalone TD0–TD2 composition: saved preview terrain, test human, ortho camera and the existing reusable robot.
using Godot;
using System.IO;

public partial class TopDownWorldTest : Node3D
{
    [Export] public TopDownTestSettings Settings { get; set; } = null!;

    // =========================================================
    // Runtime settings remain separate from both the authoring master and original world resources.
    public override void _Ready()
    { Settings = (TopDownTestSettings)Settings.Duplicate(true); SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(InitializePreview)); }

    // =========================================================
    // Missing data is a visible setup error, never a reason to bake terrain during gameplay.
    public void InitializePreview()
    {
        var status = GetNode<Label>("Controls/Status");
        string directory = ProjectSettings.GlobalizePath(TopDownTestSettings.TerrainDirectory);
        if (!Directory.Exists(directory) || Directory.GetFiles(directory, "*.res").Length == 0)
        { status.Text = "Preview terrain not baked.\nRun WORLD/TopDownWorldTest/Terrain/BakePreviewTerrain.tscn once (F6),\nthen run this scene again. The main world is unchanged."; GD.PushWarning(status.Text); return; }
        var terrain = GetNode<Node3D>("Terrain3D");
        var data = terrain.Get("data").AsGodotObject();
        Vector3 spawn = new(Settings.SpawnXZ.X, 0, Settings.SpawnXZ.Y);
        float height = data.Call("get_height", spawn).AsSingle();
        if (!float.IsFinite(height)) { status.Text = "Saved preview terrain has no ground at spawn; inspect the preview bake/settings."; GD.PushError(status.Text); return; }
        var player = GetNode<TopDownPlayer>("Player");
        player.GlobalPosition = new Vector3(spawn.X, height + 0.05f, spawn.Z);
        var camera = GetNode<TopDownCamera>("Camera");
        camera.Initialize(player, Settings);
        terrain.Call("set_camera", camera);
        // Full/Game collision for this one test region avoids dependence on the offset camera's dynamic patch.
        player.Initialize(Settings, camera);
        var cameraControls = new TopDownCameraControls();
        GetNode<CanvasLayer>("Controls").AddChild(cameraControls);
        cameraControls.Initialize(camera, Settings);
        bool patrolReady = GetNode<TopDownRobotPlacement>("RobotPatrol").Initialize(terrain, player, Settings);
        status.Text = "Top-down test · WASD screen-relative walk · Shift run · Wheel zoom\nF6 in editor: Run Current Scene"
            + (patrolReady ? " · Robot patrol west/southwest of spawn" : "\nRobot placement failed; see Output. Player preview remains available.");
    }
}
