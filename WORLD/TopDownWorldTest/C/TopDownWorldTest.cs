// Standalone TD0–TD2 composition: saved preview terrain, test human, ortho camera and the existing reusable robot.
using Godot;
using System.IO;

public partial class TopDownWorldTest : Node3D
{
    [Export] public TopDownTestSettings Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    private WeatherBridge _weather = null!;
    private TopDownCamera _camera = null!;

    // =========================================================
    // Runtime settings remain separate from both the authoring master and original world resources.
    public override void _Ready()
    {
        Settings = (TopDownTestSettings)Settings.Duplicate(true);
        EnvironmentSettings = (EnvironmentConfig)EnvironmentSettings.Duplicate(true);
        var sky = GD.Load<PackedScene>("res://ENVIRONMENT/Sky/WildernessSky.tscn").Instantiate<Node>();
        AddChild(sky);
        // A readable ambient floor survives a below-horizon/new moon; Sky3D still owns its colour and sun/moon motion.
        var environment = ((WorldEnvironment)sky).Environment;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        environment.AmbientLightSkyContribution = 0;
        environment.AmbientLightEnergy = Mathf.Max(0, Settings.AmbientLightEnergy);
        sky.GetNode("SkyDome").Set("moon_light_energy", Mathf.Max(0, Settings.MoonlightEnergy));
        var skyController = new SkyController { Name = "Environment", Sky = sky, Settings = EnvironmentSettings };
        AddChild(skyController);
        // This panel owns time controls; leave the parked FPS world's P/F8 shortcuts untouched.
        skyController.SetProcessUnhandledInput(false);
        SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(InitializePreview));
    }

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
        var equipment = new PlayerWeaponController { Name = "Equipment" }; player.AddChild(equipment);
        equipment.Initialize(player, camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"));
        _camera = camera;
        _weather = new WeatherBridge { Name = "Weather" }; AddChild(_weather);
        _weather.InitializeView(camera, player, EnvironmentSettings);
        camera.SettingsChanged += UpdateWeatherCoverage;
        GetViewport().SizeChanged += UpdateWeatherCoverage;
        UpdateWeatherCoverage();
        var cameraControls = new TopDownCameraControls();
        GetNode<CanvasLayer>("Controls").AddChild(cameraControls);
        cameraControls.Initialize(camera, Settings, GetNode<SkyController>("Environment"), _weather);
        SpawnZombies(data, player);
        bool patrolReady = GetNode<TopDownRobotPlacement>("RobotPatrol").Initialize(terrain, player, Settings);
        status.Text = "Top-down test · Arrows screen-relative walk · Shift run · Right Ctrl crouch · Mouse aim · Left click/hold fire · Wheel zoom\nF6 in editor: Run Current Scene"
            + (patrolReady ? " · Robot patrol west/southwest of spawn" : "\nRobot placement failed; see Output. Player preview remains available.");
    }

    // =========================================================
    // Scene-local placement only; all zombie behaviour, artwork and defaults live under ENTITIES/TYPES/Zombies.
    private void SpawnZombies(GodotObject terrainData, TopDownPlayer player)
    {
        var group = new Node3D { Name = "Zombies" }; AddChild(group);
        var scene = GD.Load<PackedScene>("res://ENTITIES/TYPES/Zombies/Zombie.tscn");
        for (int i = 0; i < Settings.ZombieSpawnOffsets.Length; i++)
        {
            Vector2 offset = Settings.ZombieSpawnOffsets[i];
            Vector3 position = new(Settings.SpawnXZ.X + offset.X, 0, Settings.SpawnXZ.Y + offset.Y);
            float height = terrainData.Call("get_height", position).AsSingle();
            if (!float.IsFinite(height)) { GD.PushWarning("Skipped zombie spawn outside preview terrain."); continue; }
            var zombie = scene.Instantiate<ZombieController>();
            group.AddChild(zombie); zombie.GlobalPosition = new Vector3(position.X, height + 0.05f, position.Z);
            zombie.InitializeTarget(player, unchecked((uint)(Settings.Seed + i * 7919)));
        }
    }

    // =========================================================
    // Coverage changes only on zoom/pitch input or viewport resize; clear weather remains dormant.
    private void UpdateWeatherCoverage() => _weather.SetOrthographicCoverage(_camera.ViewHeight);

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_camera)) _camera.SettingsChanged -= UpdateWeatherCoverage;
        GetViewport().SizeChanged -= UpdateWeatherCoverage;
    }
}
