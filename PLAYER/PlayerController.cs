// Active 1.8m player: physical walking and one visual gait driver, dormant when grounded/idle.
using Godot;

public partial class PlayerController : CharacterBody3D
{
    public event System.Action<Vector3>? PositionChanged;
    private PlayerSettings _settings = null!;
    private Node3D _art = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Node3D _leftFoot = null!, _rightFoot = null!;
    private Node3D _leftKnee = null!, _rightKnee = null!, _upperBody = null!;
    private CollisionShape3D _collider = null!;
    private CapsuleShape3D _capsule = null!;
    private PhysicsShapeQueryParameters3D _standingQuery = null!;
    public bool IsCrouching { get; private set; }
    public event System.Action<bool>? StanceChanged;
    public Marker3D WeaponMount { get; private set; } = null!;
    public Vector3 WeaponProbeOrigin => _upperBody.GlobalPosition + _settings.WeaponProbeOffset;
    public event System.Action? WeaponPoseChanged;
    private bool _weaponPose;
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
    public virtual void Initialize(PlayerSettings settings, Camera3D camera)
    {
        _settings = settings;
        _screenRight = camera.GlobalBasis.X; _screenRight.Y = 0; _screenRight = _screenRight.Normalized();
        _screenForward = -camera.GlobalBasis.Z; _screenForward.Y = 0; _screenForward = _screenForward.Normalized();
        FloorSnapLength = 0.5f; FloorMaxAngle = Mathf.DegToRad(46);
        if (!_initialized)
        {
            var mesh = new PlayerMeshBuilder(this, _settings);
            _art = mesh.Art; _upperBody = mesh.UpperBody;
            _leftArm = mesh.LeftArm; _rightArm = mesh.RightArm;
            _leftLeg = mesh.LeftLeg; _rightLeg = mesh.RightLeg;
            _leftKnee = mesh.LeftKnee; _rightKnee = mesh.RightKnee;
            _leftFoot = mesh.LeftFoot; _rightFoot = mesh.RightFoot; WeaponMount = mesh.WeaponMount;
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
        if (IsMovementKey(key.PhysicalKeycode))
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
        Vector2 keys = GetWindow().HasFocus() ? ReadMovementInput().Normalized() : Vector2.Zero;
        Vector3 direction = GetMovementDirection(keys);
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
            if (!_weaponPose) _art.Rotation = new Vector3(0, Mathf.LerpAngle(_art.Rotation.Y, yaw, 1 - Mathf.Exp(-14 * dt)), 0);
            _phase += dt * Mathf.Tau * Mathf.Max(0.1f, _settings.GaitRate) * actualSpeed / Mathf.Max(0.1f, _settings.WalkSpeed);
            Pose(Mathf.Sin(_phase) * 0.5f, Mathf.Abs(Mathf.Sin(_phase * 2)) * 0.025f);
        }
        else Pose(0, 0);
        if (_crouch != previousCrouch) WeaponPoseChanged?.Invoke();
        if (before.DistanceSquaredTo(GlobalPosition) > 0.0000001f) PositionChanged?.Invoke(GlobalPosition);
        if (IsOnFloor() && keys == Vector2.Zero && horizontal.LengthSquared() < 0.0001f && _crouch == (IsCrouching ? 1 : 0))
        { Velocity = Vector3.Zero; _phase = 0; Pose(0, 0); SetPhysicsProcess(false); }
    }

    // =========================================================
    // Small input hooks let isolated test views reuse collision, crouch and movement without copying the controller.
    protected virtual bool IsMovementKey(Key key) => key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right || key == Key.Shift;
    protected virtual Vector2 ReadMovementInput() => new(
        (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
        (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0));
    protected virtual Vector3 GetMovementDirection(Vector2 keys) => (_screenRight * keys.X + _screenForward * keys.Y).Normalized();
    protected Node3D VisualRoot => _art;
    public float CapsuleHeight => _capsule.Height;

    // =========================================================
    // The actor owns its holding pose and socket; equipment owns everything about firing.
    public void SetWeaponPose(bool enabled) { _weaponPose = enabled; if (_initialized) Pose(0, 0); }

    // =========================================================
    // Face the cursor while walking/crouching; pitch only the held socket, independent of leg gait.
    public void SetWeaponAim(Vector3 target)
    {
        Vector3 horizontal = target - GlobalPosition; horizontal.Y = 0;
        if (horizontal.LengthSquared() < 0.01f) return;
        _art.Rotation = new Vector3(0, Mathf.Atan2(-horizontal.X, -horizontal.Z), 0);
        Vector3 direction = target - WeaponMount.GlobalPosition;
        if (direction.LengthSquared() > 0.0001f && Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) < 0.99f)
            WeaponMount.GlobalBasis = Basis.LookingAt(direction, Vector3.Up);
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
        if (_weaponPose) { _leftArm.RotationDegrees = _settings.LeftWeaponArmDegrees; _rightArm.RotationDegrees = _settings.RightWeaponArmDegrees; }
        _leftLeg.Rotation = new Vector3(stride + 1.15f * _crouch, 0, 0);
        _rightLeg.Rotation = new Vector3(-stride + 1.15f * _crouch, 0, 0);
        _leftKnee.Rotation = new Vector3(-2.3f * _crouch, 0, 0);
        _rightKnee.Rotation = new Vector3(-2.3f * _crouch, 0, 0);
        _leftFoot.Rotation = new Vector3(-_leftLeg.Rotation.X - _leftKnee.Rotation.X, 0, 0);
        _rightFoot.Rotation = new Vector3(-_rightLeg.Rotation.X - _rightKnee.Rotation.X, 0, 0);
    }

}
