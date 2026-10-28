// FPS eye camera: render-frame translation between physics snapshots, immediate mouse rotation and no idle follow loop.
using Godot;

public partial class FirstPersonCamera : Camera
{
    [Export] public FirstPersonSettings TestSettings { get; set; } = null!;
    private PlayerController _actor = null!;
    private bool _ready;
    private Vector3 _previousEye, _currentEye;
    private ulong _sampleTick;
    private float _lookPitchDegrees;

    // =========================================================
    // The shared world calls this before the capsule is initialized; use standing height for the first placement.
    public override void Initialize(PlayerController player, CameraSettings settings)
    {
        _actor = player;
        Projection = ProjectionType.Perspective; Fov = Mathf.Clamp(TestSettings.FieldOfView, 40, 110);
        Near = 0.05f; Far = 1800; Current = true; Rotation = Vector3.Zero;
        _lookPitchDegrees = 0f;
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
        if (!_ready || _actor.GameplayInputBlocked) return;
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.Escape)
        { ReleaseMouse(); GetViewport().SetInputAsHandled(); }
        else if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left
            && Input.MouseMode != Input.MouseModeEnum.Captured && GetViewport().GuiGetHoveredControl() == null)
        { Input.MouseMode = Input.MouseModeEnum.Captured; _actor.SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); }
        else if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float sensitivity = Mathf.Clamp(TestSettings.MouseSensitivity, 0.01f, 1);
            float limit = Mathf.Clamp(TestSettings.MaximumPitch, 10, 89);
            Vector3 angle = RotationDegrees;
            angle.Y = Mathf.Wrap(angle.Y - motion.ScreenRelative.X * sensitivity, -180, 180);
            // Positive pitch looks upward; inverted mode makes mouse-up look down.
            float verticalDelta = (TestSettings.InvertMouseY ? 1f : -1f) * motion.ScreenRelative.Y;
            // Mouse movement is the permanent aim, including any recoil-induced rise.
            // The weapon adds one-way pitch; firing stops do not automatically lower the view.
            _lookPitchDegrees = Mathf.Clamp(_lookPitchDegrees + verticalDelta * sensitivity, -limit, limit);
            angle.X = _lookPitchDegrees;
            RotationDegrees = angle; NotifyViewChanged();
            GetViewport().SetInputAsHandled();
        }
    }

    // =========================================================
    // FPS-only one-way physical camera kick. This advances the mouse-look orientation itself,
    // not a temporary offset that springs back on release, Q shove or weapon switching.
    // The applied amount is clamped by the existing pitch bound; excess is discarded.
    public void ApplyWeaponRecoilKick(float degrees)
    {
        if (!_ready || !float.IsFinite(degrees) || degrees <= 0f) return;
        float limit = Mathf.Clamp(TestSettings.MaximumPitch, 10f, 89f);
        float next = Mathf.Clamp(_lookPitchDegrees + degrees, -limit, limit);
        if (Mathf.IsEqualApprox(next, _lookPitchDegrees)) return;
        _lookPitchDegrees = next;
        Vector3 angle = RotationDegrees;
        angle.X = next;
        RotationDegrees = angle; NotifyViewChanged();
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
    // FPS follows at eye level, with a small allowance for translation interpolation; it has no overhead orbit.
    public override float MaximumRenderOffset => 4;
    public override float ViewHeight => 4;

    // =========================================================
    public override void _ExitTree()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible; _ready = false;
    }
}
