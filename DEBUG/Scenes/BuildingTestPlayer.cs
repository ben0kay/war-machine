// Minimal FPS building inspection actor; independent of the gameplay PlayerController, vitals and equipment.
using Godot;

public partial class BuildingTestPlayer : CharacterBody3D
{
    public enum TestStance { Standing, Crouching, Prone }

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "1,12,0.5")] public float WalkSpeed { get; set; } = 5f;
    [Export(PropertyHint.Range, "1,20,0.5")] public float SprintSpeed { get; set; } = 8f;
    [Export(PropertyHint.Range, "1,40,0.5")] public float Acceleration { get; set; } = 18f;
    [Export(PropertyHint.Range, "1,16,0.5")] public float JumpVelocity { get; set; } = 6f;

    [ExportGroup("Inspection camera")]
    [Export(PropertyHint.Range, "0.001,0.02,0.001")] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public bool InvertMouseY { get; set; } = true;

    [ExportGroup("Stances")]
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float CrouchSpeedMultiplier { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0.05,0.4,0.01")] public float ProneSpeedMultiplier { get; set; } = 0.17f;
    [Export(PropertyHint.Range, "0.65,1.15,0.05")] public float ProneCollisionHeight { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "1,12,0.5")] public float StanceHeightSpeed { get; set; } = 5f;

    private const float StandingHeight = 1.8f, CrouchingHeight = 1.4f, EyeInset = 0.15f;
    private Node3D _viewPivot = null!;
    private CollisionShape3D _collider = null!;
    private CapsuleShape3D _capsule = null!;
    private PhysicsShapeQueryParameters3D _standingQuery = null!, _crouchingQuery = null!;
    private TestStance _stance = TestStance.Standing, _stanceBeforeProne = TestStance.Standing;
    private bool _jumpQueued, _crouchQueued, _proneQueued;
    private float _pitch;

    // =========================================================
    // Cache a player-owned capsule, and clearance shapes for growing taller.
    // Each query excludes this CharacterBody3D, matching the gameplay stance approach.
    public override void _Ready()
    {
        _viewPivot = GetNode<Node3D>("ViewPivot");
        _collider = GetNode<CollisionShape3D>("CollisionShape3D");
        _capsule = (CapsuleShape3D)_collider.Shape.Duplicate();
        _collider.Shape = _capsule;
        var stand = (CapsuleShape3D)_capsule.Duplicate(); stand.Height = StandingHeight;
        var crouch = (CapsuleShape3D)_capsule.Duplicate(); crouch.Height = CrouchingHeight;
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        _standingQuery = new PhysicsShapeQueryParameters3D { Shape = stand,
            CollisionMask = CollisionMask, Exclude = excluded };
        _crouchingQuery = new PhysicsShapeQueryParameters3D { Shape = crouch,
            CollisionMask = CollisionMask, Exclude = excluded };
        FloorSnapLength = 0.35f;
        FloorMaxAngle = Mathf.DegToRad(48f);
        FloorStopOnSlope = true;
        _pitch = 0f;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // =========================================================
    // Handle only debug FPS controls. Stance requests are consumed in physics so
    // direct-space clearance queries never run from the input callback.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Escape || key.PhysicalKeycode == Key.Escape)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _jumpQueued = _crouchQueued = _proneQueued = false;
                GetViewport().SetInputAsHandled();
                return;
            }

            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                // Actual gameplay PlayerInput currently uses Right Ctrl for crouch
                // and Slash for prone; Left Ctrl/C are convenient debug aliases.
                bool crouch = key.PhysicalKeycode == Key.C ||
                    key.PhysicalKeycode == Key.Ctrl;
                bool prone = key.PhysicalKeycode == Key.Slash;
                if (crouch || prone)
                {
                    _crouchQueued = crouch;
                    _proneQueued = prone;
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }

        if (input is InputEventMouseButton button && button.Pressed)
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
            {
                if (button.ButtonIndex == MouseButton.Left)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    GetViewport().SetInputAsHandled();
                }
                return;
            }
            if (button.ButtonIndex == MouseButton.Right)
            {
                _jumpQueued = true;
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float sensitivity = Mathf.Clamp(MouseSensitivity, 0.001f, 0.02f);
            RotateY(-motion.ScreenRelative.X * sensitivity);
            float vertical = (InvertMouseY ? 1f : -1f) * motion.ScreenRelative.Y;
            _pitch = Mathf.Clamp(_pitch + vertical * sensitivity,
                Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
            _viewPivot.Rotation = new Vector3(_pitch, 0f, 0f);
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // Match the real player's stance states: toggle crouch; toggle prone from
    // ground; return to the previous stance when safe, otherwise try crouching.
    private void ConsumeStanceRequests()
    {
        if (_proneQueued)
        {
            if (_stance == TestStance.Prone)
            {
                if (!TrySetStance(_stanceBeforeProne) && _stanceBeforeProne == TestStance.Standing)
                    TrySetStance(TestStance.Crouching);
            }
            else if (IsOnFloor())
            {
                _stanceBeforeProne = _stance;
                TrySetStance(TestStance.Prone);
            }
        }
        else if (_crouchQueued)
        {
            if (_stance == TestStance.Prone) TrySetStance(TestStance.Crouching);
            else TrySetStance(_stance == TestStance.Crouching ? TestStance.Standing : TestStance.Crouching);
        }
        _crouchQueued = _proneQueued = false;
    }

    // =========================================================
    // Only test taller destination capsules; short stances can always be entered.
    // The destination shape is evaluated from feet level with 3cm ground separation.
    private bool TrySetStance(TestStance next)
    {
        if (next == _stance) return true;
        if (next == TestStance.Standing && !HasClearance(_standingQuery, StandingHeight)) return false;
        if (next == TestStance.Crouching && _stance == TestStance.Prone &&
            !HasClearance(_crouchingQuery, CrouchingHeight)) return false;
        _stance = next;
        return true;
    }

    // =========================================================
    private bool HasClearance(PhysicsShapeQueryParameters3D query, float height)
    {
        query.Transform = GlobalTransform * new Transform3D(Basis.Identity,
            Vector3.Up * (height * 0.5f + 0.03f));
        return GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    // =========================================================
    // Single actor physics driver. Capsule/eye transition together; no extra
    // camera processing or stance tick. Sprint and jump are standing-only.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        ConsumeStanceRequests();
        float targetHeight = _stance switch
        {
            TestStance.Crouching => CrouchingHeight,
            TestStance.Prone => Mathf.Clamp(ProneCollisionHeight, 0.65f, 1.15f),
            _ => StandingHeight
        };
        float heightStep = Mathf.Max(1f, StanceHeightSpeed) * dt;
        _capsule.Height = Mathf.MoveToward(_capsule.Height, targetHeight, heightStep);
        _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
        _viewPivot.Position = new Vector3(0f,
            Mathf.MoveToward(_viewPivot.Position.Y, Mathf.Max(0.5f, _capsule.Height - EyeInset),
                heightStep), 0f);

        bool captured = Input.MouseMode == Input.MouseModeEnum.Captured;
        float right = captured ? Axis(Key.A, Key.D) + Axis(Key.Left, Key.Right) : 0f;
        float forward = captured ? Axis(Key.S, Key.W) + Axis(Key.Down, Key.Up) : 0f;
        Vector2 movement = new(right, forward);
        if (movement.LengthSquared() > 1f) movement = movement.Normalized();

        Vector3 desired = GlobalBasis * new Vector3(movement.X, 0f, -movement.Y);
        bool sprint = captured && _stance == TestStance.Standing &&
            Input.IsPhysicalKeyPressed(Key.Shift);
        float speed = _stance switch
        {
            TestStance.Crouching => WalkSpeed * Mathf.Clamp(CrouchSpeedMultiplier, 0.1f, 1f),
            TestStance.Prone => WalkSpeed * Mathf.Clamp(ProneSpeedMultiplier, 0.05f, 0.4f),
            _ => sprint ? SprintSpeed : WalkSpeed
        };

        Vector3 velocity = Velocity;
        float rate = Mathf.Max(1f, Acceleration) * dt;
        velocity.X = Mathf.MoveToward(velocity.X, desired.X * speed, rate);
        velocity.Z = Mathf.MoveToward(velocity.Z, desired.Z * speed, rate);
        if (IsOnFloor()) velocity.Y =
            _jumpQueued && captured && _stance == TestStance.Standing ? JumpVelocity : -0.1f;
        else velocity.Y -= 9.8f * dt;
        _jumpQueued = false;
        Velocity = velocity;
        MoveAndSlide();
    }

    // =========================================================
    private static float Axis(Key negative, Key positive)
        => (Input.IsPhysicalKeyPressed(positive) ? 1f : 0f)
            - (Input.IsPhysicalKeyPressed(negative) ? 1f : 0f);

    // =========================================================
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _jumpQueued = _crouchQueued = _proneQueued = false;
        }
    }

    // =========================================================
    public override void _ExitTree() => Input.MouseMode = Input.MouseModeEnum.Visible;
}
