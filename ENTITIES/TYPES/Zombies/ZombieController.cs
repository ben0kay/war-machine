// Dumb reusable zombie: staggered range decisions, slow wandering/chasing, and one shambling animation driver.
using Godot;

public partial class ZombieController : EntityCore, IDamageReceiver
{
    public float Health => _health == null ? 0 : _health.CurrentHealth;
    private HealthComponent _health = null!;
    public bool IsChasing { get; private set; }
    private Node3D? _target;
    private ZombieSettings _config = null!;
    private Timer _brain = null!;
    private readonly RandomNumberGenerator _random = new();
    private System.Func<Vector3, float>? _groundHeight;
    private PhysicsRayQueryParameters3D _groundRay = null!;
    private PhysicsShapeQueryParameters3D _recoveryQuery = null!;
    private CollisionShape3D _bodyShape = null!;
    private bool _reportedGroundFailure;
    private Vector3 _home, _goal;
    private bool _hasGoal, _configured;
    private ulong _restUntil;
    private float _phase, _stuck;
    private Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    // =========================================================
    // Art and one decision timer are built once. The inherited scene stays dormant until placement supplies a target.
    public override void _Ready()
    {
        if (_brain == null)
        {
            var mesh = new ZombieMeshBuilder(VisualRoot);
            _leftArm = mesh.LeftArm; _rightArm = mesh.RightArm; _leftLeg = mesh.LeftLeg; _rightLeg = mesh.RightLeg;
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
    // World owns placement and target references; the entity has no dependency on PlayerController or Terrain3D.
    public void InitializeTarget(Node3D player, uint seed, System.Func<Vector3, float>? groundHeight = null)
    {
        Deactivate(); _groundHeight = groundHeight; _reportedGroundFailure = false; _target = player; _home = GlobalPosition; _random.Seed = seed;
        _health.Configure(_config.HealthProfile);
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
    { if (_configured) { _brain.Start(_random.RandfRange(0.05f, 0.4f)); SetPhysicsProcess(true); } }

    // =========================================================
    protected override void OnDeactivated()
    { _brain?.Stop(); _hasGoal = false; IsChasing = false; if (_leftArm != null) Pose(0); }

    // =========================================================
    // Range-only awareness, intentionally no LOS/stealth/attack logic. Hysteresis avoids boundary flicker.
    private void Think()
    {
        if (!IsActive || !_configured) return;
        CheckGroundPenetration();
        bool chasing = false;
        if (IsInstanceValid(_target))
        {
            Vector3 offset = _target!.GlobalPosition - GlobalPosition; offset.Y = 0;
            float range = IsChasing ? Mathf.Max(_config.DetectionRange, _config.LoseRange) : _config.DetectionRange;
            chasing = offset.LengthSquared() <= Mathf.Max(0, range) * Mathf.Max(0, range);
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
        _brain.Start(0.4);
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
        float dt = (float)delta;
        Vector3 offset = _goal - GlobalPosition; offset.Y = 0;
        float stop = IsChasing ? Mathf.Max(0.7f, _config.StopDistance) : 0.4f;
        bool walking = _hasGoal && offset.LengthSquared() > stop * stop;
        if (_hasGoal && !walking && !IsChasing) Rest();
        Vector3 direction = walking ? offset.Normalized() : Vector3.Zero;
        float speed = Mathf.Max(0, IsChasing ? _config.ChaseSpeed : _config.WanderSpeed);
        Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(direction * speed, dt * 5);
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? -0.1f : Velocity.Y - 20 * dt, horizontal.Z);
        MoveAndSlide();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        bool moved = travel.LengthSquared() > 0.000001f;
        if (moved)
        {
            float yaw = Mathf.Atan2(-travel.X, -travel.Z);
            VisualRoot.Rotation = new Vector3(0, Mathf.LerpAngle(VisualRoot.Rotation.Y, yaw, 1 - Mathf.Exp(-4 * dt)), 0);
            _phase += travel.Length() * 5; Pose(Mathf.Sin(_phase)); _stuck = 0;
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
    private void Pose(float step)
    {
        VisualRoot.Position = Vector3.Up * (Mathf.Abs(step) * 0.015f);
        _leftArm.Rotation = new Vector3((IsChasing ? 1.4f : 0.15f) - step * 0.08f, 0, -0.08f);
        _rightArm.Rotation = new Vector3((IsChasing ? 1.4f : 0.15f) + step * 0.08f, 0, 0.08f);
        _leftLeg.Rotation = new Vector3(step * 0.22f, 0, 0); _rightLeg.Rotation = new Vector3(-step * 0.22f, 0, 0);
    }

}
