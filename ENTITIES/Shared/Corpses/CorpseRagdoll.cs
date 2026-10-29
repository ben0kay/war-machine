// Active-only connected rigid bodies for procedural rigs; bake the final pose and release joints/bodies at rest.
using Godot;

public sealed class CorpseRagdoll
{
    private sealed class Part
    {
        public CorpseRagdollPart Spec = null!;
        public Node3D Parent = null!;
        public RigidBody3D Body = null!;
        public BoxShape3D Shape = null!;
    }
    private static readonly PhysicsMaterial ContactMaterial = new() { Friction = 0.65f, Bounce = 0 };
    private readonly Part[] _parts;
    private readonly Node3D _runtime;
    private readonly CorpseSettings _settings;
    private readonly Vector3 _start;
    private readonly PhysicsRayQueryParameters3D _support;
    private float _age, _stable;
    public bool Finished { get; private set; }
    public int PartCount => _parts.Length;
    public Aabb Bounds { get; private set; }

    // =========================================================
    // All runtime nodes remain inside the original art root so render/debug visibility still owns the whole corpse.
    public CorpseRagdoll(Node3D visual, CorpseRagdollPart[] specs, CorpseSettings settings, Vector3 direction, Vector3 velocity)
    {
        _settings = settings; _parts = new Part[specs.Length];
        _runtime = new Node3D { Name = "DeathPhysics" }; visual.AddChild(_runtime);
        float total = 0;
        foreach (CorpseRagdollPart spec in specs) total += spec.MassWeight;
        for (int i = 0; i < specs.Length; i++)
        {
            CorpseRagdollPart spec = specs[i];
            var body = new RigidBody3D {
                Name = $"Part{i}", Freeze = true, CanSleep = false, ContinuousCd = true,
                CollisionLayer = CorpseBody.PileLayer, CollisionMask = 1 | CorpseBody.PileLayer,
                Mass = settings.MassKg * spec.MassWeight / total,
                LinearDampMode = RigidBody3D.DampMode.Replace, LinearDamp = settings.RagdollLinearDamping,
                AngularDampMode = RigidBody3D.DampMode.Replace, AngularDamp = settings.RagdollAngularDamping,
                PhysicsMaterialOverride = ContactMaterial
            };
            _runtime.AddChild(body);
            body.GlobalTransform = new Transform3D(spec.Visual.GlobalBasis.Orthonormalized(), spec.Visual.ToGlobal(spec.Center));
            var shape = new BoxShape3D { Size = spec.Size };
            body.AddChild(new CollisionShape3D { Shape = shape });
            _parts[i] = new Part { Spec = spec, Body = body, Shape = shape, Parent = (Node3D)spec.Visual.GetParent() };
        }
        // Save parents and all body frames before reparenting any nested art pivots.
        foreach (Part part in _parts) part.Spec.Visual.Reparent(part.Body, true);
        for (int i = 0; i < _parts.Length; i++)
        {
            Part part = _parts[i];
            for (int j = 0; j < i; j++) part.Body.AddCollisionExceptionWith(_parts[j].Body);
            if (part.Spec.Parent >= 0) Connect(part, _parts[part.Spec.Parent]);
        }
        _start = _parts[0].Body.GlobalPosition;
        _support = new PhysicsRayQueryParameters3D { CollisionMask = 1 | CorpseBody.PileLayer, CollideWithAreas = false };
        var excluded = new Godot.Collections.Array<Rid>();
        foreach (Part part in _parts) excluded.Add(part.Body.GetRid());
        _support.Exclude = excluded;
        Vector3 spin = Vector3.Up.Cross(direction) * settings.InitialAngularSpeed;
        foreach (Part part in _parts)
        {
            part.Body.Freeze = false;
            part.Body.LinearVelocity = velocity.LimitLength(2) * 0.25f + direction * settings.PushSpeed
                + spin.Cross(part.Body.GlobalPosition - _start) * 0.3f;
            part.Body.AngularVelocity = spin;
        }
        visual.ResetPhysicsInterpolation(); UpdateBounds();
    }

