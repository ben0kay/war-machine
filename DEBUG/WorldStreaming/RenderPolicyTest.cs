// WS1 smoke tests use real native ranges and fog settings. No source asset/world/terrain mutations.
using Godot;
using System;

public partial class RenderPolicyTest : Node
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public void Run()
    {
        try
        {
            var bounds = new Aabb(new Vector3(-8, 0, -8), new Vector3(16, 40, 16));
            Vector3 eye = new(0, 35, 35), playerAt = Vector3.Zero;
            float end = WorldRenderEnvelope.End(bounds, eye, playerAt, 50, false, 12);
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = bounds.GetEndpoint(i);
                if (new Vector2(point.X, point.Z).Length() <= 50)
                    Check(eye.DistanceTo(bounds.GetCenter()) <= end, "tall visible bounds retained");
            }
            Check(WorldRenderEnvelope.Cap(30, 100) == 30, "short authored cap retained");
            Check(WorldRenderEnvelope.Cap(0, 100) == 100, "unlimited authored range capped");
            Check(WorldRenderEnvelope.End(bounds, eye, playerAt, 50, true, 12) >= 50 + bounds.Size.Length() * 0.5f, "FPS sphere envelope");
            var player = GD.Load<PackedScene>("res://PLAYER/Player.tscn").Instantiate<PlayerController>(); AddChild(player);
            var camera = new Camera(); AddChild(camera);
            player.Initialize(new PlayerSettings(), camera); camera.Initialize(player, new CameraSettings());
            var props = new Node3D(); AddChild(props);
            var mesh = new MeshInstance3D { Mesh = new BoxMesh(), VisibilityRangeEnd = 0 }; props.AddChild(mesh);
            var lod = new MeshInstance3D { Mesh = new BoxMesh(), VisibilityRangeBegin = 20, VisibilityRangeEnd = 90 }; props.AddChild(lod);
            lod.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, 90f);
            var lamp = new OmniLight3D { OmniRange = 10, Visible = false }; props.AddChild(lamp);
            var exempt = new MeshInstance3D { Mesh = new BoxMesh() }; exempt.SetMeta(WorldRenderEnvelope.ExemptMetadata, true); props.AddChild(exempt);
            var environment = new EnvironmentConfig { DistanceFogEnabled = true, DistanceFogStart = 24,
                DistanceFogEnd = 48, DistanceFogOpacity = 1, DistanceFogEdgeVariation = 2 };
            var fog = new PlayerDistanceFog(); AddChild(fog); fog.Initialize(camera, player, environment);
            Check(fog.FullyOpaque && fog.OpaqueRadius == 50, "effective fog edge includes variation");
            var policy = new WorldRenderCulling(); AddChild(policy);
            policy.Initialize(fog, camera, player, new WorldRenderSettings(), props); policy.Flush();
            Check(policy.RegisteredCount == 3 && mesh.VisibilityRangeEnd > 0, "only eligible native objects registered/capped");
            Check(lod.VisibilityRangeBegin == 20 && lod.VisibilityRangeEnd <= 90, "LOD start and authored ceiling preserved");
            Check(lamp.DistanceFadeEnabled && !lamp.Visible, "native light fade without enabling fixture");
            Check(exempt.VisibilityRangeEnd == 0, "exempt owner preserved");
            lod.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, 75f); lod.VisibilityRangeEnd = 75;
            policy.Refresh(); policy.Flush();
            environment.DistanceFogOpacity = 0.5f; fog.ApplySettings(); policy.Flush();
            Check(mesh.VisibilityRangeEnd == 0 && lod.VisibilityRangeEnd == 75 && !lamp.DistanceFadeEnabled, "translucent fog restores latest authored ranges");
            environment.DistanceFogOpacity = 1; fog.ApplySettings(); policy.Flush();
            Check(mesh.VisibilityRangeEnd > 0, "opaque fog reactivates");
            policy.Settings.Enabled = false; policy.ApplySettings(); policy.Flush();
            Check(mesh.VisibilityRangeEnd == 0 && !lamp.DistanceFadeEnabled, "policy OFF restoration");
            policy.Settings.Enabled = true; policy.ApplySettings(); policy.Flush();
            props.RemoveChild(mesh); Check(mesh.VisibilityRangeEnd == 0, "scope removal restores geometry"); mesh.Free();
            var fpsEnvironment = new EnvironmentConfig { DistanceFogEnabled = true, DistanceFogOpacity = 1, FpsSkyFogOpacity = 0.08f };
            var fpsFog = new PlayerDistanceFog(); AddChild(fpsFog); fpsFog.Initialize(camera, player, fpsEnvironment, cameraDistance: true);
            Check(fpsFog.FullyOpaque && !fpsFog.OpaqueBackdrop, "open FPS sky cannot authorize extra geometry cutoff");
            fpsEnvironment.FpsSkyFogOpacity = 1; fpsEnvironment.SetDaylight(0.5f);
            Check(fpsFog.OpaqueBackdrop, "opaque sky restores geometry-cull eligibility");
            GD.Print($"[WS1] PASS: {_checks} checks. Actual world visuals/profiling remain manual."); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[WS1] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
