// Always-visible performance HUD: sampled FPS, visible primitives and player world/motion coordinates; no frame polling.
using Godot;

public partial class FpsCounter : Label
{
    [Export] public DebugConfig Settings { get; set; } = null!;
    private Timer _timer = null!;
    private Label? _triangles, _coordinates, _movementSpeed;
    private PlayerController? _player;
    private int _lastFps = -1, _lastPrimitives = -1;

    // =========================================================
    // Set up one slow timer rather than redrawing or formatting a label every frame.
    public override void _Ready()
    {
        _triangles = GetNodeOrNull<Label>("Triangles");
        _coordinates = GetNodeOrNull<Label>("Coordinates");
        _movementSpeed = GetNodeOrNull<Label>("MovementSpeed");
        // Shared World and inherited FPS both keep the player at this path.
        // Other scenes with just FPS/TRIS continue to work without these optional labels.
        if (_coordinates != null || _movementSpeed != null)
            _player = GetNodeOrNull<PlayerController>("../../../WorldActorsRoot/PersistentEntitiesRoot/Player");
        _timer = new Timer { WaitTime = Mathf.Clamp(Settings.FpsUpdateSeconds, 0.1, 5.0) };
        AddChild(_timer); _timer.Timeout += UpdateFps;
        VisibilityChanged += RefreshVisibility;
        ApplySettings();
    }

    // =========================================================
    // Explicit reconfiguration supports debug UI changes without polling settings.
    public void ApplySettings()
    {
        _timer.WaitTime = Mathf.Clamp(Settings.FpsUpdateSeconds, 0.1, 5.0);
        Visible = Settings.ShowFps;
        RefreshVisibility();
    }

    // =========================================================
    // A hidden overlay performs no timer callbacks or label updates.
    private void RefreshVisibility()
    {
        if (!IsVisibleInTree()) { _timer.Stop(); return; }
        UpdateFps(); _timer.Start();
    }

    // =========================================================
    // One timer owns both readouts. Do not let unchanged FPS suppress triangle updates.
    private void UpdateFps()
    {
        int fps = (int)Engine.GetFramesPerSecond();
        if (fps != _lastFps)
        {
            _lastFps = fps;
            Text = $"FPS: {fps}";
            AddThemeColorOverride("font_color", GetFpsColor(fps));
        }

        if (_triangles != null)
        {
            // Visible 3D primitives, excluding shadow passes. Unchanged count
            // must NOT prevent position/speed from updating during movement.
            int primitives = RenderingServer.ViewportGetRenderInfo(
                GetViewport().GetViewportRid(),
                RenderingServer.ViewportRenderInfoType.Visible,
                RenderingServer.ViewportRenderInfo.PrimitivesInFrame);
            if (primitives != _lastPrimitives)
            {
                _lastPrimitives = primitives;
                _triangles.Text = $"TRIS: {primitives:N0}";
            }
        }
        UpdatePlayerMovement();
    }

    // =========================================================
    // Sample the actor's current world coordinates and horizontal physics velocity.
    // One shared 0.5s timer; no separate motion polling or physics subscriptions.
    private void UpdatePlayerMovement()
    {
        if (_player == null || !IsInstanceValid(_player)) return;
        Vector3 position = _player.GlobalPosition;
        if (_coordinates != null)
        {
            string value = $"POS: X {position.X:F1}  Y {position.Y:F1}  Z {position.Z:F1}";
            if (_coordinates.Text != value) _coordinates.Text = value;
        }
        if (_movementSpeed != null)
        {
            Vector3 velocity = _player.Velocity;
            float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
            string value = $"SPEED: {horizontalSpeed:F1} m/s";
            if (_movementSpeed.Text != value) _movementSpeed.Text = value;
        }
    }

    // =========================================================
    // Continuous colour transition: green at >=60, yellow at 40, orange at 30, red at <=20.
    // Theme font colour changes only when the sampled FPS integer changes.
    private static Color GetFpsColor(int fps)
    {
        Color red = new(0.98f, 0.21f, 0.18f);
        Color yellow = new(1.00f, 0.91f, 0.20f);
        Color green = new(0.25f, 0.98f, 0.35f);
        if (fps <= 20) return red;
        if (fps >= 60) return green;
        return fps < 40 ? red.Lerp(yellow, (fps - 20f) / 20f)
            : yellow.Lerp(green, (fps - 40f) / 20f);
    }
}
