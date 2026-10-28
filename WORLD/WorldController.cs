// Active world composition: saved terrain, reusable player/camera, entities, equipment and environment.
using Godot;
using System.IO;

public partial class WorldController : Node3D
{
    [Export] public WorldSettings Settings { get; set; } = null!;
    [Export] public PlayerSettings PlayerSettings { get; set; } = null!;
    [Export] public CameraSettings CameraSettings { get; set; } = null!;
    public EnvironmentConfig EnvironmentSettings { get; private set; } = null!;
    [Export] public VegetationConfig GrassSettings { get; set; } = null!;
    [ExportGroup("Debug")]
    [Export] public bool ShowZombieSightConesOnStart { get; set; } = false;
    protected PinePlacement _pines = null!;
    protected TerrainGrass _grass = null!;
    private PlayerController _player = null!;
    protected WorldAudio _audio = null!;
    protected WeatherBridge _weather = null!;
    protected Camera _camera = null!;
    private PlayerDistanceFog? _distanceFog;
    private WorldRenderCulling _renderCulling = null!;
    protected GroundHaze? _groundHaze;
    protected GameEnvironment _environmentScene = null!;
    private ZombieVisionDebug _visionDebug = null!;
    private ContentScatter _rocks = null!;
    private VillageLayout? _village;
    private CorpseManager? _corpses;
    private WorldManifest? _sectionManifest;
    private WorldSectionDiagnostics? _sectionDiagnostics;
    private Node3D _startupTerrain = null!;
    private GodotObject _startupData = null!;
    private Godot.Collections.Array<Rect2> _startupMountain = new();
    private int _startupTicks;

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
        _audio = GetNode<WorldAudio>("SystemsRoot/Audio");
        _environmentScene = GetNode<GameEnvironment>("SystemsRoot/EnvironmentRoot");
        EnvironmentSettings = _environmentScene.Settings;
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
        var mountainPath = new Godot.Collections.Array<Rect2>();
        if (Settings.VillageEnabled)
        {
            _village = new VillageLayout();
            var structures = GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures");
            if (!_village.Prepare(structures, GetNode<RobotPlacement>("WorldActorsRoot/PersistentEntitiesRoot/RobotPatrol"), data, Settings)
                || !VillageTerrain.Apply(terrain, data, Settings, _village))
            { status.Text = "Village placement failed; actors remain dormant. See Output for terrain/layout errors."; return; }
            var props = GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot");
            VillageRoadMesh.Build(props, _village.Roads, _village.Level);
        }
        else
        {
            GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures/ConvenienceStoreSite001").QueueFree();
            mountainPath = GameplayMountain.Apply(terrain, data, Settings);
        }
        Vector3 spawn = new(Settings.SpawnXZ.X, 0, Settings.SpawnXZ.Y);
        float height = data.Call("get_height", spawn).AsSingle();
        if (!float.IsFinite(height)) { status.Text = "Saved preview terrain has no ground at spawn; inspect the preview bake/settings."; GD.PushError(status.Text); return; }
        var player = GetNode<PlayerController>("WorldActorsRoot/PersistentEntitiesRoot/Player");
        player.GlobalPosition = new Vector3(spawn.X, height + 0.05f, spawn.Z);
        var camera = GetNode<Camera>("Camera");
        camera.Initialize(player, CameraSettings);
        terrain.Call("set_camera", camera);
        // Finalize native coverage after data/sculpt and camera binding, before actors activate.
        var collision = terrain.Get("collision").AsGodotObject();
        collision.Call("set_mode", 3); // FULL_GAME, including distant horde spawns.
        collision.Call("build");
        // Full/Game collision for this one test region avoids dependence on the offset camera's dynamic patch.
        _startupTerrain = terrain; _startupData = data; _startupMountain = mountainPath;
        _startupTicks = 2; SetPhysicsProcess(true);
    }

    // =========================================================
    // Let native static bodies and rebuilt terrain enter the physics space before any spawn queries/AI.
    public override void _PhysicsProcess(double delta)
    {
        if (_startupTicks <= 0) { SetPhysicsProcess(false); return; }
        if (--_startupTicks > 0) return;
        SetPhysicsProcess(false); InitializeActors();
    }

    // =========================================================
    // Startup-only physics-safe phase; the world itself has no processing after actors initialize.
    private void InitializeActors()
    {
        var terrain = _startupTerrain; var data = _startupData; var mountainPath = _startupMountain;
        var player = GetNode<PlayerController>("WorldActorsRoot/PersistentEntitiesRoot/Player");
        var camera = GetNode<Camera>("Camera");
        var status = DebugControls.GetNode<Label>("Status");
        if (_village != null)
        {
            var space = player.GetWorld3D().DirectSpaceState;
            var ray = new PhysicsRayQueryParameters3D { CollisionMask = 1,
                From = player.GlobalPosition + Vector3.Up, To = player.GlobalPosition - Vector3.Up };
            var support = space.IntersectRay(ray);
            if (support.Count == 0 || support["normal"].AsVector3().Y < Mathf.Cos(player.FloorMaxAngle))
            { status.Text = "Village arrival has no safe walkable support; actors remain dormant."; return; }
            player.GlobalPosition = support["position"].AsVector3() + Vector3.Up * 0.06f;
            var shape = player.GetNode<CollisionShape3D>("CollisionShape3D");
            var clearance = new PhysicsShapeQueryParameters3D { Shape = shape.Shape,
                Transform = shape.GlobalTransform, CollisionMask = player.CollisionMask,
                Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
            if (space.IntersectShape(clearance, 1).Count != 0)
            { status.Text = "Village arrival capsule is blocked; actors remain dormant."; return; }
        }
        player.Initialize(PlayerSettings, camera);
        _audio.BindListener(player, camera);
        InitializeEquipment(player, camera);
        _camera = camera; _player = player;
        InitializeHealthFeedback(player, camera);
        InitializeAmmoHud(player);
        InitializeBackpack(player);
        // The shared F1 model spawner is debug-only, with no real prop physics or world placement.
        var spawnMenu = GD.Load<PackedScene>("res://DEBUG/PlaceableSpawner/PlaceableSpawnMenu.tscn").Instantiate<PlaceableSpawnMenu>();
        GetNode("DebugUIRoot").AddChild(spawnMenu);
        spawnMenu.Initialize(player, camera, terrain, GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot"));
        // Build one deterministic horde layout for both placement and its tree/rock clearings.
        // The inherited first-person debug scene explicitly opts out.
        var horde = EnableHorde ? BuildHordeOffsets() : (Slow: System.Array.Empty<Vector2>(), Fast: System.Array.Empty<Vector2>());
        // One separate, reversible population test using the same world spawn path in both views.
        bool storeStressEnabled = Settings.VillageEnabled && Settings.StoreStressZombiesEnabled;
        Vector2[] storeStressSlow = storeStressEnabled ? Settings.StoreStressSlowOffsets : System.Array.Empty<Vector2>();
        Vector2[] storeStressFast = storeStressEnabled ? Settings.StoreStressRunnerOffsets : System.Array.Empty<Vector2>();
        var grassClearings = new Godot.Collections.Array<Rect2>();
        var sceneryClearings = _village != null ? _village.Clearings : PlaceScenery(data, out grassClearings);
        if (_village != null) grassClearings = _village.GrassClearings;
        foreach (Rect2 cliffClearing in mountainPath)
        { sceneryClearings.Add(cliffClearing); grassClearings.Add(cliffClearing); }
        AddDeerPreviewClearing(grassClearings);
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
        // No trunks inside either existing spawns or the extra 20-zombie test cluster.
        AddZombieSpawnClearings(_pines.ExclusionAreas, Settings.RunnerSpawnOffsets);
        AddZombieSpawnClearings(_pines.ExclusionAreas, Settings.ZombieSpawnOffsets);
        AddZombieSpawnClearings(_pines.ExclusionAreas, storeStressSlow);
        AddZombieSpawnClearings(_pines.ExclusionAreas, storeStressFast);
        AddZombieSpawnClearings(_pines.ExclusionAreas, horde.Slow);
        AddZombieSpawnClearings(_pines.ExclusionAreas, horde.Fast);
        AddDeerPreviewClearing(_pines.ExclusionAreas);
        GetNode("SystemsRoot/VegetationRoot").AddChild(_pines); _pines.InitializeView(terrain, player, WorldSettings.TerrainSize, Settings.SpawnXZ);
        player.PositionChanged += _pines.UpdateAnchor;
        _rocks = new ContentScatter { Name = "Rocks",
            Settings = (ContentScatterSettings)GD.Load<ContentScatterSettings>("res://WORLD/Content/Rocks/Configuration/GameplayRockMaster.tres").Duplicate(true),
            EnvironmentSettings = EnvironmentSettings };
        foreach (Rect2 clearing in sceneryClearings) _rocks.ExclusionAreas.Add(clearing);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, Settings.ZombieSpawnOffsets);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, Settings.RunnerSpawnOffsets);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, storeStressSlow);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, storeStressFast);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, horde.Slow);
        AddZombieSpawnClearings(_rocks.ExclusionAreas, horde.Fast);
        AddDeerPreviewClearing(_rocks.ExclusionAreas);
        // Keep the giant's route and foot corridor free of new solid boulders.
        if (_village == null)
            _rocks.ExclusionAreas.Add(new Rect2(Settings.SpawnXZ + new Vector2(-38, -6), new Vector2(28, 28)));
        GetNode("WorldActorsRoot/PersistentPropsRoot").AddChild(_rocks);
        _rocks.InitializeView(terrain, player, WorldSettings.TerrainSize, Settings.SpawnXZ);
        player.PositionChanged += _rocks.UpdateAnchor;
        InitializeOcclusion(camera, player);
        _environmentScene.InitializeView(terrain, player, camera);
        _weather = _environmentScene.Weather; _groundHaze = _environmentScene.Haze;
        // GH1: explicit building/canopy occupancy, never the store site's broad parking footprint.
        // Capture once while all five village prefabs are resident; immutable zones survive WS2 unloads.
        _groundHaze.SetBuildingExclusions(GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures"));
        camera.SettingsChanged += UpdateWeatherCoverage;
        GetViewport().SizeChanged += UpdateWeatherCoverage;
        UpdateWeatherCoverage();
        InitializeDistanceFog(camera, player);
        _visionDebug = new ZombieVisionDebug { Name = "ZombieVisionDebug" };
        // =========================================================
        // Scene-owned startup default affects only debug visuals; F7/checkbox can toggle them afterward.
        _visionDebug.SetConesVisible(ShowZombieSightConesOnStart);
        GetNode("DebugUIRoot").AddChild(_visionDebug);
        InitializeControls(camera);
        InitializeSectionDiagnostics(player);
        InitializeCorpses();
        SpawnZombies(data, player, horde.Slow, horde.Fast, storeStressSlow, storeStressFast);
        SpawnDeerPreview(data, player);
        bool patrolReady = GetNode<RobotPlacement>("WorldActorsRoot/PersistentEntitiesRoot/RobotPatrol").Initialize(terrain, player, Settings, _village?.Structures);
        status.Text = ControlsHelp
            + (_village != null ? "\nVillage · Store ahead · Road east then north to depot/lodge/office · Fuel station northeast · Robot on western perimeter"
                : "\nShowcase layout · Existing scenery/robot inspection")
            + (patrolReady ? "" : "\nRobot placement failed; see Output.")
            + (horde.Slow.Length + horde.Fast.Length > 0 ? $"\nDebug horde: {horde.Slow.Length} slow + {horde.Fast.Length} fast" : "");
        InitializeStaticStreaming(player, camera);
    }

    // =========================================================
    // View-specific adapters are replaceable; terrain, scenery and entity composition remain shared.
    protected virtual string ControlsHelp => "World · Arrows screen-relative walk · Shift run · Left Ctrl/C crouch · Right Ctrl prone · Right click jump · Mouse aim · Left click/hold fire · Q shove · Wheel zoom\nF6 in editor: Run Current Scene";
    protected virtual bool EnableHorde => true;
    protected virtual void InitializeEquipment(PlayerController player, Camera camera)
    {
        var equipment = new PlayerWeaponController { Name = "Equipment" }; player.AddChild(equipment);
        equipment.Initialize(player, camera, GD.Load<EquipmentConfig>("res://EQUIPMENT/Configuration/EquipmentMaster.tres"), _audio);
    }

    // =========================================================
    protected virtual void InitializeOcclusion(Camera camera, PlayerController player)
    {
        var occlusion = new CameraOcclusion { Name = "Occlusion" }; camera.AddChild(occlusion);
        occlusion.Initialize(camera, player, CameraSettings); occlusion.RegisterProvider(_pines);
        occlusion.RegisterStructures(GetNode("WorldActorsRoot/PersistentPropsRoot/Structures"));
    }

    // =========================================================
    // Top-down visibility atmosphere is optional presentation; FPS overrides this hook without changing its world.
    protected virtual void InitializeDistanceFog(Camera camera, PlayerController player)
    {
        _distanceFog = new PlayerDistanceFog { Name = "PlayerDistanceFog" };
        GetNode("SystemsRoot/EnvironmentRoot").AddChild(_distanceFog);
        _distanceFog.Initialize(camera, player, EnvironmentSettings);
        InitializeRenderPolicy(_distanceFog, camera, player);
    }

    // =========================================================
    // Only static props and existing vegetation batches participate; actors/terrain/global effects keep their owners.
    protected void InitializeRenderPolicy(PlayerDistanceFog fog, Camera camera, PlayerController player)
    {
        _renderCulling = new WorldRenderCulling { Name = "FogRenderPolicy" }; GetNode("SystemsRoot").AddChild(_renderCulling);
        _renderCulling.Initialize(fog, camera, player, Settings.RenderPolicy,
            GetNode("WorldActorsRoot/PersistentPropsRoot"), GetNode("SystemsRoot/VegetationRoot"));
    }

    // =========================================================
    protected virtual IDebugOverlayView? OverlayView => null;

    // =========================================================
    protected virtual void InitializeControls(Camera camera)
    {
        var panel = DebugControls.GetNode<DebugOverlay>("DebugOverlay");
        panel.Initialize(camera, CameraSettings, GetNode<SkyController>("SystemsRoot/EnvironmentRoot/Environment"), _weather, _grass, _pines, _distanceFog, _groundHaze, _visionDebug, OverlayView, _renderCulling, _player);
    }

    // =========================================================
    // Read current static placements once. WS0 diagnostics never take over their lifecycle or save gameplay state.
    private void InitializeSectionDiagnostics(PlayerController player)
    {
        try
        {
            var manifest = WorldManifestCapture.Capture(GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures"), Settings.Sections, Settings.Seed);
            var diagnostic = new WorldSectionDiagnostics { Name = "WorldSections" };
            GetNode<Control>("GameplayHUD/Screen").AddChild(diagnostic);
            diagnostic.Initialize(player, manifest, Settings.Sections);
            _sectionManifest = manifest; _sectionDiagnostics = diagnostic;
            if (Settings.Sections.ShowHeading)
            {
                var status = DebugControls.GetNode<Label>("Status");
                float shift = Mathf.Max(0, 52 - status.OffsetTop);
                status.OffsetTop += shift; status.OffsetBottom += shift;
            }
        }
        catch (System.Exception error) when (error is System.ArgumentException or System.IO.InvalidDataException or System.OverflowException or System.Text.Json.JsonException)
        { GD.PushWarning($"WS0 diagnostics unavailable: {error.Message}"); }
    }

    // =========================================================
    // Views can author their streaming band without changing the shared world master or scheduler.
    protected virtual WorldStreamingSettings CreateStreamingSettings() => Settings.Streaming;

    // =========================================================
    // Adopt static village buildings only after terrain, visibility, diagnostics and actor startup have finished.
    private void InitializeStaticStreaming(PlayerController player, Camera camera)
    {
        if (_village == null || !Settings.StreamVillageBuildings || _sectionManifest == null) return;
        var bridge = new WorldStaticStreaming { Name = "WorldStreaming" };
        GetNode("SystemsRoot").AddChild(bridge);
        try
        {
            bridge.Initialize(player, camera, GetNode<Node3D>("WorldActorsRoot/PersistentPropsRoot/Structures"),
                GetNode("WorldActorsRoot/PersistentEntitiesRoot"), _sectionManifest, CreateStreamingSettings());
            _sectionDiagnostics?.BindStreaming(bridge.Streamer);
            _village.ReleaseStartupBuildingReferences();
        }
        catch (System.ArgumentException error)
        { GD.PushWarning($"WS2 streaming unavailable; startup scenery retained: {error.Message}"); }
    }

    // =========================================================
    // One independent corpse root/service for both zombie variants and both views. Other species retain their death logic.
    private void InitializeCorpses()
    {
        var root = new Node3D { Name = "Corpses" };
        GetNode("WorldActorsRoot/PersistentEntitiesRoot").AddChild(root);
        var manager = new CorpseManager { Name = "CorpseManager" }; GetNode("SystemsRoot").AddChild(manager);
        try
        {
            manager.Initialize(root, GD.Load<CorpseSettings>("res://ENTITIES/Shared/Corpses/Configuration/CorpseMaster.tres"));
            _corpses = manager;
        }
        catch (System.ArgumentException error) { GD.PushWarning($"Corpse falls disabled: {error.Message}"); }
    }

    // =========================================================
    // Shared HUD discovers current/future health components automatically; nothing ticks at full HP.
    protected virtual void InitializeHealthFeedback(PlayerController player, Camera camera)
    {
        var hud = GetNode<Control>("GameplayHUD/Screen");
        var overlay = new HealthBarOverlay { Name = "HealthBars" };
        hud.AddChild(overlay); overlay.Initialize(player, camera);
        var body = GD.Load<PackedScene>("res://FEEDBACK/Bars/PlayerStatus/BodyConditionWidget.tscn").Instantiate<BodyConditionWidget>();
        hud.AddChild(body);
        body.Initialize(player.Vitals, GD.Load<FeedbackSettings>("res://FEEDBACK/Configuration/FeedbackMaster.tres"));
    }

    // =========================================================
    // Select the live equipment slots once at startup; FPS overrides only this lookup path.
    protected virtual WeaponSlots? ResolveWeaponSlots(PlayerController player)
    { return player.GetNodeOrNull<WeaponSlots>("Equipment/WeaponSlots"); }

    // =========================================================
    // One read-only ammo panel in the existing GameplayHUD/Screen, between body silhouette and hotbar.
    private void InitializeAmmoHud(PlayerController player)
    {
        var slots = ResolveWeaponSlots(player);
        if (slots == null) { GD.PushWarning("AM1 HUD: selected weapon slots were not found."); return; }
        var hud = GD.Load<PackedScene>("res://UI/GameplayHUD/Ammo/AmmoHud.tscn").Instantiate<AmmoHud>();
        GetNode<Control>("GameplayHUD/Screen").AddChild(hud);
        hud.Initialize(slots);
    }

    // =========================================================
    // One model on the player, one presentation below the existing shared HUD (also inherited by FPS).
    private void InitializeBackpack(PlayerController player)
    {
        var backpack = new PlayerBackpack { Name = "Backpack" }; player.AddChild(backpack);
        backpack.Initialize(GD.Load<InventorySettings>("res://INVENTORY/Configuration/InventoryMaster.tres"));
        var panel = GD.Load<PackedScene>("res://UI/GameplayHUD/Inventory/Inventory.tscn").Instantiate<InventoryPanel>();
        GetNode<Control>("GameplayHUD/Screen").AddChild(panel); panel.Initialize(player, backpack);
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
        PlaceObject(terrainData, structures, "res://WORLD/Content/Assets/Structures/Building001/Building001.tscn", Settings.Building001Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, structures, "res://WORLD/Content/Assets/Structures/Building002/Building002.tscn", Settings.Building002Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, structures, "res://WORLD/Content/Assets/Structures/Building003/Building003.tscn", Settings.Building003Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, structures, "res://WORLD/Content/Assets/Structures/Building004/Building004.tscn", Settings.Building004Offset, 3, 4, clearings, grassClearings);
        PlaceObject(terrainData, objects, "res://WORLD/Content/Assets/Objects/Static/Cars/Car001/Car001.tscn", Settings.Car001Offset, 2, 0, clearings, grassClearings);
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
    // Deterministic irregular grid gives a readable, closely packed group without overlapping zombie capsules.
    // No terrain writes or runtime spawn logic; maximum count is bounded for this horde test.
    private (Vector2[] Slow, Vector2[] Fast) BuildHordeOffsets()
    {
        int count = Mathf.Clamp(Settings.HordeCount, 0, 60);
        int runners = Mathf.Clamp(Settings.HordeRunnerCount, 0, count);
        var slow = new Vector2[count - runners]; var fast = new Vector2[runners];
        if (count == 0) return (slow, fast);
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count * 1.25f));
        int rows = (count + columns - 1) / columns;
        float spacing = Mathf.Clamp(Settings.HordeSpacing, 1.8f, 4f);
        float jitter = Mathf.Min(0.32f, spacing * 0.12f);
        var rng = new RandomNumberGenerator { Seed = unchecked((ulong)(Settings.Seed + 23771)) };
        int slowIndex = 0, fastIndex = 0;
        for (int i = 0; i < count; i++)
        {
            int column = i % columns, row = i / columns;
            Vector2 offset = Settings.HordeCenterOffset + new Vector2(
                (column - (columns - 1) * 0.5f) * spacing + rng.RandfRange(-jitter, jitter),
                (row - (rows - 1) * 0.5f) * spacing + rng.RandfRange(-jitter, jitter));
            // Spread fast zombies throughout the horde, never all in one corner.
            bool runner = ((i + 1) * runners / count) > (i * runners / count);
            if (runner) fast[fastIndex++] = offset; else slow[slowIndex++] = offset;
        }
        return (slow, fast);
    }

    // =========================================================
    // Both obstacle scatter systems reserve the same initial spawn footprints, once at world startup.
    private void AddZombieSpawnClearings(Godot.Collections.Array<Rect2> exclusions, Vector2[] offsets)
    {
        foreach (Vector2 offset in offsets)
            exclusions.Add(new Rect2((Settings.VillageEnabled ? Settings.VillageOriginXZ : Settings.SpawnXZ) + offset - Vector2.One * 2, Vector2.One * 4));
    }

    // =========================================================
    // =========================================================
    // Reserve one small wildlife test footprint with the existing grass/tree/rock exclusion systems.
    private void AddDeerPreviewClearing(Godot.Collections.Array<Rect2> exclusions)
    {
        if (!Settings.DeerPreviewEnabled) return;
        Vector2 at = Settings.SpawnXZ + Settings.DeerPreviewOffset;
        exclusions.Add(new Rect2(at - Vector2.One * 2.5f, Vector2.One * 5f));
    }

    // =========================================================
    // Place one real AI deer using startup terrain/collision checks, then inject its player target.
    // DeerController owns subsequent wandering, flight and moving-only terrain sampling.
    private void SpawnDeerPreview(GodotObject terrainData, PlayerController player)
    {
        if (!Settings.DeerPreviewEnabled) return;
        Vector2 at = Settings.SpawnXZ + Settings.DeerPreviewOffset;
        float height = terrainData.Call("get_height", new Vector3(at.X, 0, at.Y)).AsSingle();
        if (!float.IsFinite(height))
        { GD.PushWarning("Deer preview skipped: terrain height unavailable."); return; }

        var space = player.GetWorld3D().DirectSpaceState;
        var supportRay = new PhysicsRayQueryParameters3D
        {
            From = new Vector3(at.X, height + 2f, at.Y),
            To = new Vector3(at.X, height - 1.5f, at.Y),
            CollisionMask = 1, CollideWithAreas = false
        };
        var support = space.IntersectRay(supportRay);
        if (support.Count == 0 || support["normal"].AsVector3().Y < 0.75f)
        { GD.PushWarning("Deer preview skipped: no walkable support at configured offset."); return; }

        var scene = GD.Load<PackedScene>("res://ENTITIES/TYPES/Wildlife/Deer/Deer.tscn");
        if (scene == null) { GD.PushWarning("Deer preview scene not found."); return; }
        var deer = scene.Instantiate<DeerController>();
        var wildlife = GetNode<Node3D>("WorldActorsRoot/PersistentEntitiesRoot/Wildlife");
        wildlife.AddChild(deer);
        // Godot can report a script Ready exception without throwing from AddChild. Do not cascade into target setup.
        if (!deer.IsAiReady)
        { GD.PushWarning("Deer preview skipped: initialization failed; world startup will continue."); deer.QueueFree(); return; }
        deer.GlobalPosition = support["position"].AsVector3() + Vector3.Up * 0.06f;
        var body = deer.GetNode<CollisionShape3D>("CollisionShape3D");
        var clearance = new PhysicsShapeQueryParameters3D
        {
            Shape = body.Shape, Transform = body.GlobalTransform,
            CollisionMask = 23, CollideWithBodies = true, CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { deer.GetRid() }
        };
        if (space.IntersectShape(clearance, 1).Count > 0)
        {
            GD.PushWarning("Deer preview skipped: spawn blocked by scenery or other actors.");
            deer.QueueFree(); return;
        }
        // Same explicit player target and terrain-sampler injection as zombie placement.
        deer.InitializeTarget(player, unchecked((uint)(Settings.Seed + 17011)),
            point => terrainData.Call("get_height", point).AsSingle());
    }

    // Scene-local placement only; all zombie behaviour, artwork and defaults live under ENTITIES/TYPES/Zombies.
    private void SpawnZombies(GodotObject terrainData, PlayerController player, Vector2[] hordeSlow, Vector2[] hordeFast,
        Vector2[] storeStressSlow, Vector2[] storeStressFast)
    {
        var group = GetNode<Node3D>("WorldActorsRoot/PersistentEntitiesRoot/Zombies");
        SpawnZombieSet(terrainData, player, group, _visionDebug, "res://ENTITIES/TYPES/Zombies/Zombie.tscn", Settings.ZombieSpawnOffsets, 0);
        SpawnZombieSet(terrainData, player, group, _visionDebug, "res://ENTITIES/TYPES/Zombies/Zombie_002/Zombie_002.tscn", Settings.RunnerSpawnOffsets, 104729);
        SpawnZombieSet(terrainData, player, group, _visionDebug, "res://ENTITIES/TYPES/Zombies/Zombie.tscn", hordeSlow, 209759);
        SpawnZombieSet(terrainData, player, group, _visionDebug, "res://ENTITIES/TYPES/Zombies/Zombie_002/Zombie_002.tscn", hordeFast, 314573);
        int slow = SpawnZombieSet(terrainData, player, group, _visionDebug,
            "res://ENTITIES/TYPES/Zombies/Zombie.tscn", storeStressSlow, 419431);
        int fast = SpawnZombieSet(terrainData, player, group, _visionDebug,
            "res://ENTITIES/TYPES/Zombies/Zombie_002/Zombie_002.tscn", storeStressFast, 524287);
        if (storeStressSlow.Length + storeStressFast.Length > 0)
            GD.Print($"Store stress zombies: {slow} slow + {fast} fast spawned / {storeStressSlow.Length + storeStressFast.Length} requested.");
    }

    // =========================================================
    // Both views use this bounded startup placement, ground sampler and vision debug wiring for either species.
    private int SpawnZombieSet(GodotObject terrainData, PlayerController player, Node3D group,
        ZombieVisionDebug visionDebug, string scenePath, Vector2[] offsets, int seedOffset)
    {
        if (offsets.Length == 0) return 0;
        var scene = GD.Load<PackedScene>(scenePath);
        var origin = Settings.VillageEnabled ? Settings.VillageOriginXZ : Settings.SpawnXZ;
        var space = player.GetWorld3D().DirectSpaceState;
        var support = new PhysicsRayQueryParameters3D { CollisionMask = 1, CollideWithAreas = false };
        var accepted = new System.Collections.Generic.List<Vector3>();
        int spawned = 0;
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2 offset = offsets[i];
            Vector3 position = new(origin.X + offset.X, 0, origin.Y + offset.Y);
            float height = terrainData.Call("get_height", position).AsSingle();
            if (!float.IsFinite(height)) { GD.PushWarning("Skipped zombie spawn outside preview terrain."); continue; }
            var zombie = scene.Instantiate<ZombieController>();
            group.AddChild(zombie);
            if (_village != null)
            {
                support.From = new Vector3(position.X, height + 1, position.Z);
                support.To = new Vector3(position.X, height - 0.5f, position.Z);
                var hit = space.IntersectRay(support);
                if (hit.Count == 0 || hit["normal"].AsVector3().Y < Mathf.Cos(zombie.FloorMaxAngle))
                { GD.PushWarning($"Skipped unsupported village zombie at {offset}."); zombie.QueueFree(); continue; }
                position = hit["position"].AsVector3() + Vector3.Up * 0.06f;
                Vector3 fromPlayer = position - player.GlobalPosition; fromPlayer.Y = 0;
                bool safe = fromPlayer.LengthSquared() >= 20 * 20;
                foreach (Vector3 other in accepted) if (position.DistanceSquaredTo(other) < 4) safe = false;
                foreach (Rect2 corridor in _village.Patrol)
                    if (corridor.HasPoint(new Vector2(position.X, position.Z))) safe = false;
                zombie.GlobalPosition = position;
                var shape = zombie.GetNode<CollisionShape3D>("CollisionShape3D");
                var clearance = new PhysicsShapeQueryParameters3D { Shape = shape.Shape,
                    Transform = shape.GlobalTransform, CollisionMask = zombie.CollisionMask,
                    Exclude = new Godot.Collections.Array<Rid> { zombie.GetRid() } };
                if (!safe || space.IntersectShape(clearance, 1).Count != 0)
                { GD.PushWarning($"Skipped blocked/unsafe village zombie at {offset}."); zombie.QueueFree(); continue; }
                ((ZombieSettings)zombie.RuntimeSettings).WanderRadius = Mathf.Clamp(Settings.VillageZombieWanderRadius, 4, 12);
                accepted.Add(position);
            }
            else zombie.GlobalPosition = new Vector3(position.X, height + 0.05f, position.Z);
            zombie.CorpseOwner = _corpses;
            zombie.InitializeTarget(player, unchecked((uint)(Settings.Seed + seedOffset + i * 7919)),
                point => terrainData.Call("get_height", point).AsSingle());
            visionDebug.Register(zombie);
            spawned++;
        }
        return spawned;
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
        if (IsInstanceValid(_player) && IsInstanceValid(_rocks)) _player.PositionChanged -= _rocks.UpdateAnchor;
        if (IsInstanceValid(_camera))
        {
            _camera.SettingsChanged -= UpdateWeatherCoverage;
        }
        if (IsInstanceValid(_player) && IsInstanceValid(_pines)) _player.PositionChanged -= _pines.UpdateAnchor;
        if (IsInstanceValid(_player) && IsInstanceValid(_grass)) _player.PositionChanged -= _grass.UpdateAnchor;
        GetViewport().SizeChanged -= UpdateWeatherCoverage;
    }
}
