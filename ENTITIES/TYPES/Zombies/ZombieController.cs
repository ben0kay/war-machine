// Reusable zombie: shared sight, health, roaming and chase with overridable artwork/gait for future variants.
using Godot;

public partial class ZombieController : EntityCore, IDamageReceiver
{
    public float Health => _health == null ? 0 : _health.CurrentHealth;
    private HealthComponent _health = null!;
    public bool IsChasing { get; private set; }
    private Node3D? _target;
    private IPerceptionTarget? _sightTarget;
    private VisibilityResolver? _visibility;
    private MeshInstance3D? _visionCone;
    private bool _showVisionCone;
    public float VisionRange => IsChasing ? Mathf.Max(_config.DetectionRange, _config.LoseRange) : _config.DetectionRange;
    private ZombieSettings _config = null!;
    private Timer _brain = null!;
    private readonly RandomNumberGenerator _random = new();
    private System.Func<Vector3, float>? _groundHeight;
    private PhysicsRayQueryParameters3D _groundRay = null!;
    private PhysicsShapeQueryParameters3D _recoveryQuery = null!;
    private CollisionShape3D _bodyShape = null!;
    private bool _reportedGroundFailure, _awaitingSpawnGround, _reportedSpawnGround;
    private Vector3 _home, _goal;
    private bool _hasGoal, _configured;
    private ulong _restUntil;
    private float _phase, _stuck;
    protected Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    protected Node3D _leftKnee = null!, _rightKnee = null!;
    // =========================================================
    // Art and one decision timer are built once. The inherited scene stays dormant until placement supplies a target.
    public override void _Ready()
    {
        if (_brain == null)
        {
            BuildArt();
            Pose(0);
            _health = new HealthComponent { Name = "Health" }; AddChild(_health);
            _health.Died += OnKilled;
            _brain = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
            AddChild(_brain); _brain.Timeout += Think;
        }
        FloorSnapLength = 0.5f;
        _bodyShape = GetNode<CollisionShape3D>("CollisionShape3D");
        _groundRay = new PhysicsRayQueryParameters3D { CollisionMask = 1,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        _recoveryQuery = new PhysicsShapeQueryParameters3D { Shape = _bodyShape.Shape, CollisionMask = CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        base._Ready();
        _config = (ZombieSettings)RuntimeSettings;
    }

    // =========================================================
    // Subtypes only supply art and optional motion tuning; detection, health and AI stay shared.
    protected virtual void BuildArt()
    {
        var mesh = new ZombieMeshBuilder(VisualRoot);
        BindArtJoints(mesh.LeftArm, mesh.RightArm, mesh.LeftLeg, mesh.RightLeg, mesh.LeftKnee, mesh.RightKnee);
    }

    // =========================================================
    // Share the existing six-pivot shamble contract across independent procedural mesh builders.
    protected void BindArtJoints(Node3D leftArm, Node3D rightArm, Node3D leftLeg, Node3D rightLeg,
        Node3D leftKnee, Node3D rightKnee)
    {
        _leftArm = leftArm; _rightArm = rightArm; _leftLeg = leftLeg; _rightLeg = rightLeg;
        _leftKnee = leftKnee; _rightKnee = rightKnee;
    }

    // =========================================================
    // Only species animation/movement feel is customizable; all current zombie timings remain unchanged.
    protected virtual float MovementAcceleration => 5f;
    protected virtual float GaitPhasePerMeter => 5f;

    // =========================================================
    // World owns placement and target references; the entity has no dependency on PlayerController or Terrain3D.
    public void InitializeTarget(Node3D player, uint seed, System.Func<Vector3, float>? groundHeight = null)
    {
        Deactivate(); _groundHeight = groundHeight; _reportedGroundFailure = false; _target = player; _home = GlobalPosition; _random.Seed = seed;
        _sightTarget = player as IPerceptionTarget;
        _visibility = player is CollisionObject3D body && _sightTarget != null
            ? new VisibilityResolver(this, body, _config.SightBlockerMask) : null;
        if (_visibility == null) GD.PushWarning("Zombie target must provide body samples and a physics body; eyesight disabled.");
        _health.Configure(_config.HealthProfile);
        _awaitingSpawnGround = groundHeight != null; _reportedSpawnGround = false;
        ResetPhysicsInterpolation();
        _configured = true; _hasGoal = false; IsChasing = false; _restUntil = 0; Activate();
    }

    // =========================================================
    // Shared health owns all damage maths. Species code only decides whether hits are accepted and how death looks.
    public void ReceiveHit(DamagePacket hit)
    { if (IsActive && _configured) _health.ApplyDamage(hit); }

    // =========================================================
    // The actor owns its death reaction; the resolver has no zombie/robot/deer-specific behaviour.
    private void OnKilled(DamagePacket hit)
    { Deactivate(); CollisionLayer = 0; CollisionMask = 0; Hide(); QueueFree(); }

    // =========================================================
    protected override void OnActivated()
    { if (_configured) { _brain.Start(_random.RandfRange(0.05f, Mathf.Max(0.05f, _config.SightIntervalSeconds))); SetPhysicsProcess(true); } UpdateVisionDebug(); }

    // =========================================================
    protected override void OnDeactivated()
    { _brain?.Stop(); _hasGoal = false; IsChasing = false; if (_leftArm != null) Pose(0); UpdateVisionDebug(); }

    // =========================================================
    // Cheap range and visual-facing gates precede bounded head/torso LOS. No hidden live-position pursuit.
    private void Think()
    {
        if (!IsActive || !_configured) return;
        if (_awaitingSpawnGround && !ConfirmSpawnGround())
        { _brain.Start(Mathf.Max(0.1f, _config.SightIntervalSeconds)); return; }
        CheckGroundPenetration();
        bool chasing = false;
        if (IsInstanceValid(_target) && _target!.IsInsideTree() && _visibility != null && _sightTarget != null)
        {
            Vector3 offset = _target.GlobalPosition - GlobalPosition; offset.Y = 0;
            float squared = offset.LengthSquared(), range = Mathf.Max(0, VisionRange);
            Vector3 forward = -VisualRoot.GlobalBasis.Z; forward.Y = 0; forward = forward.Normalized();
            float threshold = Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(_config.VisionAngleDegrees, 1, 180)) * 0.5f);
            if (squared <= range * range && (squared < 0.000001f || forward.Dot(offset / Mathf.Sqrt(squared)) >= threshold))
                chasing = _visibility.CanSee(GetWorld3D().DirectSpaceState,
                    GlobalPosition + Vector3.Up * _config.EyeHeight, _sightTarget);
        }
        if (chasing != IsChasing) { IsChasing = chasing; _hasGoal = false; Pose(0); }
        if (IsChasing)
        { _goal = _target!.GlobalPosition; _hasGoal = true; }
        else if (!_hasGoal && Time.GetTicksMsec() >= _restUntil)
        {
            float angle = _random.RandfRange(0, Mathf.Tau);
            float radius = Mathf.Sqrt(_random.Randf()) * Mathf.Max(1, _config.WanderRadius);
            _goal = _home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            _hasGoal = true;
        }
        if (_hasGoal || !IsOnFloor()) SetPhysicsProcess(true);
        UpdateVisionDebug();
        _brain.Start(Mathf.Max(0.1f, _config.SightIntervalSeconds));
    }

