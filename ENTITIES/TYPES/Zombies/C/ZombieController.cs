// Dumb reusable zombie: staggered range decisions, slow wandering/chasing, and one shambling animation driver.
using Godot;

public partial class ZombieController : EntityCore, IDamageReceiver
{
    public float Health { get; private set; }
    private bool _dead;
    public bool IsChasing { get; private set; }
    private Node3D? _target;
    private ZombieSettings _config = null!;
    private Timer _brain = null!;
    private readonly RandomNumberGenerator _random = new();
    private Vector3 _home, _goal;
    private bool _hasGoal, _configured;
    private ulong _restUntil;
    private float _phase, _stuck;
    private Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly StandardMaterial3D Skin = new() { AlbedoColor = new Color(0.39f, 0.49f, 0.32f), Roughness = 1 };
    private static readonly StandardMaterial3D Shirt = new() { AlbedoColor = new Color(0.36f, 0.3f, 0.25f), Roughness = 1 };
    private static readonly StandardMaterial3D Pants = new() { AlbedoColor = new Color(0.2f, 0.25f, 0.28f), Roughness = 1 };

    // =========================================================
    // Art and one decision timer are built once. The inherited scene stays dormant until placement supplies a target.
    public override void _Ready()
    {
        if (_brain == null)
        {
            BuildArt();
            _brain = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
            AddChild(_brain); _brain.Timeout += Think;
        }
        FloorSnapLength = 0.5f;
        base._Ready();
        _config = (ZombieSettings)RuntimeSettings;
    }

    // =========================================================
    // World owns placement and target references; the entity has no dependency on TopDownPlayer or Terrain3D.
    public void InitializeTarget(Node3D player, uint seed)
    {
        Deactivate(); _target = player; _home = GlobalPosition; _random.Seed = seed;
        Health = Mathf.Max(1, _config.MaxHealth); _dead = false;
        _configured = true; _hasGoal = false; IsChasing = false; _restUntil = 0; Activate();
    }

    // =========================================================
    // Shared hit contract: no weapon lookup, polling or knowledge of rifle balance. Dead actors stop immediately.
    public void ReceiveHit(float damage, Vector3 position, Vector3 direction, Node3D source)
    {
        if (_dead || !IsActive || !float.IsFinite(damage) || damage <= 0) return;
        Health = Mathf.Max(0, Health - damage);
        if (Health > 0) return;
        _dead = true; Deactivate(); CollisionLayer = 0; CollisionMask = 0; Hide(); QueueFree();
    }

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

    // =========================================================
    // Original low-poly placeholder; shared meshes/materials, cached joints, no per-limb scripts or allocations in gait.
    private void BuildArt()
    {
        Part(VisualRoot, "Torso", new(0, 1.1f, 0), new(0.48f, 0.6f, 0.28f), Shirt);
        Part(VisualRoot, "Head", new(0, 1.56f, -0.04f), new(0.32f, 0.36f, 0.32f), Skin);
        _leftArm = Joint("LeftArm", new(-0.32f, 1.3f, 0)); _rightArm = Joint("RightArm", new(0.32f, 1.3f, 0));
        foreach (var arm in new[] { _leftArm, _rightArm })
        {
            Part(arm, "Sleeve", new(0, -0.15f, 0), new(0.17f, 0.3f, 0.2f), Shirt);
            Part(arm, "Forearm", new(0, -0.42f, 0), new(0.14f, 0.27f, 0.16f), Skin);
        }
        _leftLeg = Joint("LeftLeg", new(-0.13f, 0.82f, 0)); _rightLeg = Joint("RightLeg", new(0.13f, 0.82f, 0));
        foreach (var leg in new[] { _leftLeg, _rightLeg })
            Part(leg, "Leg", new(0, -0.4f, -0.025f), new(0.22f, 0.8f, 0.27f), Pants);
        Pose(0);
    }

    // =========================================================
    private Node3D Joint(string name, Vector3 position)
    { var joint = new Node3D { Name = name, Position = position }; VisualRoot.AddChild(joint); return joint; }

    // =========================================================
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 scale, Material material)
    { parent.AddChild(new MeshInstance3D { Name = name, Mesh = Box, Position = position, Scale = scale, MaterialOverride = material }); }
}
