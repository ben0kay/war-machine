// FPS eye camera: render-frame translation between physics snapshots, immediate mouse rotation and no idle follow loop.
using Godot;

public partial class FirstPersonCamera : Camera
{
    [Export] public FirstPersonSettings TestSettings { get; set; } = null!;
    public bool DebugControlsOpen { get; set; }
    private PlayerController _actor = null!;
    private bool _ready;
    private Vector3 _previousEye, _currentEye;
    private ulong _sampleTick;

    // =========================================================
    // The shared world calls this before the capsule is initialized; use standing height for the first placement.
    public override void Initialize(PlayerController player, CameraSettings settings)
    {
        _actor = player;
        Projection = ProjectionType.Perspective; Fov = Mathf.Clamp(TestSettings.FieldOfView, 40, 110);
        Near = 0.05f; Far = 1800; Current = true; Rotation = Vector3.Zero;
        GlobalPosition = player.GlobalPosition + Vector3.Up * (1.8f - TestSettings.EyeInset);
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off; // We interpolate translation only, never mouse rotation.
        _previousEye = _currentEye = GlobalPosition;
        _sampleTick = Engine.GetPhysicsFrames();
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
            && Input.MouseMode != Input.MouseModeEnum.Captured && !DebugControlsOpen)
        { Input.MouseMode = Input.MouseModeEnum.Captured; _actor.SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); }
        else if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float sensitivity = Mathf.Clamp(TestSettings.MouseSensitivity, 0.01f, 1);
            float limit = Mathf.Clamp(TestSettings.MaximumPitch, 10, 89);
            Vector3 angle = RotationDegrees;
            angle.Y = Mathf.Wrap(angle.Y - motion.ScreenRelative.X * sensitivity, -180, 180);
            // Positive pitch looks upward; inverted mode makes mouse-up look down.
            float verticalDelta = (TestSettings.InvertMouseY ? 1f : -1f) * motion.ScreenRelative.Y;
            angle.X = Mathf.Clamp(angle.X + verticalDelta * sensitivity, -limit, limit);
            RotationDegrees = angle; NotifyViewChanged();
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // Called after player physics, not from two separate movement/stance signals. No exponential follow lag.
    public void RecordPhysicsPose(float capsuleHeight)
    {
        Vector3 eye = _actor.GlobalPosition + Vector3.Up * Mathf.Max(0.5f, capsuleHeight - TestSettings.EyeInset);
        _previousEye = _currentEye; _currentEye = eye; _sampleTick = Engine.GetPhysicsFrames();
        float resetDistance = Mathf.Max(1, TestSettings.TeleportResetDistance);
        if (!TestSettings.InterpolateTranslation || _previousEye.DistanceSquaredTo(eye) > resetDistance * resetDistance)
        { _previousEye = eye; SetEye(eye); SetProcess(false); return; }
        if (_previousEye == _currentEye) { SetEye(eye); SetProcess(false); }
        else SetProcess(true);
    }

    // =========================================================
    private void SetEye(Vector3 eye)
    { if (GlobalPosition != eye) { GlobalPosition = eye; NotifyViewChanged(); } }

    // =========================================================
    private void ReleaseMouse()
    { Input.MouseMode = Input.MouseModeEnum.Visible; _actor.SetPhysicsProcess(true); }

    // =========================================================
    // Focus loss never captures the pointer automatically when returning to the editor.
    public override void _Notification(int what)
    { if (_ready && what == NotificationApplicationFocusOut) ReleaseMouse(); }

    // =========================================================
    // Render between fixed-tick eye positions. Finish the final tick exactly, then become dormant.
    public override void _Process(double delta)
    {
        if (!_ready) { SetProcess(false); return; }
        if (!TestSettings.InterpolateTranslation || Engine.GetPhysicsFrames() > _sampleTick)
        { SetEye(_currentEye); SetProcess(false); return; }
        SetEye(_previousEye.Lerp(_currentEye, (float)Engine.GetPhysicsInterpolationFraction()));
    }
    public override float ViewHeight => 4;

    // =========================================================
    public override void _ExitTree()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible; _ready = false;
    }
}
