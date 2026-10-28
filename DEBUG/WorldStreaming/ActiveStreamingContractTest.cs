// Active-world WS2 contracts: adoption, conservative camera envelopes, sleeping actor pins and canonical reload identity.
using Godot;
using System;
using System.Threading.Tasks;

public partial class ActiveStreamingContractTest : Node3D
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public async void Run()
    {
        try
        {
            var settings = new WorldStreamingSettings { UnloadDistance = 260 };
            Vector2 overhead = WorldStaticStreaming.Distances(settings, 256, 25.9f);
            Vector2 fps = WorldStaticStreaming.Distances(settings, 220, 32);
            Vector2 clearFps = WorldStaticStreaming.Distances(settings, 1800, 32);
            var tunedFpsSettings = new WorldStreamingSettings { LoadDistance = 180, UnloadDistance = 244 };
            Vector2 tunedFps = WorldStaticStreaming.Distances(tunedFpsSettings, 235, 4);
            Check(tunedFps == new Vector2(272, 336), "FPS tuned preload and unload with 115m fog edge");
            Check(tunedFps.X > 235 + 4 && tunedFps.Y - tunedFps.X == 64, "clip safety and hysteresis retained");
            Check(overhead == new Vector2(320, 400), "overhead full-zoom/clip envelope");
            Check(fps.X >= 220 + 32 + 32 && clearFps.X > 1800, "FPS and fog-off far clip covered");
            var manifest = WorldManifest.Create(new WorldSectionSettings(), 1);
            var root = new Node3D { Name = "Structures" }; AddChild(root);
            var original = new Node3D { Name = "OriginalCar", Position = new Vector3(128, 0, 128) }; root.AddChild(original);
            var record = WorldPlacementRecord.Capture("adopt.car.001", "object.car.001", original,
                new Rect2(125, 125, 6, 6), manifest, 1);
            manifest.Placements.Add(record);
            var stream = new StaticSectionStreamer(); AddChild(stream); stream.Initialize(manifest, settings, root);
            stream.Adopt(record.InstanceId, original);
            Check(stream.ResidentCount == 1 && root.GetChildCount() == 1 && stream.IsCollisionReady(record.InstanceId), "adopt without duplication or rebuild");
            stream.CanUnload = _ => false;
            stream.UpdateAnchor(new Vector3(700, 0, 700), true); await Settle(stream);
            Check(stream.PinnedCount == 1 && stream.UnloadCount == 0 && root.GetChildCount() == 1, "actor pin preserves scene and sleeps");
            stream.CanUnload = _ => true;
            stream.UpdateAnchor(new Vector3(700, 0, 700), true); await Settle(stream);
            Check(stream.ResidentCount == 0 && root.GetChildCount() == 0, "released pin permits teardown");
            stream.UpdateAnchor(new Vector3(128, 0, 128), true); await Settle(stream);
            var restored = root.GetNode<Node3D>("OriginalCar");
            Check(root.GetChildCount() == 1 && restored.GlobalTransform == record.WorldTransform
                && restored.GetMeta("world_placement_id").AsString() == record.InstanceId, "reload preserves name identity and transform");
            Check(restored.GetNodeOrNull<Node3D>("Generated") != null, "canonical procedural Ready completed");
            stream.SetDistances(512, 600, new Vector3(400, 0, 400)); await Settle(stream);
            Check(stream.ResidentCount == 1 && stream.LoadCount == 2, "envelope expansion does not duplicate existing scene");
            GD.Print($"[WS2 integration] PASS: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[WS2 integration] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private async Task Settle(StaticSectionStreamer stream)
    {
        ulong deadline = Time.GetTicksMsec() + 15000;
        do
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Time.GetTicksMsec() > deadline) throw new TimeoutException("Streamer failed to settle.");
        } while (stream.Working);
    }

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
