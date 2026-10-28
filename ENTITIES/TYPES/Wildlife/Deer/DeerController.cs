// Deer brain: shared EntityCore/health, VisibilityResolver and GroundGoalSampler.
// A one-shot timer decides wandering/flight; only moving uses physics and lightweight leg gait.
using Godot;

public partial class DeerController : EntityCore, IDamageReceiver
{
    public float Health => _health?.CurrentHealth ?? 0f;
    public bool IsDead => _health?.IsDead ?? false;
    public bool IsFleeing => _fleeing;
    public Vector3 WanderAnchor => _anchor;
    private HealthComponent _health = null!;
    private DeerSettings _config = null!;
    private Timer _brain = null!;
    private CollisionShape3D _body = null!;
    private PhysicsRayQueryParameters3D _groundRay = null!;
    private PhysicsShapeQueryParameters3D _goalQuery = null!;
    private readonly RandomNumberGenerator _random = new();
    private Node3D? _target;
    private IPerceptionTarget? _perception;
    private VisibilityResolver? _visibility;
    private System.Func<Vector3, float>? _groundHeight;
    private Node3D[] _legs = System.Array.Empty<Node3D>();
    private Vector3 _anchor, _goal;
    private ulong _restUntil;
    private float _gait, _stuck;
    private bool _configured, _fleeing, _hasGoal, _reportedBlockedGoals, _reportedFirstMove;
    private int _failedGoalRounds;

