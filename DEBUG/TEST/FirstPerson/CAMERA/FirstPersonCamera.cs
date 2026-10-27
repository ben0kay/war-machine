// Eye-height perspective camera for the isolated FPS test. Input and player events replace follow polling.
using Godot;

public partial class FirstPersonCamera : Camera
{
    [Export] public FirstPersonSettings TestSettings { get; set; } = null!;
    private PlayerController _actor = null!;
    private bool _ready;

    // =========================================================
    // The shared world calls this before the capsule is initialized; use standing height for the first placement.
    public override void Initialize(PlayerController player, CameraSettings settings)
    {
        _actor = player;
        Projection = ProjectionType.Perspective; Fov = Mathf.Clamp(TestSettings.FieldOfView, 40, 110);
        Near = 0.05f; Far = 1800; Current = true; Rotation = Vector3.Zero;
        GlobalPosition = player.GlobalPosition + Vector3.Up * (1.8f - TestSettings.EyeInset);
        player.PositionChanged += OnPositionChanged; player.WeaponPoseChanged += UpdateEye;
        _ready = true; Input.MouseMode = Input.MouseModeEnum.Captured;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Escape releases mouse control; a click recaptures without firing that same click.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_ready) return;
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.Escape)
        { ReleaseMouse(); GetViewport().SetInputAsHandled(); }
        else if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left
            && Input.MouseMode != Input.MouseModeEnum.Captured)
        { Input.MouseMode = Input.MouseModeEnum.Captured; _actor.SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); }
        else if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float sensitivity = Mathf.Clamp(TestSettings.MouseSensitivity, 0.01f, 1);
            float limit = Mathf.Clamp(TestSettings.MaximumPitch, 10, 89);
            Vector3 angle = RotationDegrees;
            angle.Y = Mathf.Wrap(angle.Y - motion.Relative.X * sensitivity, -180, 180);
            angle.X = Mathf.Clamp(angle.X - motion.Relative.Y * sensitivity, -limit, limit);
            RotationDegrees = angle; NotifyViewChanged(); _actor.SetPhysicsProcess(true);
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    private void OnPositionChanged(Vector3 position) => UpdateEye();

    // =========================================================
    // Capsule height tracks the existing smooth crouch animation and standing-clearance behavior.
    private void UpdateEye()
    {
        GlobalPosition = _actor.GlobalPosition + Vector3.Up * Mathf.Max(0.5f, _actor.CapsuleHeight - TestSettings.EyeInset);
        NotifyViewChanged();
    }

    // =========================================================
    private void ReleaseMouse()
    { Input.MouseMode = Input.MouseModeEnum.Visible; _actor.SetPhysicsProcess(true); }

    // =========================================================
    // Focus loss never captures the pointer automatically when returning to the editor.
    public override void _Notification(int what)
    { if (_ready && what == NotificationApplicationFocusOut) ReleaseMouse(); }

    // =========================================================
    // No overhead follow or wheel-zoom loop is used by this test camera.
    public override void _Process(double delta) { SetProcess(false); }
    public override float ViewHeight => 4;

    // =========================================================
    public override void _ExitTree()
    {
        if (_ready && IsInstanceValid(_actor))
        { _actor.PositionChanged -= OnPositionChanged; _actor.WeaponPoseChanged -= UpdateEye; }
        Input.MouseMode = Input.MouseModeEnum.Visible; _ready = false;
    }
}
