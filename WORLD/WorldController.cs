// Active world composition: saved terrain, reusable player/camera, entities, equipment and environment.
using Godot;
using System.IO;

public partial class WorldController : Node3D
{
    [Export] public WorldSettings Settings { get; set; } = null!;
    [Export] public PlayerSettings PlayerSettings { get; set; } = null!;
    [Export] public CameraSettings CameraSettings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    [Export] public VegetationConfig GrassSettings { get; set; } = null!;
    protected PinePlacement _pines = null!;
    protected TerrainGrass _grass = null!;
    private PlayerController _player = null!;
    protected WeatherBridge _weather = null!;
    protected Camera _camera = null!;
    private PlayerDistanceFog? _distanceFog;

    // =========================================================
    // Shared by the overhead panel and inherited FPS crosshair; resolved only during startup.
    protected CanvasLayer DebugControls => GetNode<CanvasLayer>("DebugUIRoot/Controls");

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
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(sky);
        // A readable ambient floor survives a below-horizon/new moon; Sky3D still owns its colour and sun/moon motion.
        var environment = ((WorldEnvironment)sky).Environment;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        environment.AmbientLightSkyContribution = 0;
        environment.AmbientLightEnergy = Mathf.Max(0, EnvironmentSettings.AmbientLightEnergy);
        sky.GetNode("SkyDome").Set("moon_light_energy", Mathf.Max(0, EnvironmentSettings.MoonlightEnergy));
        var skyController = new SkyController { Name = "Environment", Sky = sky, Settings = EnvironmentSettings };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(skyController);
        // This panel owns time controls; leave the parked FPS world's P/F8 shortcuts untouched.
        skyController.SetProcessUnhandledInput(false);
        SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(InitializeWorld));
    }

    // =========================================================
    // Missing data is a visible setup error, never a reason to bake terrain during gameplay.
    public void InitializeWorld()
    {
        var status = DebugControls.GetNode<Label>("Status");
        string directory = ProjectSettings.GlobalizePath(WorldSettings.TerrainDirectory);
        if (!Directory.Exists(directory) || Directory.GetFiles(directory, "*.res").Length == 0)
        { status.Text = "Preview terrain not baked.\nRun WORLD/Generation/BakeGameplayTerrain.tscn once (F6),\nthen run this scene again. The main world is unchanged."; GD.PushWarning(status.Text); return; }
        var terrain = GetNode<Node3D>("TerrainRoot/Terrain3D");
        terrain.Set("data_directory", WorldSettings.TerrainDirectory);
        if (!BindTerrainAppearance(terrain, out string appearanceError))
        { status.Text = appearanceError; GD.PushError(appearanceError); return; }
        var data = terrain.Get("data").AsGodotObject();
        Vector3 spawn = new(Settings.SpawnXZ.X, 0, Settings.SpawnXZ.Y);
        float height = data.Call("get_height", spawn).AsSingle();
        if (!float.IsFinite(height)) { status.Text = "Saved preview terrain has no ground at spawn; inspect the preview bake/settings."; GD.PushError(status.Text); return; }
        var player = GetNode<PlayerController>("WorldActorsRoot/PersistentEntitiesRoot/Player");
        player.GlobalPosition = new Vector3(spawn.X, height + 0.05f, spawn.Z);
        var camera = GetNode<Camera>("Camera");
        camera.Initialize(player, CameraSettings);
        terrain.Call("set_camera", camera);
        // Full/Game collision for this one test region avoids dependence on the offset camera's dynamic patch.
        player.Initialize(PlayerSettings, camera);
        InitializeEquipment(player, camera);
        _camera = camera; _player = player;
        var sceneryClearings = PlaceScenery(data, out var grassClearings);
        _grass = new TerrainGrass { Name = "Grass",
            Settings = (VegetationConfig)(GrassSettings ?? GD.Load<VegetationConfig>("res://VEGETATION/Configuration/VegetationMaster.tres")).Duplicate(true),
            EnvironmentSettings = EnvironmentSettings };
        _grass.SetExclusionAreas(grassClearings);
        GetNode("SystemsRoot/VegetationRoot").AddChild(_grass); _grass.InitializeView(terrain, player, camera);
        player.PositionChanged += _grass.UpdateAnchor;
        _pines = new PinePlacement { Name = "Pines",
            Settings = (PineSettings)GD.Load<PineSettings>("res://VEGETATION/Trees/Pine/Configuration/PineMaster.tres").Duplicate(true),
            EnvironmentSettings = EnvironmentSettings };
        foreach (Rect2 clearing in sceneryClearings) _pines.ExclusionAreas.Add(clearing);
        // Keep initial zombie positions free of newly solid trunks.
        foreach (Vector2 offset in Settings.ZombieSpawnOffsets)
            _pines.ExclusionAreas.Add(new Rect2(Settings.SpawnXZ + offset - Vector2.One * 2, Vector2.One * 4));
        GetNode("SystemsRoot/VegetationRoot").AddChild(_pines); _pines.InitializeView(terrain, player, WorldSettings.TerrainSize, Settings.SpawnXZ);
        player.PositionChanged += _pines.UpdateAnchor;
        InitializeOcclusion(camera, player);
        _weather = new WeatherBridge { Name = "Weather" }; GetNode("SystemsRoot/EnvironmentRoot").AddChild(_weather);
        _weather.InitializeView(camera, player, EnvironmentSettings);
        camera.SettingsChanged += UpdateWeatherCoverage;
        GetViewport().SizeChanged += UpdateWeatherCoverage;
        UpdateWeatherCoverage();
        InitializeDistanceFog(camera, player);
        InitializeControls(camera);
        SpawnZombies(data, player);
        bool patrolReady = GetNode<RobotPlacement>("WorldActorsRoot/PersistentEntitiesRoot/RobotPatrol").Initialize(terrain, player, Settings);
        status.Text = ControlsHelp
            + (patrolReady ? " · Robot patrol west/southwest of spawn · Depot north · Lodge northwest · Fuel station and car southeast" : "\nRobot placement failed; see Output. Player preview remains available.");
    }

    // =========================================================
    // View-specific adapters are replaceable; terrain, scenery and entity composition remain shared.
    protected virtual string ControlsHelp => "World · Arrows screen-relative walk · Shift run · Right Ctrl crouch · Mouse aim · Left click/hold fire · Wheel zoom\nF6 in editor: Run Current Scene";
    protected virtual void InitializeEquipment(PlayerController player, Camera camera)
    {
        var equipment = new PlayerWeaponController { Name = "Equipment" }; player.AddChild(equipment);
        equipment.Initialize(player, camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"));
    }

    // =========================================================
    protected virtual void InitializeOcclusion(Camera camera, PlayerController player)
    {
        var occlusion = new CameraOcclusion { Name = "Occlusion" }; camera.AddChild(occlusion);
        occlusion.Initialize(camera, player, CameraSettings); occlusion.RegisterProvider(_pines);
    }

    // =========================================================
    // Top-down visibility atmosphere is optional presentation; FPS overrides this hook without changing its world.
    protected virtual void InitializeDistanceFog(Camera camera, PlayerController player)
    {
        _distanceFog = new PlayerDistanceFog { Name = "PlayerDistanceFog" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(_distanceFog);
        _distanceFog.Initialize(camera, player, EnvironmentSettings);
    }

    // =========================================================
    protected virtual void InitializeControls(Camera camera)
    {
        var panel = new WorldTuningPanel(); DebugControls.AddChild(panel);
        panel.Initialize(camera, CameraSettings, GetNode<SkyController>("SystemsRoot/EnvironmentRoot/Environment"), _weather, _grass, _pines, _distanceFog);
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
    // One shared placement pass serves both top-down and inherited FPS views; no terrain is edited.
    private Godot.Collections.Array<Rect2> PlaceScenery(GodotObject terrainData, out Godot.Collections.Array<Rect2> grassClearings)
    {
        var clearings = new Godot.Collections.Array<Rect2>();
        grassClearings = new Godot.Collections.Array<Rect2>();
        var structures = GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures");
        var objects = GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Objects/Cars");
        PlaceObject(terrainData, structures, "res://WORLD/Content/Structures/Building001/Building001.tscn", Settings.Building001Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, structures, "res://WORLD/Content/Structures/Building002/Building002.tscn", Settings.Building002Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, structures, "res://WORLD/Content/Structures/Building003/Building003.tscn", Settings.Building003Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, objects, "res://WORLD/Content/Objects/Cars/Car001/Car001.tscn", Settings.Car001Offset, 2, 0, clearings, grassClearings);
        return clearings;
    }

    // =========================================================
    // Ground each prefab once, then reserve its actual generated mesh bounds including porches, pumps and open doors.
    private void PlaceObject(GodotObject terrainData, Node3D parent, string scenePath, Vector2 offset,
        float margin, float approach, Godot.Collections.Array<Rect2> clearings, Godot.Collections.Array<Rect2> grassClearings)
    {
        Vector2 location = Settings.SpawnXZ + offset;
        float height = terrainData.Call("get_height", new Vector3(location.X, 0, location.Y)).AsSingle();
        if (!float.IsFinite(height)) { GD.PushError($"No terrain for {scenePath}; scenery skipped."); return; }
        var instance = GD.Load<PackedScene>(scenePath).Instantiate<Node3D>();
        instance.Position = new Vector3(location.X, height, location.Y); parent.AddChild(instance);
        Vector2 low = location, high = location;
        // Prefabs build synchronously in Ready; inspect only these four subtrees, once at startup.
        foreach (Node child in instance.FindChildren("*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)child;
            if (mesh.Mesh == null) continue;
            Aabb bounds = mesh.GetAabb();
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = mesh.GlobalTransform * bounds.GetEndpoint(i);
                var xz = new Vector2(point.X, point.Z); low = low.Min(xz); high = high.Max(xz);
            }
        }
        // Grass only clears the actual prefab envelope, with a small edge buffer for leaning blades.
        // Tree approach/spacing margins below remain independent so grass can grow beside the building.
        grassClearings.Add(new Rect2(low, high - low).Grow(0.3f));
        // All building doors face +Z. The car receives symmetric clearance for its swung-open driver door.
        low -= Vector2.One * margin; high += Vector2.One * margin + new Vector2(0, approach);
        clearings.Add(new Rect2(low, high - low));
        GD.Print($"{instance.Name} placed at {instance.GlobalPosition}; offset {offset} from spawn.");
    }

    // =========================================================
    // Scene-local placement only; all zombie behaviour, artwork and defaults live under ENTITIES/TYPES/Zombies.
    private void SpawnZombies(GodotObject terrainData, PlayerController player)
    {
        var group = GetNode<Node3D>("WorldActorsRoot/PersistentEntitiesRoot/Zombies");
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
    protected virtual void UpdateWeatherCoverage()
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
