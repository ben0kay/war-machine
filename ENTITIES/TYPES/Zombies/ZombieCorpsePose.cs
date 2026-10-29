// Standard/runner death adapter: capture the death pose, supply articulated parts or relax the cheap proxy fallback.
using Godot;

public sealed class ZombieCorpsePose : IArticulatedCorpsePose
{
    public AnatomySnapshot Injuries { get; }
    private readonly Node3D[] _joints;
    private readonly Vector3[] _from, _to;
    private readonly Transform3D[] _captured;

    // =========================================================
    public ZombieCorpsePose(uint seed, AnatomySnapshot injuries, params Node3D[] joints)
    {
        Injuries = injuries;
        _captured = new Transform3D[joints.Length];
        _joints = joints; _from = new Vector3[joints.Length]; _to = new Vector3[joints.Length];
        var random = new RandomNumberGenerator { Seed = seed ^ 0x9e3779b9u };
        for (int i = 0; i < joints.Length; i++)
        {
            _captured[i] = joints[i].Transform; _from[i] = joints[i].Rotation;
            bool knee = i >= 4 && i <= 5;
            _to[i] = new Vector3(knee ? random.RandfRange(-0.65f, -0.08f) : random.RandfRange(-0.35f, 0.45f),
                random.RandfRange(-0.15f, 0.15f), random.RandfRange(-0.3f, 0.3f));
        }
    }

    // =========================================================
    public void Apply(float progress)
    {
        float weight = Mathf.SmoothStep(0, 1, progress);
        for (int i = 0; i < _joints.Length; i++)
            if (GodotObject.IsInstanceValid(_joints[i]))
            { _joints[i].Transform = _captured[i]; _joints[i].Rotation = _from[i].Lerp(_to[i], weight); }
    }

    // =========================================================
    // Map both cached procedural rigs once at death. Missing whole limbs create no invisible physics segments.
    public CorpseRagdollPart[] CreateRagdollParts(Node3D visual)
    {
        var upper = visual.GetNode<Node3D>("UpperBody");
        var pelvis = new Node3D { Name = "DeathPelvis" }; visual.AddChild(pelvis);
        pelvis.GlobalTransform = upper.GlobalTransform;
        foreach (string name in new[] { "Hips", "Waist" })
        {
            Node3D? mesh = upper.GetNodeOrNull<Node3D>(name);
            if (mesh != null) mesh.Reparent(pelvis, true);
        }
        var parts = new System.Collections.Generic.List<CorpseRagdollPart>(11);
        parts.Add(new CorpseRagdollPart { Visual = pelvis, Center = new(0, -0.015f, 0),
            Size = new(0.32f, 0.19f, 0.27f), MassWeight = 12 });
        Add(parts, upper, 0, new(0, 0.26f, -0.025f), new(0.43f, 0.51f, 0.29f),
            23, upper.ToGlobal(new Vector3(0, 0.08f, 0)), 35, 25);
        var head = upper.GetNode<Node3D>("Head");
        Add(parts, head, 1, new(0, -0.025f, -0.015f), new(0.24f, 0.32f, 0.24f),
            5, head.ToGlobal(new Vector3(0, -0.12f, 0)), 40, 30);
        AddLimb(parts, _joints[0], BodyRegion.LeftArm, 1, true);
        AddLimb(parts, _joints[1], BodyRegion.RightArm, 1, true);
        AddLimb(parts, _joints[2], BodyRegion.LeftLeg, 0, false);
        AddLimb(parts, _joints[3], BodyRegion.RightLeg, 0, false);
        return parts.ToArray();
    }

    // =========================================================
    // Existing shoulder/hip animation is unchanged; only death physics drives elbows and knees.
    private void AddLimb(System.Collections.Generic.List<CorpseRagdollPart> parts,
        Node3D limb, BodyRegion region, int parent, bool arm)
    {
        if (Injuries.IsSevered(region))
        {
            // Keep a severed leg's visible stump attached to the falling pelvis.
            if (!arm) limb.Reparent(parts[0].Visual, true);
            return;
        }
        int index = parts.Count;
        Add(parts, limb, parent, new(0, arm ? -0.135f : -0.18f, 0),
            arm ? new(0.17f, 0.27f, 0.18f) : new(0.20f, 0.35f, 0.22f),
            arm ? 2.5f : 7, limb.GlobalPosition, arm ? 85 : 65, arm ? 45 : 25);
        Node3D lower = limb.GetNode<Node3D>(arm ? "Elbow" : "Knee");
        int lowerIndex = parts.Count;
        Add(parts, lower, index, new(0, arm ? -0.15f : -0.18f, arm ? -0.01f : -0.02f),
            arm ? new(0.14f, 0.34f, 0.15f) : new(0.18f, 0.41f, 0.24f),
            arm ? 1.5f : 4, lower.GlobalPosition, 0, 0);
        CorpseRagdollPart spec = parts[lowerIndex];
        spec.Hinge = true;
        float current = Mathf.RadToDeg(lower.Rotation.X);
        spec.LowerDegrees = (arm ? -5 : -135) - current;
        spec.UpperDegrees = (arm ? 140 : 5) - current;
        // HingeJoint3D turns about local Z: map it to the limb's anatomical X flexion axis.
        spec.Anchor = new Transform3D(limb.GlobalBasis.Orthonormalized()
            * new Basis(Vector3.Up, Mathf.Pi * 0.5f), lower.GlobalPosition);
    }

    // =========================================================
    // ConeTwistJoint3D twists about X; align X with the rig's longitudinal Y axis.
    private static void Add(System.Collections.Generic.List<CorpseRagdollPart> parts, Node3D visual,
        int parent, Vector3 center, Vector3 size, float mass, Vector3 anchor, float swing, float twist)
    {
        parts.Add(new CorpseRagdollPart { Visual = visual, Parent = parent, Center = center, Size = size,
            MassWeight = mass, SwingDegrees = swing, TwistDegrees = twist,
            Anchor = new Transform3D(visual.GlobalBasis.Orthonormalized()
                * new Basis(Vector3.Back, Mathf.Pi * 0.5f), anchor) });
    }
}
