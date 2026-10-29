// IX0 native-physics smoke: projections, reach, faded solid cover, floors and target-owned actions.
using Godot;
using System;

public partial class InteractionTargetingTest : Node3D
{
    private sealed class CenterView : IInteractionView
    {
        public bool CanFocus => true;
        public bool UseFadedCameraSelection { get; set; }
        // =========================================================
        public Vector2 AimPosition(Viewport viewport) => viewport.GetVisibleRect().Size * 0.5f;
    }
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public async void Run()
    {
        try
        {
            var camera = new Camera3D { Current = true }; AddChild(camera);
            var player = GD.Load<PackedScene>("res://PLAYER/Player.tscn").Instantiate<PlayerController>();
            AddChild(player); player.Initialize(new PlayerSettings(), camera); player.SetPhysicsProcess(false);
            var target = GD.Load<PackedScene>("res://DEBUG/Interaction/InteractionTestSwitch.tscn").Instantiate<InteractionTestSwitch>();
            AddChild(target); target.Position = new Vector3(0, 0, -2);
            var view = new CenterView(); var settings = new InteractionSettings();
            var targeting = new InteractionTargeting(player, camera, view, settings);
            camera.Position = new Vector3(0, 1.5f, 0); camera.LookAt(new Vector3(0, 0.9f, -2));
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(targeting.TrySelect(out var selected, out var context, out var action), "FPS centre ray selects nearby target");
            Check(ReferenceEquals(selected, target) && selected!.TryInteract(context, action.Kind), "shared action dispatch");
            selected!.TryGetAction(context, out var changed);
            Check(changed.Label.Contains("OFF"), "target owns toggle state");
            camera.Projection = Camera3D.ProjectionType.Orthogonal; camera.Size = 8;
            camera.Position = new Vector3(0, 8, 5); camera.LookAt(new Vector3(0, 0.9f, -2));
            view.UseFadedCameraSelection = true;
            Check(targeting.TrySelect(out _, out _, out _), "overhead projection selects same object");
            player.Position = new Vector3(8, 0, 0);
            Check(!targeting.TrySelect(out _, out _, out _), "camera visibility cannot bypass player reach");
            player.Position = Vector3.Zero;
            var wall = new StaticBody3D { Position = new Vector3(0, 1.5f, -1), CollisionLayer = 1 }; AddChild(wall);
            var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(3, 3, 0.12f) } }; wall.AddChild(shape);
            var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(3, 3, 0.12f) } };
            wall.AddChild(mesh); FadeAwareAim.Link(shape, mesh); mesh.SetMeta(FadeAwareAim.FadedMeta, true);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(!targeting.TrySelect(out _, out _, out _), "solid reach rejects visually faded cover");
            wall.Free(); target.Position = new Vector3(0, 3, -1);
            camera.Position = new Vector3(0, 8, 3); camera.LookAt(new Vector3(0, 3.9f, -1));
            var floor = new StaticBody3D { Position = new Vector3(0, 2, 0), CollisionLayer = 1 }; AddChild(floor);
            floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(8, 0.15f, 8) } });
            settings.ReachMeters = 4; targeting = new InteractionTargeting(player, camera, view, settings);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(!targeting.TrySelect(out _, out _, out _), "different-floor cover blocks interaction");
            GD.Print($"[IX0] PASS: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[IX0] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private void Check(bool passed, string message)
    { _checks++; if (!passed) throw new InvalidOperationException(message); }
}
