// Angled orthographic follow/zoom. Player movement or scroll wakes interpolation; settled camera sleeps.
using Godot;

public partial class TopDownCamera : Camera3D
{
    public event System.Action? SettingsChanged;
    private TopDownPlayer _player = null!;
    private TopDownTestSettings _settings = null!;
    private Vector3 _target;
    private float _zoomTarget;
    private bool _initialized;

    // =========================================================
    // Wait for explicit test-world setup, without assuming the existing explorer or input map.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Fixed angle looks at the physics root's middle, never at its bobbing visual model.
    public void Initialize(TopDownPlayer player, TopDownTestSettings settings)
    {
        if (_initialized) _player.PositionChanged -= OnPlayerMoved;
        _player = player; _settings = settings;
        Projection = ProjectionType.Orthogonal; Current = true; Near = 0.1f; Far = 1800;
        settings.CameraPitchDegrees = Mathf.Clamp(settings.CameraPitchDegrees, 25, 80);
        RotationDegrees = new Vector3(-settings.CameraPitchDegrees, settings.CameraYawDegrees, 0);
        _zoomTarget = ClampZoom(settings.CameraSize); Size = _zoomTarget;
        settings.CameraSize = _zoomTarget;
        _target = player.GlobalPosition;
        GlobalPosition = DesiredPosition();
        player.PositionChanged += OnPlayerMoved; _initialized = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        SetProcess(false);
    }

    // =========================================================
    // Camera-relative axes are projected onto the actual 3D ground plane by the player.
    private void OnPlayerMoved(Vector3 position) { _target = position; SetProcess(true); }

    // =========================================================
    // Mouse wheel smoothly changes orthographic size within bounded test-only settings.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || input is not InputEventMouseButton mouse || !mouse.Pressed) return;
        int direction = mouse.ButtonIndex == MouseButton.WheelUp ? -1 : mouse.ButtonIndex == MouseButton.WheelDown ? 1 : 0;
        if (direction == 0) return;
        SetZoom(_zoomTarget + direction * Mathf.Max(0.1f, _settings.ZoomStep));
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Pitch changes keep the player centred immediately; yaw and screen-relative movement axes stay fixed.
    public void SetPitch(float degrees)
    {
        if (!_initialized) return;
        _settings.CameraPitchDegrees = Mathf.Clamp(degrees, 25, 80);
        RotationDegrees = new Vector3(-_settings.CameraPitchDegrees, _settings.CameraYawDegrees, 0);
        GlobalPosition = DesiredPosition();
        SettingsChanged?.Invoke();
    }

    // =========================================================
    // Wheel and slider share one zoom target and notify the panel only when an input changes it.
    public void SetZoom(float size)
    {
        if (!_initialized) return;
        _zoomTarget = ClampZoom(size);
        _settings.CameraSize = _zoomTarget;
        SetProcess(true);
        SettingsChanged?.Invoke();
    }

    // =========================================================
    // Continue only until both follow and zoom settle; no constant transform/uniform assignments at rest.
    public override void _Process(double delta)
    {
        float weight = 1 - Mathf.Exp(-Mathf.Max(0.1f, _settings.FollowSmoothing) * (float)delta);
        Vector3 desired = DesiredPosition();
        bool moving = GlobalPosition.DistanceSquaredTo(desired) > 0.00001f;
        bool zooming = Mathf.Abs(Size - _zoomTarget) > 0.001f;
        if (moving) GlobalPosition = GlobalPosition.Lerp(desired, weight); else if (GlobalPosition != desired) GlobalPosition = desired;
        if (zooming) Size = Mathf.Lerp(Size, _zoomTarget, weight); else if (Size != _zoomTarget) Size = _zoomTarget;
        if (!moving && !zooming) SetProcess(false);
    }

    // =========================================================
    // Offset follows the camera's backwards axis so its optical axis remains centred on the player.
    private Vector3 DesiredPosition() => _target + Vector3.Up * 0.9f + GlobalBasis.Z * Mathf.Clamp(_settings.CameraDistance, 20, 60);

    // =========================================================
    // Reversed authored limits remain safe and predictable.
    private float ClampZoom(float value) => Mathf.Clamp(value, Mathf.Max(5, Mathf.Min(_settings.MinimumZoom, _settings.MaximumZoom)),
        Mathf.Max(5, Mathf.Max(_settings.MinimumZoom, _settings.MaximumZoom)));

    // =========================================================
    // Target unload cannot retain a camera subscription.
    public override void _ExitTree()
    { if (_initialized && GodotObject.IsInstanceValid(_player)) _player.PositionChanged -= OnPlayerMoved; }
}
