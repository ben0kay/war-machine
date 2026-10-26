// Intentional free-flight controller for inspecting the integrated landscape, without collision.
using Godot;

public partial class ExplorationController : Node3D
{
    public event System.Action<Vector3>? ViewPositionChanged;
    [Export] public PlayerConfig Settings { get; set; } = null!;
    public bool ControlsBlocked { get; set; }
    private Camera3D _camera = null!;
    private float _pitch;

    // =========================================================
    // Cache the camera and apply the reusable player configuration.
    public override void _Ready()
    {
        SetNotifyTransform(true);
        _camera = GetNode<Camera3D>("Camera3D");
        _camera.Fov = Settings.FieldOfView; _camera.Far = Settings.FarDistance;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        SetPhysicsProcess(false);
    }

    // =========================================================
    // Mouse capture and movement events wake flight only while input is held.
    public override void _UnhandledInput(InputEvent input)
    {
        if (ControlsBlocked) return;
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * Settings.MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * Settings.MouseSensitivity, -1.5f, 1.5f);
            _camera.Rotation = new Vector3(_pitch, 0, 0);
        }
        if (input is InputEventKey || input is InputEventMouseButton) SetPhysicsProcess(Input.MouseMode == Input.MouseModeEnum.Captured && Direction() != Vector3.Zero);
    }

    // =========================================================
    // Move relative to the camera; stop callbacks when keys are released or focus is lost.
    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = Direction();
        if (ControlsBlocked || Input.MouseMode != Input.MouseModeEnum.Captured || !GetWindow().HasFocus() || direction == Vector3.Zero)
        { SetPhysicsProcess(false); return; }
        float speed = Settings.MoveSpeed * (Input.IsPhysicalKeyPressed(Key.Shift) ? Settings.BoostMultiplier : 1f);
        GlobalPosition += direction.Normalized() * speed * (float)delta;
    }

    // =========================================================
    // Notify nearby detail only when the explorer transform actually changes, including teleports.
    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged && IsInsideTree())
            ViewPositionChanged?.Invoke(GlobalPosition);
    }

    // =========================================================
    // Read physical WASD/arrows plus vertical QE/Space/Ctrl/RMB without allocating collections.
    private Vector3 Direction()
    {
        Vector3 direction = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up)) direction -= _camera.GlobalBasis.Z;
        if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down)) direction += _camera.GlobalBasis.Z;
        if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left)) direction -= GlobalBasis.X;
        if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right)) direction += GlobalBasis.X;
        if (Input.IsPhysicalKeyPressed(Key.E) || Input.IsPhysicalKeyPressed(Key.Space) || Input.IsMouseButtonPressed(MouseButton.Right)) direction += Vector3.Up;
        if (Input.IsPhysicalKeyPressed(Key.Q) || Input.IsPhysicalKeyPressed(Key.Ctrl)) direction -= Vector3.Up;
        return direction;
    }
}
