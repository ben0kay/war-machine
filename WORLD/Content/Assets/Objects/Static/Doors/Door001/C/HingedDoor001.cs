// DOOR1: one reusable hinged service door, no player input or per-frame gameplay callbacks.
// A rigid leaf swings on Godot's HingeJoint3D; its door handle anchors support future DOOR2 pulling.
using Godot;

public partial class HingedDoor001 : Node3D
{
    [Export] public HingedDoorSettings Settings { get; set; } = null!;

    // =========================================================
    // Build one rigid leaf, fixed hinge anchor and physical joint when the prefab enters the world.
    // The parent's transform must be static; its local origin is the vertical hinge line at the floor.
    public override void _Ready()
    {
        if (GetNodeOrNull<RigidBody3D>("Leaf") != null) return;
        var s = Settings ?? new HingedDoorSettings();
        float width = Mathf.Clamp(s.Width, 0.65f, 2f);
        float height = Mathf.Clamp(s.Height, 1.85f, 2.85f);
        float thickness = Mathf.Clamp(s.Thickness, 0.035f, 0.16f);
        float bottom = Mathf.Clamp(s.BottomClearance, 0.04f, 0.30f);
        float middle = bottom + height * 0.5f;

        // The anchor intentionally has no collision shape: the building wall supplies
        // its real jamb collision. This body simply fixes the joint in world space.
        AddChild(new StaticBody3D { Name = "HingeAnchor", CollisionLayer = 0, CollisionMask = 0 });

        // Leaf origin is at its centre of mass; the hinge remains on the left edge.
        var leaf = new RigidBody3D
        {
            Name = "Leaf", Position = new Vector3(width * 0.5f, middle, 0),
            CollisionLayer = 1, CollisionMask = 1,
            Mass = Mathf.Clamp(s.MassKg, 1f, 50f),
            AngularDamp = Mathf.Clamp(s.AngularDamping, 0f, 12f),
            CanSleep = true, ContinuousCd = true
        };
        AddChild(leaf);
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
            Shape = new BoxShape3D { Size = new Vector3(width, height, thickness) }
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

        // Hinge's local +Y is the vertical rotation axis. Bidirectional angular limits
        // allow physical pushing from either side; no motor, lock or scripted auto-close.
        var joint = new HingeJoint3D
        {
            Name = "HingeJoint3D", Position = new Vector3(0, middle, 0),
            NodeA = new NodePath("../HingeAnchor"),
            NodeB = new NodePath("../Leaf"),
            ExcludeNodesFromCollision = true
        };
        joint.SetFlag(HingeJoint3D.Flag.UseLimit, true);
        float limit = Mathf.DegToRad(Mathf.Clamp(s.SwingLimitDegrees, 25f, 120f));
        joint.SetParam(HingeJoint3D.Param.LimitLower, -limit);
        joint.SetParam(HingeJoint3D.Param.LimitUpper, limit);
        joint.SetFlag(HingeJoint3D.Flag.EnableMotor, false);
        AddChild(joint);
        SetProcess(false); SetPhysicsProcess(false);
    }
}
