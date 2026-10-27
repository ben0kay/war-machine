// Active world composition: saved terrain, reusable player/camera, entities, equipment and environment.
using Godot;
using System.IO;

public partial class WorldController : Node3D
{
    [Export] public WorldSettings Settings { get; set; } = null!;
    [Export] public PlayerSettings PlayerSettings { get; set; } = null!;
    [Export] public CameraSettings CameraSettings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    private PinePlacement _pines = null!;
    private TerrainGrass _grass = null!;
    private PlayerController _player = null!;
    private WeatherBridge _weather = null!;
    private Camera _camera = null!;

    // =========================================================
    // Runtime settings remain separate from both the authoring master and original world resources.
    public override void _Ready()
    {
        Settings = (WorldSettings)Settings.Duplicate(true);
        PlayerSettings = (PlayerSettings)PlayerSettings.Duplicate(true);
        CameraSettings = (CameraSettings)CameraSettings.Duplicate(true);
        GameplayTerrainStorage.CopyLegacyBakeIfNeeded();
        EnvironmentSettings = (EnvironmentConfig)EnvironmentSettings.Duplicate(true);
        var sky = GD.Load<PackedScene>("res://ENVIRONMENT/Sky/WildernessSky.tscn").Instantiate<Node>();
        AddChild(sky);
        // A readable ambient floor survives a below-horizon/new moon; Sky3D still owns its colour and sun/moon motion.
        var environment = ((WorldEnvironment)sky).Environment;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        environment.AmbientLightSkyContribution = 0;
        environment.AmbientLightEnergy = Mathf.Max(0, EnvironmentSettings.AmbientLightEnergy);
        sky.GetNode("SkyDome").Set("moon_light_energy", Mathf.Max(0, EnvironmentSettings.MoonlightEnergy));
        var skyController = new SkyController { Name = "Environment", Sky = sky, Settings = EnvironmentSettings };
        AddChild(skyController);
        // This panel owns time controls; leave the parked FPS world's P/F8 shortcuts untouched.
        skyController.SetProcessUnhandledInput(false);
        SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(InitializeWorld));
    }

    // =========================================================
    // Missing data is a visible setup error, never a reason to bake terrain during gameplay.
    public void InitializeWorld()
    {
        var status = GetNode<Label>("Controls/Status");
        string directory = ProjectSettings.GlobalizePath(WorldSettings.TerrainDirectory);
        if (!Directory.Exists(directory) || Directory.GetFiles(directory, "*.res").Length == 0)
        { status.Text = "Preview terrain not baked.\nRun WORLD/Generation/BakeGameplayTerrain.tscn once (F6),\nthen run this scene again. The main world is unchanged."; GD.PushWarning(status.Text); return; }
        var terrain = GetNode<Node3D>("Terrain3D");
        terrain.Set("data_directory", WorldSettings.TerrainDirectory);
        if (!BindTerrainAppearance(terrain, out string appearanceError))
        { status.Text = appearanceError; GD.PushError(appearanceError); return; }
        var data = terrain.Get("data").AsGodotObject();
        Vector3 spawn = new(Settings.SpawnXZ.X, 0, Settings.SpawnXZ.Y);
        float height = data.Call("get_height", spawn).AsSingle();
        if (!float.IsFinite(height)) { status.Text = "Saved preview terrain has no ground at spawn; inspect the preview bake/settings."; GD.PushError(status.Text); return; }
        var player = GetNode<PlayerController>("Player");
        player.GlobalPosition = new Vector3(spawn.X, height + 0.05f, spawn.Z);
        var camera = GetNode<Camera>("Camera");
        camera.Initialize(player, CameraSettings);
        terrain.Call("set_camera", camera);
        // Full/Game collision for this one test region avoids dependence on the offset camera's dynamic patch.
        player.Initialize(PlayerSettings, camera);
        var equipment = new PlayerWeaponController { Name = "Equipment" }; player.AddChild(equipment);
        equipment.Initialize(player, camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"));
        _camera = camera; _player = player;
        _grass = new TerrainGrass { Name = "Grass",
            Settings = (VegetationConfig)GD.Load<VegetationConfig>("res://VEGETATION/Configuration/VegetationMaster.tres").Duplicate(true),
            EnvironmentSettings = EnvironmentSettings };
        AddChild(_grass); _grass.InitializeView(terrain, player, camera);
        player.PositionChanged += _grass.UpdateAnchor;
        Rect2 buildingClearing = PlaceBuilding(data);
        _pines = new PinePlacement { Name = "Pines",
            Settings = (PineSettings)GD.Load<PineSettings>("res://VEGETATION/Trees/Pine/Configuration/PineMaster.tres").Duplicate(true),
            EnvironmentSettings = EnvironmentSettings };
        if (buildingClearing.Size != Vector2.Zero) _pines.ExclusionAreas.Add(buildingClearing);
        // Keep initial zombie positions free of newly solid trunks.
        foreach (Vector2 offset in Settings.ZombieSpawnOffsets)
            _pines.ExclusionAreas.Add(new Rect2(Settings.SpawnXZ + offset - Vector2.One * 2, Vector2.One * 4));
        AddChild(_pines); _pines.InitializeView(terrain, player, WorldSettings.TerrainSize, Settings.SpawnXZ);
        player.PositionChanged += _pines.UpdateAnchor;
        var occlusion = new CameraOcclusion { Name = "Occlusion" }; camera.AddChild(occlusion);
        occlusion.Initialize(camera, player, CameraSettings); occlusion.RegisterProvider(_pines);
        _weather = new WeatherBridge { Name = "Weather" }; AddChild(_weather);
        _weather.InitializeView(camera, player, EnvironmentSettings);
        camera.SettingsChanged += UpdateWeatherCoverage;
        GetViewport().SizeChanged += UpdateWeatherCoverage;
        UpdateWeatherCoverage();
        var cameraControls = new WorldTuningPanel();
        GetNode<CanvasLayer>("Controls").AddChild(cameraControls);
        cameraControls.Initialize(camera, CameraSettings, GetNode<SkyController>("Environment"), _weather, _grass, _pines);
        SpawnZombies(data, player);
        bool patrolReady = GetNode<RobotPlacement>("RobotPatrol").Initialize(terrain, player, Settings);
        status.Text = "World · Arrows screen-relative walk · Shift run · Right Ctrl crouch · Mouse aim · Left click/hold fire · Wheel zoom\nF6 in editor: Run Current Scene"
            + (patrolReady ? " · Robot patrol west/southwest of spawn · Building001 north of spawn" : "\nRobot placement failed; see Output. Player preview remains available.");
    }

    // =========================================================
    // Loading an initially empty native terrain can enable its checker debug view. Bind textures first, then clear it.
    private static bool BindTerrainAppearance(Node3D terrain, out string error)
    {
        error = "";
        var sourceAssets = GD.Load<Resource>("res://WORLD/Terrain/TerrainAssets.tres");
        var sourceMaterial = GD.Load<Resource>("res://WORLD/Terrain/TerrainMaterial.tres");
        if (sourceAssets == null || sourceMaterial == null)
        { error = "Terrain appearance resources could not load. See Output for missing resource/import errors."; return false; }
        // New native resource instances force binding even if the scene's original resource reference was already assigned.
        var assets = sourceAssets.Duplicate();
        int count = assets.Call("get_texture_count").AsInt32();
        if (count < 2)
        { error = $"Terrain texture list failed to load ({count}/2 layers). Reimport project assets; see Output."; return false; }
        terrain.Set("assets", assets);
        var material = sourceMaterial.Duplicate();
        terrain.Set("material", material);
        // Set after native initialization, which can enable checkered mode while the asset list is empty.
        material.Set("show_checkered", false);
        material.Set("show_colormap", false);
        GD.Print($"Active terrain: {count} texture layers bound; checker debug view disabled.");
        return true;
    }

    // =========================================================
    // Place the reusable depot on existing terrain, east of the robot's route and north of the player; never reshape terrain.
    private Rect2 PlaceBuilding(GodotObject terrainData)
    {
        Vector2 location = Settings.SpawnXZ + Settings.Building001Offset;
        float height = terrainData.Call("get_height", new Vector3(location.X, 0, location.Y)).AsSingle();
        if (!float.IsFinite(height)) { GD.PushError("Building001 placement has no terrain; structure skipped."); return default; }
        var building = GD.Load<PackedScene>("res://WORLD/Content/Structures/Building001/Building001.tscn").Instantiate<Building001Mesh>();
        var structures = new Node3D { Name = "Structures" }; AddChild(structures);
        building.Position = new Vector3(location.X, height, location.Y); structures.AddChild(building);
        float width = Mathf.Clamp(building.Settings.Width, 16, 32), depth = Mathf.Clamp(building.Settings.Depth, 10, 24);
        GD.Print($"Building001 placed at {building.GlobalPosition}; offset {Settings.Building001Offset} from spawn.");
        // Include the front (+Z) loading apron and walking approach, not just the wall footprint.
        return new Rect2(location - new Vector2(width * 0.5f + 3, depth * 0.5f + 3), new Vector2(width + 6, depth + 11));
    }

    // =========================================================
    // Scene-local placement only; all zombie behaviour, artwork and defaults live under ENTITIES/TYPES/Zombies.
    private void SpawnZombies(GodotObject terrainData, PlayerController player)
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
    private void UpdateWeatherCoverage()
    {
        _weather.SetOrthographicCoverage(_camera.ViewHeight);
        _grass.UpdateView(_camera.ViewHeight, CameraSettings.CameraDistance);
        _pines.UpdateView(CameraSettings.CameraDistance);
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_camera)) _camera.SettingsChanged -= UpdateWeatherCoverage;
        if (IsInstanceValid(_player) && IsInstanceValid(_pines)) _player.PositionChanged -= _pines.UpdateAnchor;
        if (IsInstanceValid(_player) && IsInstanceValid(_grass)) _player.PositionChanged -= _grass.UpdateAnchor;
        GetViewport().SizeChanged -= UpdateWeatherCoverage;
    }
}
