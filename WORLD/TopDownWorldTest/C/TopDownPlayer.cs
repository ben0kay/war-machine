// Test-only 1.8m low-poly human: physical walking and one visual gait driver, dormant when grounded/idle.
using Godot;

public partial class TopDownPlayer : CharacterBody3D
{
    public event System.Action<Vector3>? PositionChanged;
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private TopDownTestSettings _settings = null!;
    private Node3D _art = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Node3D _leftFoot = null!, _rightFoot = null!;
    private Node3D _leftKnee = null!, _rightKnee = null!, _upperBody = null!;
    private CollisionShape3D _collider = null!;
    private CapsuleShape3D _capsule = null!;
    private PhysicsShapeQueryParameters3D _standingQuery = null!;
    public bool IsCrouching { get; private set; }
    public event System.Action<bool>? StanceChanged;
    private float _crouch;
    private bool _toggleCrouch;
    private Vector3 _screenRight, _screenForward;
    private float _phase;
    private bool _initialized;

    // =========================================================
    // The test world places the player before enabling physics; no spawn-time fall without terrain data.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Cache ground-plane camera axes and build a small original model once.
    public void Initialize(TopDownTestSettings settings, Camera3D camera)
    {
        _settings = settings;
        _screenRight = camera.GlobalBasis.X; _screenRight.Y = 0; _screenRight = _screenRight.Normalized();
        _screenForward = -camera.GlobalBasis.Z; _screenForward.Y = 0; _screenForward = _screenForward.Normalized();
        FloorSnapLength = 0.5f; FloorMaxAngle = Mathf.DegToRad(46);
        if (!_initialized)
        {
            BuildHuman();
            _collider = GetNode<CollisionShape3D>("CollisionShape3D");
            _capsule = (CapsuleShape3D)_collider.Shape.Duplicate(); _collider.Shape = _capsule;
            var standing = (CapsuleShape3D)_capsule.Duplicate(); standing.Height = 1.8f;
            _standingQuery = new PhysicsShapeQueryParameters3D { Shape = standing, CollisionMask = CollisionMask,
                Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        }
        _initialized = true; Velocity = Vector3.Zero; SetPhysicsProcess(true);
    }

    // =========================================================
    // Keyboard changes wake movement/gravity. No new project-wide actions or explorer controls are introduced.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || input is not InputEventKey key || key.Echo) return;
        if (key.PhysicalKeycode == Key.Ctrl && key.Location == KeyLocation.Right && key.Pressed)
        {
            _toggleCrouch = !_toggleCrouch;
            SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); return;
        }
        if (key.PhysicalKeycode == Key.Up || key.PhysicalKeycode == Key.Left || key.PhysicalKeycode == Key.Down
            || key.PhysicalKeycode == Key.Right || key.PhysicalKeycode == Key.Shift)
            SetPhysicsProcess(true);
    }

    // =========================================================
    // Ground contact sleeps again after focus returns; no held direction is allowed while the window is unfocused.
    public override void _Notification(int what)
    {
        if (_initialized && (what == NotificationApplicationFocusOut || what == NotificationApplicationFocusIn)) SetPhysicsProcess(true);
    }

    // =========================================================
    // Physics root owns movement. Art joints animate from actual horizontal travel and settle immediately at rest.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (_toggleCrouch)
        {
            _toggleCrouch = false;
            // Query only in physics, once per attempt to stand; exclude our own capsule.
            _standingQuery.Transform = GlobalTransform * new Transform3D(Basis.Identity, Vector3.Up * 0.93f);
            if (!IsCrouching || GetWorld3D().DirectSpaceState.IntersectShape(_standingQuery, 1).Count == 0)
            { IsCrouching = !IsCrouching; StanceChanged?.Invoke(IsCrouching); }
        }
        float previousCrouch = _crouch;
        _crouch = Mathf.MoveToward(_crouch, IsCrouching ? 1 : 0, dt * 6);
        if (_crouch != previousCrouch)
        {
            _capsule.Height = Mathf.Lerp(1.8f, 1.4f, _crouch);
            _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
        }
        Vector2 keys = GetWindow().HasFocus() ? new Vector2(
            (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0)) : Vector2.Zero;
        keys = keys.Normalized();
        Vector3 direction = (_screenRight * keys.X + _screenForward * keys.Y).Normalized();
        float speed = Mathf.Max(0.1f, _settings.WalkSpeed) * (IsCrouching ? Mathf.Clamp(_settings.CrouchSpeedMultiplier, 0.1f, 1) : Input.IsPhysicalKeyPressed(Key.Shift) ? Mathf.Max(1, _settings.RunMultiplier) : 1);
        Vector3 horizontal = new(Velocity.X, 0, Velocity.Z);
        horizontal = horizontal.MoveToward(direction * speed, Mathf.Max(1, _settings.Acceleration) * dt);
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? -0.1f : Velocity.Y - Mathf.Max(1, _settings.Gravity) * dt, horizontal.Z);
        MoveAndSlide();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        float actualSpeed = dt > 0 ? travel.Length() / dt : 0;
        if (actualSpeed > 0.05f)
        {
            float yaw = Mathf.Atan2(-travel.X, -travel.Z);
            _art.Rotation = new Vector3(0, Mathf.LerpAngle(_art.Rotation.Y, yaw, 1 - Mathf.Exp(-14 * dt)), 0);
            _phase += dt * Mathf.Tau * Mathf.Max(0.1f, _settings.GaitRate) * actualSpeed / Mathf.Max(0.1f, _settings.WalkSpeed);
            Pose(Mathf.Sin(_phase) * 0.5f, Mathf.Abs(Mathf.Sin(_phase * 2)) * 0.025f);
        }
        else Pose(0, 0);
        if (before.DistanceSquaredTo(GlobalPosition) > 0.0000001f) PositionChanged?.Invoke(GlobalPosition);
        if (IsOnFloor() && keys == Vector2.Zero && horizontal.LengthSquared() < 0.0001f && _crouch == (IsCrouching ? 1 : 0))
        { Velocity = Vector3.Zero; _phase = 0; Pose(0, 0); SetPhysicsProcess(false); }
    }

    // =========================================================
    // One method moves all four joints; no limb scripts, AnimationTree framework or idle animation clock.
    private void Pose(float angle, float bob)
    {
        float stride = angle * Mathf.Lerp(1, 0.4f, _crouch);
        _art.Position = Vector3.Up * (bob * Mathf.Lerp(1, 0.35f, _crouch) - 0.72f * (1 - Mathf.Cos(1.15f * _crouch)));
        _upperBody.Rotation = new Vector3(0.18f * _crouch, 0, 0);
        _leftArm.Rotation = new Vector3(-stride - 0.4f * _crouch, 0, 0);
        _rightArm.Rotation = new Vector3(stride - 0.4f * _crouch, 0, 0);
        _leftLeg.Rotation = new Vector3(stride + 1.15f * _crouch, 0, 0);
        _rightLeg.Rotation = new Vector3(-stride + 1.15f * _crouch, 0, 0);
        _leftKnee.Rotation = new Vector3(-2.3f * _crouch, 0, 0);
        _rightKnee.Rotation = new Vector3(-2.3f * _crouch, 0, 0);
        _leftFoot.Rotation = new Vector3(-_leftLeg.Rotation.X - _leftKnee.Rotation.X, 0, 0);
        _rightFoot.Rotation = new Vector3(-_rightLeg.Rotation.X - _rightKnee.Rotation.X, 0, 0);
    }

    // =========================================================
    // Shared box geometry and four simple materials keep the visible test human cheap and replaceable.
    private void BuildHuman()
    {
        _art = new Node3D { Name = "VisualRoot" }; AddChild(_art);
        var shirt = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.55f, 0.62f), Roughness = 0.9f };
        var skin = new StandardMaterial3D { AlbedoColor = new Color(0.76f, 0.58f, 0.4f), Roughness = 1 };
        var trousers = new StandardMaterial3D { AlbedoColor = new Color(0.34f, 0.3f, 0.23f), Roughness = 1 };
        var boots = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.12f, 0.14f), Roughness = 1 };
        _upperBody = new Node3D { Name = "UpperBody", Position = Vector3.Up * 0.85f }; _art.AddChild(_upperBody);
        Part(_upperBody, "Torso", new(0, 0.27f, 0), new(0.5f, 0.6f, 0.3f), shirt);
        Part(_upperBody, "Head", new(0, 0.77f, 0), new(0.32f, 0.36f, 0.32f), skin);
        Part(_upperBody, "Face", new(0, 0.77f, -0.17f), new(0.23f, 0.12f, 0.04f), skin);
        _leftArm = Limb("LeftArm", new(-0.34f, 1.35f, 0)); _rightArm = Limb("RightArm", new(0.34f, 1.35f, 0));
        _leftArm.Reparent(_upperBody); _rightArm.Reparent(_upperBody);
        foreach (var arm in new[] { _leftArm, _rightArm })
        {
            Part(arm, "Sleeve", new(0, -0.18f, 0), new(0.18f, 0.36f, 0.22f), shirt);
            Part(arm, "Hand", new(0, -0.43f, 0), new(0.15f, 0.2f, 0.17f), skin);
        }
        _leftLeg = Limb("LeftLeg", new(-0.14f, 0.85f, 0)); _rightLeg = Limb("RightLeg", new(0.14f, 0.85f, 0));
        _leftKnee = BuildLeg(_leftLeg, trousers, boots, out _leftFoot); _rightKnee = BuildLeg(_rightLeg, trousers, boots, out _rightFoot);
    }

    // =========================================================
    // Knee pivots let crouching fold the legs rather than shrink the entire character.
    private Node3D BuildLeg(Node3D leg, Material trousers, Material boots, out Node3D foot)
    {
        Part(leg, "Thigh", new(0, -0.18f, 0), new(0.21f, 0.36f, 0.25f), trousers);
        var knee = new Node3D { Name = "Knee", Position = new Vector3(0, -0.36f, 0) }; leg.AddChild(knee);
        Part(knee, "Shin", new(0, -0.18f, 0), new(0.2f, 0.36f, 0.24f), trousers);
        foot = new Node3D { Name = "Ankle", Position = new Vector3(0, -0.36f, 0) }; knee.AddChild(foot);
        Part(foot, "Boot", new(0, 0.01f, -0.06f), new(0.24f, 0.25f, 0.4f), boots);
        return knee;
    }

    // =========================================================
    // Pivot placement is cached at creation, never searched in a frame callback.
    private Node3D Limb(string name, Vector3 position)
    { var pivot = new Node3D { Name = name, Position = position }; _art.AddChild(pivot); return pivot; }

    // =========================================================
    // Art meshes contain no collision or update loops; the root capsule handles all physical movement.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size, Material material)
    { parent.AddChild(new MeshInstance3D { Name = name, Mesh = Box, Position = position, Scale = size, MaterialOverride = material }); }
}
