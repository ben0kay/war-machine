// Angled camera follow, yaw orbit and bounded zoom. Changes are input-driven; settled camera sleeps.
using Godot;

public partial class Camera : Camera3D
{
    public event System.Action? SettingsChanged;
    public event System.Action? ViewChanged;
    private PlayerController _player = null!;
    private CameraSettings _settings = null!;
    private Vector3 _target;
    private float _zoomTarget;
    private bool _initialized;
    private ulong _lastMovementTick;

    // =========================================================
    // Wait for explicit test-world setup, without assuming the existing explorer or input map.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Fixed angle looks at the physics root's middle, never at its bobbing visual model.
    public virtual void Initialize(PlayerController player, CameraSettings settings)
    {
        if (_initialized) _player.PositionChanged -= OnPlayerMoved;
        _player = player; _settings = settings;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off; // Render-frame camera must not be interpolated twice.
        Projection = settings.UsePerspective ? ProjectionType.Perspective : ProjectionType.Orthogonal;
        Fov = Mathf.Clamp(settings.PerspectiveFov, 25, 90); KeepAspect = KeepAspectEnum.Height; Current = true; Near = 0.1f;
        Far = float.IsFinite(settings.FarClipDistance) ? Mathf.Clamp(settings.FarClipDistance, 64, 2048) : 256;
        settings.CameraPitchDegrees = Mathf.Clamp(settings.CameraPitchDegrees, 35, 80);
        settings.CameraYawDegrees = Mathf.Clamp(settings.CameraYawDegrees, 0, 360);
        settings.CameraDistance = ClampPerspectiveDistance(settings.CameraDistance);
        RotationDegrees = new Vector3(-settings.CameraPitchDegrees, settings.CameraYawDegrees, 0);
        _zoomTarget = ClampZoom(settings.CameraSize); Size = _zoomTarget;
        settings.CameraSize = _zoomTarget;
        _target = player.GlobalPosition;
        player.ResetPhysicsInterpolation();
        _ = player.GetGlobalTransformInterpolated(); // Prime follow-target history before movement.
        GlobalPosition = DesiredPosition();
        player.PositionChanged += OnPlayerMoved; _initialized = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        SetProcess(false);
    }

    // =========================================================
    // Camera-relative axes are projected onto the actual 3D ground plane by the player.
    private void OnPlayerMoved(Vector3 position)
    { _lastMovementTick = Engine.GetPhysicsFrames(); SetProcess(true); }

    // =========================================================
    // Mouse wheel smoothly changes orthographic size within bounded test-only settings.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || _player.GameplayInputBlocked || input is not InputEventMouseButton mouse || !mouse.Pressed) return;
        int direction = mouse.ButtonIndex == MouseButton.WheelUp ? -1 : mouse.ButtonIndex == MouseButton.WheelDown ? 1 : 0;
        if (direction == 0) return;
        SetZoom((_settings.UsePerspective ? _settings.CameraDistance : _zoomTarget) + direction * Mathf.Max(0.1f, _settings.ZoomStep));
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Tilt changes update framing immediately and preserve the current camera orbit angle.
    public void SetPitch(float degrees)
    {
        if (!_initialized) return;
        _settings.CameraPitchDegrees = Mathf.Clamp(degrees, 35, 80);
        RotationDegrees = new Vector3(-_settings.CameraPitchDegrees, _settings.CameraYawDegrees, 0);
        GlobalPosition = DesiredPosition();
        ViewChanged?.Invoke(); SettingsChanged?.Invoke();
    }

    // =========================================================
    // Orbit horizontally around the current follow point. Player-relative controls use the updated basis.
    public void SetYaw(float degrees)
    {
        if (!_initialized) return;
        float yaw = Mathf.Clamp(degrees, 0, 360);
        if (Mathf.IsEqualApprox(_settings.CameraYawDegrees, yaw)) return;
        _settings.CameraYawDegrees = yaw;
        RotationDegrees = new Vector3(-_settings.CameraPitchDegrees, yaw, 0);
        GlobalPosition = DesiredPosition();
        ViewChanged?.Invoke(); SettingsChanged?.Invoke();
    }

    // =========================================================
    // Wheel and slider share one zoom target and notify the panel only when an input changes it.
    public void SetZoom(float size)
    {
        if (!_initialized) return;
        if (_settings.UsePerspective) _settings.CameraDistance = ClampPerspectiveDistance(size);
        else { _zoomTarget = ClampZoom(size); _settings.CameraSize = _zoomTarget; }
        SetProcess(true);
        ViewChanged?.Invoke(); SettingsChanged?.Invoke();
    }

