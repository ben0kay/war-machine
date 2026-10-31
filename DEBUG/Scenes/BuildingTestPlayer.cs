// Minimal FPS-only building inspection actor; never uses the gameplay PlayerController, vitals or equipment.
using Godot;

public partial class BuildingTestPlayer : CharacterBody3D
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "1,12,0.5")] public float WalkSpeed { get; set; } = 5f;
    [Export(PropertyHint.Range, "1,20,0.5")] public float SprintSpeed { get; set; } = 8f;
    [Export(PropertyHint.Range, "1,40,0.5")] public float Acceleration { get; set; } = 18f;
    [Export(PropertyHint.Range, "1,16,0.5")] public float JumpVelocity { get; set; } = 6f;
    [Export(PropertyHint.Range, "0.001,0.02,0.001")] public float MouseSensitivity { get; set; } = 0.0025f;

    private Node3D _viewPivot = null!;
    private bool _jumpQueued;
    private float _pitch;

    // =========================================================
    // A single CharacterBody3D/capsule with its own eye camera. This scene needs no WorldController.
    public override void _Ready()
    {
        _viewPivot = GetNode<Node3D>("ViewPivot");
        FloorSnapLength = 0.35f;
        FloorMaxAngle = Mathf.DegToRad(48f);
        FloorStopOnSlope = true;
        _pitch = 0f;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // =========================================================
    // Mouse look is immediate. Escape releases the pointer; click recaptures it.
    // Right mouse jumps, matching War Machine's usual FPS test controls.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo
            && (key.Keycode == Key.Escape || key.PhysicalKeycode == Key.Escape))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _jumpQueued = false;
            GetViewport().SetInputAsHandled();
            return;
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
            _pitch = Mathf.Clamp(_pitch - motion.ScreenRelative.Y * sensitivity,
                Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
            _viewPivot.Rotation = new Vector3(_pitch, 0f, 0f);
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // One intentionally active player physics step, with cheap native stair/capsule collision.
    // There are no per-building actors, ticks, searches or global gameplay dependencies.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        bool captured = Input.MouseMode == Input.MouseModeEnum.Captured;
        float right = captured ? Axis(Key.A, Key.D) + Axis(Key.Left, Key.Right) : 0f;
        float forward = captured ? Axis(Key.S, Key.W) + Axis(Key.Down, Key.Up) : 0f;
        Vector2 movement = new(right, forward);
        if (movement.LengthSquared() > 1f) movement = movement.Normalized();

        Vector3 desired = GlobalBasis * new Vector3(movement.X, 0f, -movement.Y);
        float speed = captured && Input.IsPhysicalKeyPressed(Key.Shift) ? SprintSpeed : WalkSpeed;
        Vector3 velocity = Velocity;
        float rate = Mathf.Max(1f, Acceleration) * dt;
        velocity.X = Mathf.MoveToward(velocity.X, desired.X * speed, rate);
        velocity.Z = Mathf.MoveToward(velocity.Z, desired.Z * speed, rate);
        if (IsOnFloor())
        {
            velocity.Y = _jumpQueued && captured ? JumpVelocity : -0.1f;
        }
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
            _jumpQueued = false;
        }
    }

    // =========================================================
    public override void _ExitTree() => Input.MouseMode = Input.MouseModeEnum.Visible;
}
