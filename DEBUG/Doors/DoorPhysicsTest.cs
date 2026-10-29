// DOOR1 integration regression: actual capsule MoveAndSlide contacts drive the production push adapter, both sides and rotated parents.
using Godot;
using System;

public partial class DoorPhysicsTest : Node3D
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public async void Run()
    {
        try
        {
            // Response curve must distinguish brushing/nudging, walking and running.
            // Tested independently of collision frames so a tuning regression fails deterministically.
            var tuned = GD.Load<HingedDoorSettings>(
                "res://WORLD/Content/Assets/Objects/Static/Doors/Door001/Configuration/DoorMaster.tres");
            Check(tuned != null, "shared DoorMaster settings load");
            float nudge = HingedDoor001.ContactSpeedFactor(0.8f, tuned!);
            float walk = HingedDoor001.ContactSpeedFactor(4.0f, tuned!);
            float sprint = HingedDoor001.ContactSpeedFactor(7.0f, tuned!);
            Check(nudge > 0f && nudge < walk * 0.2f, "gentle contact transfers much less impulse than walking");
            Check(sprint > walk * 1.5f, "sprint contact transfers much more impulse than walking");
            Check(HingedDoor001.ContactSpeedFactor(0f, tuned!) == 0f, "no push at zero movement speed");
            Check(Mathf.IsEqualApprox(tuned!.MassKg, 5f), "shared rear door has owner-requested 5kg mass");
            Check(tuned.AngularDamping <= 0.40f, "hinge damping permits continued motion after release");
            Check(Mathf.IsEqualApprox(tuned.ContactSpeedReference, 3.5f), "walking contact reference is tuned lower");
            Check(tuned.AngularDamping < 1f, "light door retains momentum after contact ends");
            foreach (float yaw in new[] { 0f, 45f }) foreach (float side in new[] { -1f, 1f })
            {
                var fixture = new Node3D { Position = new Vector3(8, 0, 4), RotationDegrees = new Vector3(0, yaw, 0) }; AddChild(fixture);
                var walls = new StaticBody3D { CollisionLayer = 1, CollisionMask = 1 }; fixture.AddChild(walls);
                // Match the store: floor top .12, 1.52m opening, .23m wall thickness, header at 2.90m.
                Box(walls, new Vector3(0, -0.38f, 0), new Vector3(8, 1, 8));
                Box(walls, new Vector3(-1.88f, 1.9f, 0), new Vector3(2.24f, 3.8f, 0.23f));
                Box(walls, new Vector3(1.88f, 1.9f, 0), new Vector3(2.24f, 3.8f, 0.23f));
                Box(walls, new Vector3(0, 3.35f, 0), new Vector3(1.52f, 0.9f, 0.23f));
                var door = GD.Load<PackedScene>("res://WORLD/Content/Assets/Objects/Static/Doors/Door001/Door001.tscn").Instantiate<HingedDoor001>();
                door.Position = new Vector3(-0.69f, 0, 0); fixture.AddChild(door);
                var leaf = door.GetNode<RigidBody3D>("Leaf");
                var joint = door.GetNode<HingeJoint3D>("HingeJoint3D");
                Check(joint.GetNode<Node3D>(joint.NodeA) == door.GetNode("HingeAnchor")
                    && joint.GetNode<Node3D>(joint.NodeB) == leaf, "joint paths resolve to both bodies");
                Check(joint.GlobalBasis.Z.Normalized().Dot(Vector3.Up) > 0.9999f, "joint Z is vertical");
                Check(!leaf.AxisLockAngularX && !leaf.AxisLockAngularY && !leaf.AxisLockAngularZ, "no conflicting rigid angular locks");
                for (int i = 0; i < 60; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Check(Mathf.Abs(Yaw(leaf)) < Mathf.DegToRad(0.5f), "starts closed without spontaneous swing");
                CheckAlignment(door, leaf, joint);
                var shape = leaf.GetNode<CollisionShape3D>("DoorCollision");
                var overlap = new PhysicsShapeQueryParameters3D { Shape = shape.Shape, Transform = shape.GlobalTransform,
                    CollisionMask = 1, Exclude = new Godot.Collections.Array<Rid> { leaf.GetRid() } };
                Check(GetWorld3D().DirectSpaceState.IntersectShape(overlap, 1).Count == 0, "closed leaf clears floor jambs and header");
                var panel = leaf.GetNode<MeshInstance3D>("Panel");
                Check(shape.GetMeta(FadeAwareAim.VisualMeta).AsGodotObject() == panel
                    && panel.GetMeta(CameraOccluder.DynamicFadeVisualMeta).AsBool(), "AIM1 link and dynamic wall-fade metadata retained");
                var ray = PhysicsRayQueryParameters3D.Create(fixture.ToGlobal(new Vector3(0, 1.2f, -1)), fixture.ToGlobal(new Vector3(0, 1.2f, 1)), 1);
                panel.SetMeta(FadeAwareAim.FadedMeta, true);
                var bullet = GetWorld3D().DirectSpaceState.IntersectRay(ray);
                Check(bullet.Count > 0 && bullet["collider"].AsGodotObject() == leaf, "faded door remains solid for bullet query");
                Check(!FadeAwareAim.TryHit(GetWorld3D().DirectSpaceState, ray, out _), "cursor AIM1 can pass the faded leaf");
                panel.SetMeta(FadeAwareAim.FadedMeta, false);
                var mover = new CharacterBody3D { CollisionLayer = 2, CollisionMask = 21, SafeMargin = 0.01f,
                    FloorSnapLength = 0.5f, Position = new Vector3(0.50f, 0.13f, side * 0.9f) };
                mover.AddChild(new CollisionShape3D { Position = Vector3.Up * 0.9f, Shape = new CapsuleShape3D { Radius = 0.3f, Height = 1.8f } });
                fixture.AddChild(mover);
                Vector3 horizontal = fixture.GlobalBasis * new Vector3(0, 0, -side * 3);
                bool contactedLeaf = false, handled = false;
                for (int i = 0; i < 180 && Mathf.Abs(Yaw(leaf)) < Mathf.DegToRad(15); i++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                    mover.Velocity = horizontal + Vector3.Down * 0.5f;
                    mover.MoveAndSlide();
                    for (int j = 0; j < mover.GetSlideCollisionCount(); j++)
                        if (mover.GetSlideCollision(j).GetCollider() == leaf) contactedLeaf = true;
                    handled |= CharacterContactPush.TryApply(mover, horizontal, (float)GetPhysicsProcessDeltaTime());
                    CheckAlignment(door, leaf, joint);
                }
                Check(contactedLeaf && handled && door.CharacterPushCount > 0, "real slide leaf contact reaches TryPushFromCharacter");
                Check(side * Yaw(leaf) > Mathf.DegToRad(5), "physical push opens from the expected side");
                Check(Mathf.Abs(door.LastHingeTorqueImpulse) > 0.0001f, "handle-side contact creates vertical hinge torque");
                mover.Free();
                for (int i = 0; i < 240; i++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                    CheckAlignment(door, leaf, joint);
                    Check(Mathf.Abs(Yaw(leaf)) <= Mathf.DegToRad(door.Settings.SwingLimitDegrees + 5), "swing stays within configured limits");
                }
                Check(leaf.AngularVelocity.Length() < 0.25f, "angular damping slows the unforced leaf");
                Check(!door.IsProcessing() && !door.IsPhysicsProcessing(), "no per-door gameplay callbacks");
                GD.Print($"[DOOR1] yaw={yaw}, side={side}: {door.GetContactPushDiagnostics()}");
                fixture.Free();
            }
            GD.Print($"[DOOR1] PASS: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[DOOR1] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private void CheckAlignment(HingedDoor001 door, RigidBody3D leaf, HingeJoint3D joint)
    {
        Check(leaf.GlobalBasis.Y.Normalized().Dot(Vector3.Up) > 0.9995f, "leaf stays upright");
        Vector3 edge = leaf.ToGlobal(new Vector3(-door.Settings.Width * 0.5f, 0, 0));
        Check(edge.DistanceTo(joint.GlobalPosition) < 0.025f, "hinge edge remains attached to anchor");
    }

    // =========================================================
    private static float Yaw(Node3D node) => Mathf.Atan2(-node.Basis.X.Z, node.Basis.X.X);

    // =========================================================
    private static void Box(StaticBody3D body, Vector3 center, Vector3 size)
        => body.AddChild(new CollisionShape3D { Position = center, Shape = new BoxShape3D { Size = size } });

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
