// WS2 standalone finite strip: walk between static procedural/GLB placements. Does not touch World.tscn or saved terrain.
using Godot;
using System;
using System.Threading.Tasks;

public partial class StaticStreamingProof : Node3D
{
    [Export] public WorldStreamingSettings Settings { get; set; } = null!;
    private StaticSectionStreamer _streamer = null!;
    private WorldManifest _manifest = null!;
    private StreamingProofWalker _walker = null!;
    private Camera3D _camera = null!;
    private Label _label = null!;
    private int _checks;

    // =========================================================
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        Settings = (WorldStreamingSettings)Settings.Duplicate(true);
        var environment = new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0.19f, 0.23f, 0.22f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.7f } };
        AddChild(environment);
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-60, -25, 0), LightEnergy = 1.1f });
        var floor = new StaticBody3D { CollisionLayer = 1, CollisionMask = 1 }; AddChild(floor);
        floor.AddChild(new MeshInstance3D { Position = new Vector3(384, -0.5f, 0), Mesh = new BoxMesh { Size = new Vector3(1600, 1, 512) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.20f, 0.25f, 0.20f) } });
        floor.AddChild(new CollisionShape3D { Position = new Vector3(384, -0.5f, 0), Shape = new BoxShape3D { Size = new Vector3(1600, 1, 512) } });
        _walker = new StreamingProofWalker { Position = new Vector3(32, 0.05f, 0), CollisionLayer = 2, CollisionMask = 1 };
        _walker.AddChild(new CollisionShape3D { Position = Vector3.Up, Shape = new CapsuleShape3D { Radius = 0.35f, Height = 2 } });
        _walker.AddChild(new MeshInstance3D { Position = Vector3.Up, Mesh = new CapsuleMesh { Radius = 0.35f, Height = 2 } });
        AddChild(_walker);
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 44, Far = 120, Current = true,
            RotationDegrees = new Vector3(-55, 0, 0) }; AddChild(_camera);
        var layer = new CanvasLayer(); AddChild(layer);
        _label = new Label { HorizontalAlignment = HorizontalAlignment.Center }; layer.AddChild(_label);
        _label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _label.OffsetTop = 12; _label.OffsetBottom = 100;
        _label.AddThemeColorOverride("font_shadow_color", Colors.Black); _label.AddThemeConstantOverride("shadow_outline_size", 5);
        _manifest = BuildManifest();
        var root = new Node3D { Name = "StreamedStaticRoot" }; AddChild(root);
        _streamer = new StaticSectionStreamer { Name = "StaticSections" }; AddChild(_streamer);
        _streamer.Initialize(_manifest, Settings, root); _streamer.Changed += RefreshLabel;
        _walker.Moved += OnMoved; _walker.CanEnter = CanEnter;
        _streamer.UpdateAnchor(_walker.GlobalPosition, true); OnMoved(_walker.GlobalPosition);
        foreach (string arg in OS.GetCmdlineUserArgs()) if (arg == "--ws2-test") { CallDeferred(nameof(RunChecks)); break; }
    }

    // =========================================================
    private WorldManifest BuildManifest()
    {
        var manifest = WorldManifest.Create(new WorldSectionSettings { WorldId = "ws2-static-proof",
            LogicalBounds = new Rect2(-256, -256, 1280, 512) }, 48193);
        // Cars straddle X section boundaries; owner is still one ID. GLBs exercise the same loader.
        for (int i = 1; i <= 5; i++)
        {
            float x = i * 128;
            AddPlacement(manifest, $"proof.car.{i}", "object.car.001", new Vector3(x, 0, 0), new Rect2(x - 3, -3, 6, 6));
            AddPlacement(manifest, $"proof.hedge.{i}", "object.hedge.001", new Vector3(x, 0, -10), new Rect2(x - 3, -12, 6, 4));
            var marker = new Label3D { Text = $"X={x} · Section ({i}, 0)\nC# car + GLB hedge", Position = new Vector3(x, 5, -6),
                PixelSize = 0.02f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
            AddChild(marker); // Persistent diagnostic only, outside the streamed content.
        }
        manifest.Validate(); return manifest;
    }

    // =========================================================
    private static void AddPlacement(WorldManifest manifest, string id, string asset, Vector3 at, Rect2 bounds)
    {
        // Construct authored test records without creating temporary live instances.
        manifest.Placements.Add(new WorldPlacementRecord { InstanceId = id, AssetId = asset,
            Owner = manifest.SectionAt(new Vector2(at.X, at.Z)), Seed = manifest.Seed,
            Transform = new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, at.X, at.Y, at.Z },
            Footprint = new[] { bounds.Position.X, bounds.Position.Y, bounds.Size.X, bounds.Size.Y } });
    }

    // =========================================================
    private void OnMoved(Vector3 position)
    {
        _streamer.UpdateAnchor(position);
        _camera.GlobalPosition = position + new Vector3(0, 32, 22.4f);
        RefreshLabel();
    }

    // =========================================================
    // Test harness gates entry into unready footprints. This is not a general active-world collision barrier.
    private bool CanEnter(Vector3 position)
    {
        foreach (var item in _manifest.Placements)
            if (StaticSectionStreamer.DistanceToBounds(new Vector2(position.X, position.Z), item.Bounds) < 6
                && !_streamer.IsCollisionReady(item.InstanceId)) return false;
        return true;
    }

    // =========================================================
    private void RefreshLabel()
    {
        SectionCoordinate cell = _manifest.SectionAt(new Vector2(_walker.Position.X, _walker.Position.Z));
        _label.Text = $"WS2 proof · Section ({cell.X}, {cell.Z}) · X {_walker.Position.X:0}\n"
            + $"Resident {_streamer.ResidentCount} / desired {_streamer.DesiredCount} · requests {_streamer.InFlightCount} · "
            + $"loads {_streamer.LoadCount} / unloads {_streamer.UnloadCount} · {(_streamer.Working ? "working" : "asleep")}\n"
            + "WASD move · Shift faster · D east / A return · Separate test, no persistence";
    }

    // =========================================================
    public async void RunChecks()
    {
        try
        {
            await Settled(); Check(_streamer.IsResident("proof.car.1"), "near car loads");
            Check(_streamer.IsResident("proof.hedge.1"), "GLB loads through same lifecycle");
            Check(!_streamer.IsResident("proof.car.5"), "distant car absent");
            Check(_manifest.CoveredSections(_manifest.Placements[0].Bounds).Count == 4, "boundary footprint dependencies");
            int loads = _streamer.LoadCount;
            _streamer.UpdateAnchor(new Vector3(127, 0, 0), true); await Settled();
            _streamer.UpdateAnchor(new Vector3(129, 0, 0), true); await Settled();
            Check(_streamer.LoadCount == loads + 2, "crossing section edge loads next pair once");
            _streamer.UpdateAnchor(new Vector3(-80, 0, 0), true); await Settled();
            Check(_streamer.IsResident("proof.car.1"), "hysteresis retains outside load distance");
            _streamer.UpdateAnchor(new Vector3(-200, 0, 0), true); await Settled();
            Check(_streamer.ResidentCount == 0, "far content fully unloaded");
            _streamer.UpdateAnchor(new Vector3(640, 0, 0), true);
            _streamer._Process(0); // Begin requests, then supersede them before completion/attachment.
            _streamer.UpdateAnchor(new Vector3(-200, 0, 0), true); await Settled();
            Check(_streamer.ResidentCount == 0 && _streamer.InFlightCount == 0, "stale jobs drained without obsolete scenes");
            _streamer.UpdateAnchor(new Vector3(128, 0, 0), true); await Settled();
            Check(_streamer.IsResident("proof.car.1"), "return restores stable identity");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            var ray = PhysicsRayQueryParameters3D.Create(new Vector3(128, 4, 0), new Vector3(128, 0.1f, 0), 1);
            Check(GetWorld3D().DirectSpaceState.IntersectRay(ray).Count > 0, "returned car collision is registered");
            Check(_streamer.IsCollisionReady("proof.car.1"), "collision readiness delay elapsed");
            GD.Print($"[WS2] PASS: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[WS2] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private async Task Settled()
    {
        ulong deadline = Time.GetTicksMsec() + 15000;
        do
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CheckLimits();
            if (Time.GetTicksMsec() > deadline) throw new TimeoutException("WS2 did not settle in 15 seconds.");
        } while (_streamer.Working);
    }

    // =========================================================
    private void CheckLimits()
    {
        if (_streamer.ResidentCount > Settings.MaxResident || _streamer.InFlightCount > Settings.MaxInFlight)
            throw new InvalidOperationException("Streaming caps exceeded.");
    }

    // =========================================================
    private void Check(bool value, string message)
    { _checks++; if (!value) throw new InvalidOperationException(message); }
}
