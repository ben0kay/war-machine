// ZD0 physics-assisted fall: one temporary box body, short pose relaxation, then frozen collision-free visuals or bounded dismissal.
using Godot;

public partial class CorpseBody : RigidBody3D
{
    public bool IsSettled { get; private set; }
    private ICorpsePoseAdapter _pose = null!;
    private CorpseSettings _settings = null!;
    private CollisionShape3D _shape = null!;
    private PhysicsRayQueryParameters3D _support = null!;
    private Vector3 _start;
    private float _age, _stable;
    private bool _initialized, _finishing;

    // =========================================================
    public override void _Ready() { Freeze = true; SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(Node3D visual, Transform3D at, ICorpsePoseAdapter pose, Vector3 hint, Vector3 velocity,
        uint seed, CorpseSettings settings, bool alignPivotToVisual = false)
    {
        _settings = settings; _pose = pose;
        var random = new RandomNumberGenerator { Seed = seed };
        Vector3 direction = FallDirection(hint, random.RandfRange(-Mathf.Pi, Mathf.Pi));
        direction = direction.Rotated(Vector3.Up, random.RandfRange(-0.15f, 0.15f));
        GlobalTransform = new Transform3D(at.Basis.Orthonormalized(), at.Origin + (alignPivotToVisual ? at.Basis.Y.Normalized() : Vector3.Up) * settings.PivotHeight);
        _start = GlobalPosition;
        _shape = new CollisionShape3D { Shape = new BoxShape3D { Size = settings.ProxySize } }; AddChild(_shape);
        CollisionLayer = 0; CollisionMask = 1; // Contact static world only; no actor, bullet, sight or camera target layer.
        Mass = settings.MassKg; ContinuousCd = true; CanSleep = false;
        LinearDampMode = DampMode.Replace; LinearDamp = settings.LinearDamping;
        AngularDampMode = DampMode.Replace; AngularDamp = settings.AngularDamping;
        PhysicsMaterialOverride = new PhysicsMaterial { Friction = 0.65f, Bounce = 0 };
        visual.Reparent(this, true); visual.GlobalTransform = at; visual.ProcessMode = ProcessModeEnum.Inherit;
        _pose.Apply(0);
        _support = new PhysicsRayQueryParameters3D { CollisionMask = 1, CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        if (!velocity.IsFinite()) velocity = Vector3.Zero;
        velocity.Y = 0;
        _initialized = true; Freeze = false;
        LinearVelocity = velocity.LimitLength(2) * 0.25f + direction * settings.PushSpeed;
        AngularVelocity = Vector3.Up.Cross(direction) * settings.InitialAngularSpeed + Vector3.Up * random.RandfRange(-0.35f, 0.35f);
    }

    // =========================================================
    public static Vector3 FallDirection(Vector3 hint, float fallbackAngle)
    {
        if (hint.IsFinite()) { hint.Y = 0; if (float.IsFinite(hint.LengthSquared()) && hint.LengthSquared() > 0.0001f) return hint.Normalized(); }
        return new Vector3(Mathf.Sin(fallbackAngle), 0, Mathf.Cos(fallbackAngle));
    }

    // =========================================================
    // Physics owns contacts and root motion; no transform snapping, ground teleport, gait or scene searches during the fall.
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!_initialized || _finishing) return;
        _age += (float)state.Step;
        state.LinearVelocity = state.LinearVelocity.LimitLength(7);
        state.AngularVelocity = state.AngularVelocity.LimitLength(6);
        _pose.Apply(Mathf.Clamp(_age / _settings.MinimumFallSeconds, 0, 1));
        bool slow = state.LinearVelocity.LengthSquared() <= _settings.StableSpeed * _settings.StableSpeed
            && state.AngularVelocity.LengthSquared() <= _settings.StableAngularSpeed * _settings.StableAngularSpeed;
        _stable = slow ? _stable + (float)state.Step : 0;
        bool timeout = _age >= _settings.MaximumFallSeconds;
        bool escaped = state.Transform.Origin.DistanceSquaredTo(_start) > _settings.MaximumTravel * _settings.MaximumTravel;
        if (!timeout && !escaped && (_age < _settings.MinimumFallSeconds || _stable < _settings.StableSeconds)) return;
        _support.From = state.Transform.Origin + Vector3.Up * 0.1f;
        _support.To = state.Transform.Origin - Vector3.Up * (_settings.ProxySize.Length() * 0.7f + 0.25f);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(_support);
        bool lying = Mathf.Abs(state.Transform.Basis.Y.Dot(Vector3.Up)) < 0.6f;
        Basis basis = state.Transform.Basis;
        float supportDepth = (Mathf.Abs(basis.X.Y) * _settings.ProxySize.X
            + Mathf.Abs(basis.Y.Y) * _settings.ProxySize.Y + Mathf.Abs(basis.Z.Y) * _settings.ProxySize.Z) * 0.5f;
        bool supported = hit.Count > 0 && hit["normal"].AsVector3().Y >= 0.45f
            && state.Transform.Origin.Y - hit["position"].AsVector3().Y <= supportDepth + 0.15f;
        _finishing = true;
        if (supported && lying && !escaped) CallDeferred(nameof(Settle));
        else CallDeferred(nameof(Dismiss));
    }

    // =========================================================
    public void Settle()
    {
        if (!_initialized || IsQueuedForDeletion()) return;
        Freeze = true; LinearVelocity = AngularVelocity = Vector3.Zero;
        _pose.Apply(1); IsSettled = true;
        CollisionLayer = CollisionMask = 0; _shape.Disabled = true;
        SetProcess(false); SetPhysicsProcess(false); ProcessMode = ProcessModeEnum.Disabled;
    }

    // =========================================================
    // Unsupported/failed falls must not freeze in mid-air or continue simulating indefinitely.
    public void Dismiss() { Freeze = true; SetProcess(false); SetPhysicsProcess(false); QueueFree(); }
}