    // =========================================================
    // The scene master starts dormant. The active world injects target/terrain after placement.
    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("Health");
        _body = GetNode<CollisionShape3D>("CollisionShape3D");
        _brain = new Timer { Name = "DecisionTimer", OneShot = true,
            ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_brain); _brain.Timeout += Think;
        _groundRay = new PhysicsRayQueryParameters3D { CollisionMask = 1, CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        _goalQuery = new PhysicsShapeQueryParameters3D
        {
            Shape = _body.Shape, CollisionMask = 17, CollideWithBodies = true,
            CollideWithAreas = false, Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };
        _legs = new[] {
            VisualRoot.GetNode<Node3D>("Generated/LFrontLegPivot"),
            VisualRoot.GetNode<Node3D>("Generated/RFrontLegPivot"),
            VisualRoot.GetNode<Node3D>("Generated/LRearLegPivot"),
            VisualRoot.GetNode<Node3D>("Generated/RRearLegPivot")
        };
        FloorSnapLength = 0.5f;
        base._Ready();
        _config = (DeerSettings)RuntimeSettings;
        _health.Damaged += OnDamaged; _health.Died += OnKilled;
        _health.Configure(_config.HealthProfile);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Like zombies, deer take explicit world placement, one target and optional terrain-height input.
    public void InitializeTarget(Node3D player, uint seed, System.Func<Vector3, float>? groundHeight = null)
    {
        Deactivate();
        _target = player; _perception = player as IPerceptionTarget;
        _visibility = player is CollisionObject3D body && _perception != null
            ? new VisibilityResolver(this, body, _config.SightBlockerMask) : null;
        _random.Seed = seed; _groundHeight = groundHeight;
        _anchor = GlobalPosition; _goal = GlobalPosition;
        _fleeing = _hasGoal = _reportedBlockedGoals = _reportedFirstMove = false;
        _failedGoalRounds = 0; _stuck = _gait = 0f;
        _restUntil = Time.GetTicksMsec() + RestMilliseconds();
        _health.Configure(_config.HealthProfile);
        _configured = true; Activate();
        // One diagnostic at initialization lets test builds distinguish a sleeping AI
        // from a deer with obstructed goals; this never logs from the frame loop.
        GD.Print($"[Deer] AI initialized: active={IsActive}, timer={_brain.TimeLeft:F2}s, pos={GlobalPosition}");
    }

    // =========================================================
    public void ReceiveHit(DamagePacket hit)
    { if (IsActive && _configured && !_health.IsDead) _health.ApplyDamage(hit); }

    // =========================================================
    // The deer flees from damage even if the attacker was outside its view cone.
    private void OnDamaged(DamageResult result)
    {
        if (!IsActive || !_configured || _health.IsDead || _fleeing) return;
        Node3D? attacker = result.Hit.Source;
        Vector3 from = GodotObject.IsInstanceValid(attacker) ? attacker!.GlobalPosition
            : GlobalPosition - result.Hit.Direction * 3f;
        BeginFlee(from);
    }

    // =========================================================
    // Close proximity always alerts; otherwise share zombies' line-of-sight/cover maths.
    private bool DetectPlayer()
    {
        if (!GodotObject.IsInstanceValid(_target) || !_target!.IsInsideTree()
            || _target.IsQueuedForDeletion()) return false;
        Vector3 offset = _target.GlobalPosition - GlobalPosition; offset.Y = 0f;
        float squared = offset.LengthSquared(), range = Mathf.Clamp(_config.DetectionRange, 2f, 35f);
        if (squared > range * range) return false;
        if (squared <= 16f) return true;
        if (_visibility == null || _perception == null) return false;
        Vector3 forward = -GlobalBasis.Z; forward.Y = 0f; forward = forward.Normalized();
        float angle = Mathf.Clamp(_config.VisionAngleDegrees, 0f, 360f);
        if (angle < 359.9f && forward.Dot(offset.Normalized()) < Mathf.Cos(Mathf.DegToRad(angle * 0.5f))) return false;
        return _visibility.CanSee(GetWorld3D().DirectSpaceState,
            GlobalPosition + Vector3.Up * Mathf.Clamp(_config.EyeHeight, 0.1f, 2f), _perception);
    }

    // =========================================================
    // One low-frequency decision tick; never performs repeated perception rays per render frame.
    private void Think()
    {
        if (!IsActive || !_configured || _health.IsDead) return;
        if (!_fleeing && DetectPlayer()) BeginFlee(_target!.GlobalPosition);
        if (!_hasGoal && !_fleeing && Time.GetTicksMsec() >= _restUntil)
        {
            bool found = GroundGoalSampler.TryWander(this, GetWorld3D().DirectSpaceState,
                _groundRay, _goalQuery, _body, _random, _anchor, _config.WanderRadius,
                out Vector3 destination, strictSweep: false, maxHeightChange: 2.5f);
            // A shorter local choice rescues wandering near tree lines where every long
            // straight line is blocked. No expensive navigation/path rebuild is needed.
            if (!found) found = GroundGoalSampler.TryWander(this, GetWorld3D().DirectSpaceState,
                _groundRay, _goalQuery, _body, _random, GlobalPosition,
                Mathf.Min(3.5f, _config.WanderRadius), out destination,
                strictSweep: false, maxHeightChange: 1.4f);
            if (found) StartTravel(destination, false);
            else
            {
                _restUntil = Time.GetTicksMsec() + RestMilliseconds();
                if (++_failedGoalRounds >= 3 && !_reportedBlockedGoals)
                {
                    _reportedBlockedGoals = true;
                    GD.PushWarning($"[Deer] 3 wander rounds rejected near {GlobalPosition}. Check terrain and collisions.");
                }
            }
        }
        _brain.Start(Mathf.Clamp(_config.DecisionIntervalSeconds, 0.15f, 2f));
    }

    // =========================================================
    // Try a straight escape away from the player. Bounded angle/distance fallbacks stop
    // rather than crossing solid objects if there is no safe path (no navigation yet).
    private void BeginFlee(Vector3 threat)
    {
        // Mark flight before choosing a goal: a blocked attempt still reanchors at the stop.
        _fleeing = true;
        Vector3 away = GlobalPosition - threat; away.Y = 0f;
        if (away.LengthSquared() < 0.01f) away = -GlobalBasis.Z;
        away = away.Normalized();
        var space = GetWorld3D().DirectSpaceState;
        float length = Mathf.Clamp(_config.FleeDistance, 3f, 40f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i % 6 switch
            {
                0 => 0f, 1 => 0.32f, 2 => -0.32f,
                3 => 0.65f, 4 => -0.65f, _ => 1.05f
            };
            float factor = i < 6 ? 1f : 0.38f;
            Vector3 desired = GlobalPosition + away.Rotated(Vector3.Up, angle) * (length * factor);
            if (GroundGoalSampler.TryGoal(this, space, _groundRay, _goalQuery, _body, desired,
                out Vector3 safe, strictSweep: false, maxHeightChange: 3f))
            { StartTravel(safe, true); return; }
        }
        if (!_reportedBlockedGoals)
        {
            _reportedBlockedGoals = true;
            GD.PushWarning($"[Deer] Cannot find escape route near {GlobalPosition}; will retry sensing.");
        }
        FinishTravel();
    }

    // =========================================================
    private void StartTravel(Vector3 destination, bool fleeing)
    {
        _goal = destination; _fleeing = fleeing; _hasGoal = true;
        _failedGoalRounds = 0; _restUntil = 0; _stuck = 0f; SetPhysicsProcess(true);
        if (!_reportedFirstMove)
        {
            _reportedFirstMove = true;
            GD.Print($"[Deer] First travel: fleeing={fleeing}, from={GlobalPosition}, goal={destination}");
        }
    }

    // =========================================================
    // MoveAndSlide for real collision. Sample terrain height only during active travel,
    // since the native terrain collision patch may not cover off-camera entities.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || !_configured || !_hasGoal) { SetPhysicsProcess(false); return; }
        float dt = Mathf.Max((float)delta, 0.0001f);
        Vector3 offset = _goal - GlobalPosition; offset.Y = 0f;
        float remaining = offset.Length();
        if (remaining < 0.6f) { FinishTravel(); return; }
        Vector3 dir = offset / remaining;
        float speed = Mathf.Clamp(_fleeing ? _config.FleeSpeed : _config.WanderSpeed, 0.2f, _fleeing ? 14f : 4f);
        float braking = Mathf.Clamp(_config.Braking, 1f, 30f);
        float desiredSpeed = Mathf.Min(speed, Mathf.Sqrt(2f * braking * Mathf.Max(0f, remaining - 0.6f)));
        Vector3 current = new(Velocity.X, 0f, Velocity.Z);
        Vector3 velocity = current.MoveToward(dir * desiredSpeed,
            (desiredSpeed < current.Length() ? braking : Mathf.Clamp(_config.Acceleration, 1f, 20f)) * dt);
        if (velocity.Length() * dt > remaining) velocity = dir * (remaining / dt);
        Vector3 next = GlobalPosition + velocity * dt;
        float ground = _groundHeight?.Invoke(next) ?? GlobalPosition.Y;
        if (!float.IsFinite(ground)) { FinishTravel(); return; }
        float vertical = _groundHeight != null ? (ground + 0.06f - GlobalPosition.Y) / dt
            : IsOnFloor() ? -0.1f : Velocity.Y - 20f * dt;
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(velocity.X, vertical, velocity.Z);
        MoveAndSlide();
        Vector3 moved = GlobalPosition - before; moved.Y = 0f;
        if (moved.LengthSquared() < 0.000025f) _stuck += dt;
        else { _stuck = 0f; AnimateStride(moved, dt); }
        if (_stuck > 1.5f || GlobalPosition.DistanceSquaredTo(_goal) < 0.36f) FinishTravel();
    }