    // =========================================================
    // Anatomical limits have no motor: shoulders/hips/spine bend freely within a cone; elbows/knees hinge.
    private void Connect(Part child, Part parent)
    {
        CorpseRagdollPart spec = child.Spec;
        Joint3D joint;
        if (spec.Hinge)
        {
            var hinge = new HingeJoint3D();
            hinge.SetFlag(HingeJoint3D.Flag.UseLimit, true);
            hinge.SetParam(HingeJoint3D.Param.LimitLower, Mathf.DegToRad(spec.LowerDegrees));
            hinge.SetParam(HingeJoint3D.Param.LimitUpper, Mathf.DegToRad(spec.UpperDegrees));
            joint = hinge;
        }
        else
        {
            var cone = new ConeTwistJoint3D();
            cone.SetParam(ConeTwistJoint3D.Param.SwingSpan, Mathf.DegToRad(spec.SwingDegrees));
            cone.SetParam(ConeTwistJoint3D.Param.TwistSpan, Mathf.DegToRad(spec.TwistDegrees));
            joint = cone;
        }
        _runtime.AddChild(joint); joint.GlobalTransform = spec.Anchor;
        joint.ExcludeNodesFromCollision = true;
        joint.NodeA = joint.GetPathTo(parent.Body); joint.NodeB = joint.GetPathTo(child.Body);
    }

    // =========================================================
    // One owner callback checks all bounded parts. No body scripts, timers, scene searches or hot-path arrays.
    public bool Advance(float delta, out bool supported)
    {
        supported = false; if (Finished) return false;
        _age += delta;
        bool slow = true;
        foreach (Part part in _parts)
        {
            Vector3 linear = part.Body.LinearVelocity, angular = part.Body.AngularVelocity;
            if (!part.Body.GlobalPosition.IsFinite()
                || part.Body.GlobalPosition.DistanceSquaredTo(_start) > (_settings.MaximumTravel + 2) * (_settings.MaximumTravel + 2)) { Finished = true; return true; }
            if (linear.LengthSquared() > 49) part.Body.LinearVelocity = linear.LimitLength(7);
            if (angular.LengthSquared() > 36) part.Body.AngularVelocity = angular.LimitLength(6);
            slow &= linear.LengthSquared() <= _settings.StableSpeed * _settings.StableSpeed
                && angular.LengthSquared() <= _settings.StableAngularSpeed * _settings.StableAngularSpeed;
        }
        _stable = slow ? _stable + delta : 0;
        bool escaped = _parts[0].Body.GlobalPosition.DistanceSquaredTo(_start) > _settings.MaximumTravel * _settings.MaximumTravel;
        if (!escaped && _age < _settings.MaximumFallSeconds
            && (_age < _settings.MinimumFallSeconds || _stable < _settings.StableSeconds)) return false;
        Finished = true;
        if (escaped) return true;
        int contacts = 0;
        for (int i = 0; i < _parts.Length; i++)
        {
            Part part = _parts[i]; Basis basis = part.Body.GlobalBasis;
            Vector3 size = part.Spec.Size;
            float depth = (Mathf.Abs(basis.X.Y) * size.X + Mathf.Abs(basis.Y.Y) * size.Y + Mathf.Abs(basis.Z.Y) * size.Z) * 0.5f;
            Vector3 center = part.Body.GlobalPosition;
            _support.From = center + Vector3.Up * 0.02f;
            _support.To = center - Vector3.Up * (depth + 0.12f);
            var hit = part.Body.GetWorld3D().DirectSpaceState.IntersectRay(_support);
            if (hit.Count == 0 || hit["normal"].AsVector3().Y < 0.45f) continue;
            if (i < 2 || ++contacts >= 2) { supported = true; break; }
        }
        return true;
    }

    // =========================================================
    // Bake original rig transforms without another pose.Apply(1); retain only static compound pile contact shapes.
    public Aabb Bake(CorpseBody owner)
    {
        Stop(); UpdateBounds();
        foreach (Part part in _parts)
        {
            part.Spec.Visual.Reparent(part.Parent, true);
            var shape = new CollisionShape3D { Shape = part.Shape };
            owner.AddChild(shape); shape.GlobalTransform = part.Body.GlobalTransform;
        }
        ((Node3D)_runtime.GetParent()).ResetPhysicsInterpolation();
        _runtime.QueueFree();
        return Bounds;
    }

    // =========================================================
    // Clear contact immediately on expiry/retirement, including queued bodies waiting for end-of-frame deletion.
    public void Stop()
    {
        foreach (Part part in _parts)
        {
            part.Body.Freeze = true; part.Body.LinearVelocity = part.Body.AngularVelocity = Vector3.Zero;
            part.Body.CollisionLayer = part.Body.CollisionMask = 0;
        }
    }

    // =========================================================
    private void UpdateBounds()
    {
        bool first = true;
        foreach (Part part in _parts)
        {
            Aabb box = part.Body.GlobalTransform * new Aabb(-part.Spec.Size * 0.5f, part.Spec.Size);
            Bounds = first ? box : Bounds.Merge(box); first = false;
        }
    }
}