    // =========================================================
    // Debug owns mesh construction/input; species owns visibility lifecycle. No debug frame callback.
    public void AttachVisionDebug(MeshInstance3D cone)
    { _visionCone = cone; _showVisionCone = _config.ShowVisionCone; UpdateVisionDebug(); }

    // =========================================================
    public void SetVisionDebugVisible(bool visible)
    { _showVisionCone = visible; UpdateVisionDebug(); }

    // =========================================================
    private void UpdateVisionDebug()
    {
        if (_visionCone == null) return;
        bool visible = IsActive && _showVisionCone;
        if (_visionCone.Visible != visible) _visionCone.Visible = visible;
        float range = Mathf.Max(0, VisionRange);
        var scale = new Vector3(range, 1, range);
        if (_visionCone.Scale != scale) _visionCone.Scale = scale;
    }

    // =========================================================
    // Confirm native spawn collision on the existing staggered physics timer before enabling gravity.
    private bool ConfirmSpawnGround()
    {
        float height = _groundHeight!(GlobalPosition);
        bool confirmed = false;
        if (float.IsFinite(height))
        {
            _groundRay.From = new Vector3(GlobalPosition.X, height + 0.6f, GlobalPosition.Z);
            _groundRay.To = new Vector3(GlobalPosition.X, height - 0.6f, GlobalPosition.Z);
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(_groundRay);
            confirmed = hit.Count > 0 && hit["normal"].AsVector3().Y >= Mathf.Cos(FloorMaxAngle);
        }
        if (confirmed) { _awaitingSpawnGround = false; SetPhysicsProcess(true); return true; }
        Velocity = Vector3.Zero; SetPhysicsProcess(false);
        if (!_reportedSpawnGround)
        {
            GD.PushWarning($"Zombie spawn waiting for solid terrain: position={GlobalPosition}, terrain_height={height}, mask={CollisionMask}. No gravity until confirmed; check Terrain3D collision coverage.");
            _reportedSpawnGround = true;
        }
        return false;
    }