    // =========================================================
    // One lightweight gait updates the four hip pivots while moving; no idle animation loop.
    private void AnimateStride(Vector3 movement, float dt)
    {
        float distance = movement.Length();
        if (distance < 0.0001f) return;
        float wantedYaw = Mathf.Atan2(-movement.X, -movement.Z);
        float angle = Mathf.Wrap(wantedYaw - Rotation.Y, -Mathf.Pi, Mathf.Pi);
        float maxTurn = Mathf.DegToRad(Mathf.Clamp(_config.TurnDegreesPerSecond, 45f, 720f)) * dt;
        Rotation = new Vector3(0f, Rotation.Y + Mathf.Clamp(angle, -maxTurn, maxTurn), 0f);
        _gait += distance * (_fleeing ? 8.5f : 5f);
        float swing = Mathf.Sin(_gait) * (_fleeing ? 0.48f : 0.23f);
        _legs[0].Rotation = new Vector3(swing, 0f, 0f);
        _legs[1].Rotation = new Vector3(-swing, 0f, 0f);
        _legs[2].Rotation = new Vector3(-swing, 0f, 0f);
        _legs[3].Rotation = new Vector3(swing, 0f, 0f);
    }

    // =========================================================
    // A completed/blocked flight re-anchors to the actual stopped point, not the target point.
    private void FinishTravel()
    {
        if (_fleeing) _anchor = GlobalPosition;
        _fleeing = _hasGoal = false; _stuck = 0f;
        _restUntil = Time.GetTicksMsec() + RestMilliseconds();
        Velocity = Vector3.Zero; SetPhysicsProcess(false);
        foreach (Node3D leg in _legs) leg.Rotation = Vector3.Zero;
    }

    // =========================================================
    private ulong RestMilliseconds()
    {
        float a = Mathf.Clamp(_config.WaitMinSeconds, 0.5f, 8f);
        float b = Mathf.Clamp(_config.WaitMaxSeconds, 0.5f, 10f);
        return (ulong)Mathf.RoundToInt(_random.RandfRange(Mathf.Min(a, b), Mathf.Max(a, b)) * 1000f);
    }

    // =========================================================
    protected override void OnActivated()
    {
        if (_configured)
            _brain.Start(_random.RandfRange(0.05f, Mathf.Max(0.05f, _config.DecisionIntervalSeconds)));
    }

    // =========================================================
    protected override void OnDeactivated()
    {
        _brain?.Stop(); _hasGoal = _fleeing = false; _stuck = 0f;
        foreach (Node3D leg in _legs) leg.Rotation = Vector3.Zero;
    }

    // =========================================================
    private void OnKilled(DamagePacket hit)
    {
        Deactivate(); CollisionLayer = 0; CollisionMask = 0; Hide(); QueueFree();
    }

    // =========================================================
    protected override void OnEntityExitTree()
    {
        _brain.Timeout -= Think;
        if (GodotObject.IsInstanceValid(_health))
        { _health.Damaged -= OnDamaged; _health.Died -= OnKilled; }
        _target = null; _perception = null; _visibility = null; _groundHeight = null;
    }
}