    // =========================================================
    // Match framing at the player plane when switching, while preserving the same angle and follow target.
    public void SetProjectionMode(bool perspective)
    {
        if (!_initialized || _settings.UsePerspective == perspective) return;
        if (perspective)
            _settings.CameraDistance = ClampPerspectiveDistance(_settings.CameraSize / (2 * Mathf.Tan(Mathf.DegToRad(Fov * 0.5f))));
        else { _zoomTarget = ClampZoom(ViewHeight); _settings.CameraSize = _zoomTarget; Size = _zoomTarget; }
        _settings.UsePerspective = perspective;
        Projection = perspective ? ProjectionType.Perspective : ProjectionType.Orthogonal;
        GlobalPosition = DesiredPosition(); SetProcess(true); ViewChanged?.Invoke(); SettingsChanged?.Invoke();
    }

    // =========================================================
    // FOV is vertical (Keep Height), independently adjustable from perspective camera distance.
    public void SetPerspectiveFov(float degrees)
    {
        _settings.PerspectiveFov = Mathf.Clamp(degrees, 25, 90); Fov = _settings.PerspectiveFov;
        ViewChanged?.Invoke(); SettingsChanged?.Invoke();
    }

    // Conservative orbit allowance shared by all zoom/pitch settings; smoothing lag is checked separately.
    public float MaximumRenderOffset => _initialized
        ? Mathf.Max(1, Mathf.Max(_settings.MinimumPerspectiveDistance, _settings.MaximumPerspectiveDistance)) + 0.9f : 32;

    public virtual float ViewHeight => _settings.UsePerspective
        ? 2 * _settings.CameraDistance * Mathf.Tan(Mathf.DegToRad(Fov * 0.5f)) : _settings.CameraSize;

    // =========================================================
    // Continue only until both follow and zoom settle; no constant transform/uniform assignments at rest.
    public override void _Process(double delta)
    {
        _target = _player.GetGlobalTransformInterpolated().Origin;
        float weight = 1 - Mathf.Exp(-Mathf.Max(0.1f, _settings.FollowSmoothing) * (float)delta);
        Vector3 desired = DesiredPosition();
        bool moving = GlobalPosition.DistanceSquaredTo(desired) > 0.00001f;
        bool zooming = Mathf.Abs(Size - _zoomTarget) > 0.001f;
        if (moving) GlobalPosition = GlobalPosition.Lerp(desired, weight); else if (GlobalPosition != desired) GlobalPosition = desired;
        if (zooming) Size = Mathf.Lerp(Size, _zoomTarget, weight); else if (Size != _zoomTarget) Size = _zoomTarget;
        if (moving || zooming) ViewChanged?.Invoke();
        // Finish the last physics interval before sleeping, even if this frame still sees the old target.
        if (!moving && !zooming && Engine.GetPhysicsFrames() > _lastMovementTick) SetProcess(false);
    }

    // =========================================================
    // Offset follows the camera's backwards axis so its optical axis remains centred on the player.
    private Vector3 DesiredPosition() => _target + Vector3.Up * 0.9f + GlobalBasis.Z * ClampPerspectiveDistance(_settings.CameraDistance);

    // =========================================================
    // Reversed authored limits remain safe and predictable.
    private float ClampZoom(float value) => Mathf.Clamp(value, Mathf.Max(5, Mathf.Min(_settings.MinimumZoom, _settings.MaximumZoom)),
        Mathf.Max(5, Mathf.Max(_settings.MinimumZoom, _settings.MaximumZoom)));

    // =========================================================
    // Perspective uses distance, not orthographic size; keep both bounds independently authorable.
    private float ClampPerspectiveDistance(float value) => Mathf.Clamp(value,
        Mathf.Max(1, Mathf.Min(_settings.MinimumPerspectiveDistance, _settings.MaximumPerspectiveDistance)),
        Mathf.Max(1, Mathf.Max(_settings.MinimumPerspectiveDistance, _settings.MaximumPerspectiveDistance)));

    // =========================================================
    // Derived debug cameras notify aim adapters without depending on the overhead follow implementation.
    protected void NotifyViewChanged() => ViewChanged?.Invoke();

    // =========================================================
    // Target unload cannot retain a camera subscription.
    public override void _ExitTree()
    { if (_initialized && GodotObject.IsInstanceValid(_player)) _player.PositionChanged -= OnPlayerMoved; }
}
