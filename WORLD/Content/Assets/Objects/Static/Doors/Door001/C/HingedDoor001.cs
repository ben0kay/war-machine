// DOOR1: reusable hinged service door with speed-sensitive player contact and natural momentum; no per-frame gameplay callbacks.
// A rigid leaf swings on Godot's HingeJoint3D; its door handle anchors support future DOOR2 pulling.
using Godot;

public partial class HingedDoor001 : Node3D, ICharacterContactPushable
{
    [Export] public HingedDoorSettings Settings { get; set; } = null!;
    private RigidBody3D? _leaf;
    private HingeJoint3D? _joint;
    public int CharacterPushCount { get; private set; }
    public Vector3 LastPushPoint { get; private set; }
    public float LastHingeTorqueImpulse { get; private set; }

    // =========================================================
    // Build one rigid leaf, fixed hinge anchor and physical joint when the prefab enters the world.
    // The parent's transform must be static; its local origin is the vertical hinge line at the floor.
    public override void _Ready()
    {
        if (GetNodeOrNull<RigidBody3D>("Leaf") != null) return;
        Settings = (HingedDoorSettings)(Settings ?? new HingedDoorSettings()).Duplicate(true);
        var s = Settings;
        float width = Mathf.Clamp(s.Width, 0.65f, 2f);
        float height = Mathf.Clamp(s.Height, 1.85f, 2.85f);
        float thickness = Mathf.Clamp(s.Thickness, 0.035f, 0.16f);
        float bottom = Mathf.Clamp(s.BottomClearance, 0.04f, 0.30f);
        float middle = bottom + height * 0.5f;

        // The anchor intentionally has no collision shape: the building wall supplies
        // its real jamb collision. This body simply fixes the joint in world space.
        var anchor = new StaticBody3D { Name = "HingeAnchor", CollisionLayer = 0, CollisionMask = 0 };
        AddChild(anchor);

        // Leaf origin is at its centre of mass; the hinge remains on the left edge.
        var leaf = new RigidBody3D
        {
            Name = "Leaf", Position = new Vector3(width * 0.5f, middle, 0),
            CollisionLayer = 1, CollisionMask = 1,
            Mass = Mathf.Clamp(s.MassKg, 1f, 50f),
            AngularDampMode = RigidBody3D.DampMode.Replace,
            AngularDamp = Mathf.Clamp(s.AngularDamping, 0f, 12f),
            CenterOfMassMode = RigidBody3D.CenterOfMassModeEnum.Custom, CenterOfMass = Vector3.Zero,
            CanSleep = true, ContinuousCd = true
        };
        AddChild(leaf);
        _leaf = leaf;
        var panel = new MeshInstance3D
        {
            Name = "Panel", Mesh = new BoxMesh { Size = new Vector3(width, height, thickness) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = s.PanelColor, Roughness = 0.82f, Metallic = 0.24f
            }
        };
        // Moving visuals are included in the whole-wall opacity group, but never cached as
        // an unmoving camera blocker at their closed position.
        panel.SetMeta(CameraOccluder.DynamicFadeVisualMeta, true);
        panel.SetMeta("structure_camera_blocker", false);
        leaf.AddChild(panel);
        var shape = new CollisionShape3D
        {
            Name = "DoorCollision",
            Shape = new BoxShape3D { Size = new Vector3(width, height, thickness),
                Margin = Mathf.Clamp(s.CollisionMargin, 0.001f, Mathf.Min(0.02f, thickness * 0.2f)) }
        };
        FadeAwareAim.Link(shape, panel);
        leaf.AddChild(shape);

        // Physical handle detail is artwork only (the whole slab receives the collision).
        // Both passive anchors move with the door and can become DOOR2 pull targets.
        float handleX = width * 0.40f, handleY = Mathf.Clamp(s.HandleHeight, 0.65f, 1.55f) - middle;
        foreach (float side in new[] { -1f, 1f })
        {
            var handle = new Marker3D { Name = side < 0 ? "HandleOutside" : "HandleInside",
                Position = new Vector3(handleX, handleY, side * (thickness * 0.5f + 0.09f)) };
            leaf.AddChild(handle);
            var plate = new MeshInstance3D
            {
                Name = side < 0 ? "HandlePlateOutside" : "HandlePlateInside",
                Position = new Vector3(handleX, handleY, side * (thickness * 0.5f + 0.014f)),
                Mesh = new BoxMesh { Size = new Vector3(0.11f, 0.20f, 0.025f) },
                MaterialOverride = new StandardMaterial3D
                { AlbedoColor = s.HandleColor, Roughness = 0.36f, Metallic = 0.72f }
            };
            plate.SetMeta(CameraOccluder.DynamicFadeVisualMeta, true);
            plate.SetMeta("structure_camera_blocker", false);
            leaf.AddChild(plate);
        }

        // Godot/Jolt use the JOINT'S local Z axis, not Y, as the hinge axis.
        // Rotate only the joint frame so +Z maps to door-local +Y; the closed leaf stays identity/upright.
        // Do not add rigid-body angular axis locks: the hinge owns all other degrees of freedom.
        var joint = new HingeJoint3D
        {
            Name = "HingeJoint3D", Position = new Vector3(0, middle, 0),
            Rotation = new Vector3(-Mathf.Pi * 0.5f, 0, 0),
            NodeA = new NodePath("../HingeAnchor"),
            NodeB = new NodePath("../Leaf"),
            ExcludeNodesFromCollision = true
        };
        joint.SetFlag(HingeJoint3D.Flag.UseLimit, true);
        float limit = Mathf.DegToRad(Mathf.Clamp(s.SwingLimitDegrees, 25f, 120f));
        joint.SetParam(HingeJoint3D.Param.LimitLower, -limit);
        joint.SetParam(HingeJoint3D.Param.LimitUpper, limit);
        joint.SetFlag(HingeJoint3D.Flag.EnableMotor, false);
        AddChild(joint); _joint = joint;
        if (joint.GetNodeOrNull<StaticBody3D>(joint.NodeA) != anchor || joint.GetNodeOrNull<RigidBody3D>(joint.NodeB) != leaf)
            GD.PushError($"{GetPath()}: hinge body paths did not resolve to HingeAnchor/Leaf.");
        SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    // The CharacterBody3D's slide contacts give us a one-time force at the point of contact.
    // The hinge solves the door angle/limits; we never directly set its rotation or move it.
    // A push near the handle creates natural torque; pushing on the hinge creates very little.
    public bool TryPushFromCharacter(Vector3 direction, Vector3 point, float speed, float delta)
    {
        if (!IsInstanceValid(_leaf) || !IsInstanceValid(_joint) || _leaf!.Freeze || !direction.IsFinite()
            || !point.IsFinite() || !float.IsFinite(speed) || !float.IsFinite(delta) || speed <= 0.1f || delta <= 0) return false;
        direction.Y = 0;
        if (direction.LengthSquared() < 0.0001f) return false;
        var s = Settings ?? new HingedDoorSettings();
        float force = Mathf.Clamp(s.CharacterContactForce, 0f, 150f);
        if (force <= 0f) return false;
        // Walking pushes comfortably; a soft nudge is weak, while sprinting transfers more momentum.
        // Keep the normal contact force × dt budget, one eligible slide contact per physics step.
        float speedFactor = ContactSpeedFactor(speed, s);
        Vector3 impulse = direction.Normalized() * (force * speedFactor * Mathf.Clamp(delta, 0f, 0.05f));
        _leaf.Sleeping = false;
        _leaf.ApplyImpulse(impulse, point - _leaf.GlobalPosition);
        LastPushPoint = point;
        LastHingeTorqueImpulse = (point - _joint!.GlobalPosition).Cross(impulse).Dot(_joint.GlobalBasis.Z.Normalized());
        CharacterPushCount++;
        return true;
    }

    // =========================================================
    // Compute contact transfer from actual horizontal movement speed. No minimum impulse floor:
    // barely brushing the door must not hit it as hard as walking or sprinting.
    // Exposed internally for deterministic isolated checks; configuration remains in DoorMaster.tres.
    internal static float ContactSpeedFactor(float speed, HingedDoorSettings s)
    {
        if (!float.IsFinite(speed) || speed <= 0f) return 0f;
        float reference = Mathf.Clamp(s.ContactSpeedReference, 1f, 10f);
        float exponent = Mathf.Clamp(s.ContactSpeedExponent, 1f, 3f);
        float maximum = Mathf.Clamp(s.MaxContactSpeedMultiplier, 1f, 4f);
        return Mathf.Clamp(Mathf.Pow(speed / reference, exponent), 0f, maximum);
    }

    // =========================================================
    // F10 reads this only after the player's real MoveAndSlide reports the leaf. No idle diagnostics or extra queries.
    public string GetContactPushDiagnostics()
    {
        if (!IsInstanceValid(_leaf) || !IsInstanceValid(_joint)) return "DOOR1: missing leaf/joint.";
        return $"DOOR1 {GetPath()}: pushes={CharacterPushCount}; hinge-axis={_joint!.GlobalBasis.Z}; "
            + $"last-point={LastPushPoint}; hinge-torque-impulse={LastHingeTorqueImpulse:0.0000}; "
            + $"yaw={_leaf!.RotationDegrees.Y:0.00}; up-dot={_leaf.GlobalBasis.Y.Normalized().Dot(GlobalBasis.Y.Normalized()):0.0000}; "
            + $"sleeping={_leaf.Sleeping}; A={_joint.GetNodeOrNull<Node3D>(_joint.NodeA)?.GetPath()}; "
            + $"B={_joint.GetNodeOrNull<Node3D>(_joint.NodeB)?.GetPath()}";
    }

}