    // =========================================================
    // Use the existing staggered physics timer only while falling. The world optionally supplies open-terrain height.
    private void CheckGroundPenetration()
    {
        if (!_config.GroundRecoveryEnabled || _groundHeight == null || IsOnFloor() || Velocity.Y >= -0.1f) return;
        float height = _groundHeight(GlobalPosition);
        // Holes/missing terrain return NaN; ordinary cliff falls remain ABOVE the lower ground and are untouched.
        if (!float.IsFinite(height) || GlobalPosition.Y >= height - Mathf.Max(0.25f, _config.GroundRecoveryDepth)) return;
        Vector3 before = GlobalPosition;
        _groundRay.From = new Vector3(before.X, height + 0.6f, before.Z);
        _groundRay.To = new Vector3(before.X, height - 0.6f, before.Z);
        var space = GetWorld3D().DirectSpaceState;
        var hit = space.IntersectRay(_groundRay);
        string result = "no solid ground detected above sunk zombie";
        if (hit.Count > 0 && hit["normal"].AsVector3().Y >= Mathf.Cos(FloorMaxAngle))
        {
            Vector3 position = hit["position"].AsVector3() + Vector3.Up * 0.12f;
            Transform3D shapeTransform = _bodyShape.GlobalTransform;
            shapeTransform.Origin += position - before; _recoveryQuery.Transform = shapeTransform;
            if (space.IntersectShape(_recoveryQuery, 1).Count == 0)
            {
                GlobalPosition = position; Velocity = Vector3.Zero; ResetPhysicsInterpolation(); SetPhysicsProcess(true);
                result = $"recovered onto confirmed solid surface at {position}";
            }
            else result = "recovery blocked by another collider";
        }
        if (!_reportedGroundFailure)
        {
            GD.PushWarning($"Zombie ground penetration: position={before}, terrain_height={height}, mask={CollisionMask}; {result}.");
            _reportedGroundFailure = true;
        }
    }

    // =========================================================
    // Only moving/falling zombies run physics; waiting or stopped zombies retain just their low-rate decision timer.
    public override void _PhysicsProcess(double delta)
    {
        if (_awaitingSpawnGround) { SetPhysicsProcess(false); return; }
        float dt = (float)delta;
        Vector3 offset = _goal - GlobalPosition; offset.Y = 0;
        float stop = IsChasing ? Mathf.Max(0.7f, _config.StopDistance) : 0.4f;
        bool walking = _hasGoal && offset.LengthSquared() > stop * stop;
        if (_hasGoal && !walking && !IsChasing) Rest();
        Vector3 direction = walking ? offset.Normalized() : Vector3.Zero;
        float speed = Mathf.Max(0, IsChasing ? _config.ChaseSpeed : _config.WanderSpeed);
        Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(direction * speed, dt * Mathf.Max(0.1f, MovementAcceleration));
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? -0.1f : Velocity.Y - 20 * dt, horizontal.Z);
        MoveAndSlide();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        bool moved = travel.LengthSquared() > 0.000001f;
        if (moved)
        {
            float yaw = Mathf.Atan2(-travel.X, -travel.Z);
            VisualRoot.Rotation = new Vector3(0, Mathf.LerpAngle(VisualRoot.Rotation.Y, yaw, 1 - Mathf.Exp(-4 * dt)), 0);
            _phase += travel.Length() * Mathf.Max(0.1f, GaitPhasePerMeter);
            // Flex the airborne knee slightly, using the existing shamble clock; no extra animation loop.
            float swing = Mathf.Cos(_phase);
            Pose(Mathf.Sin(_phase), Mathf.Max(0, swing), Mathf.Max(0, -swing));
            _stuck = 0;
        }
        else { Pose(0); if (walking) _stuck += dt; }
        if (_stuck > 2) { Rest(); _stuck = 0; }
        if ((!walking || !_hasGoal) && horizontal.LengthSquared() < 0.0001f && IsOnFloor())
        { Velocity = Vector3.Zero; SetPhysicsProcess(false); }
    }

    // =========================================================
    // Bounded pauses also recover wandering from a blocked direction; pursuit reacquires on the next decision.
    private void Rest()
    { _hasGoal = false; _restUntil = Time.GetTicksMsec() + (ulong)_random.RandiRange(1000, 3000); }

    // =========================================================
    // Arms extend toward the player during pursuit, including the stop-near-player pose.
    protected virtual void Pose(float step, float leftLift = 0, float rightLift = 0)
    {
        VisualRoot.Position = Vector3.Up * (Mathf.Abs(step) * 0.015f);
        _leftArm.Rotation = new Vector3((IsChasing ? 1.4f : 0.15f) - step * 0.08f, 0, -0.08f);
        _rightArm.Rotation = new Vector3((IsChasing ? 1.4f : 0.15f) + step * 0.08f, 0, 0.08f);
        _leftLeg.Rotation = new Vector3(step * 0.22f, 0, 0); _rightLeg.Rotation = new Vector3(-step * 0.22f, 0, 0);
        _leftKnee.Rotation = new Vector3(-leftLift * 0.32f, 0, 0);
        _rightKnee.Rotation = new Vector3(-rightLift * 0.25f, 0, 0);
    }

}
