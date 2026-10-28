// Always-on debug FPS label: half-second sampling, smooth green-yellow-orange-red feedback; no frame polling.
using Godot;

public partial class FpsCounter : Label
{
    [Export] public DebugConfig Settings { get; set; } = null!;
    private Timer _timer = null!;
    private int _lastFps = -1;

    // =========================================================
    // Set up one slow timer rather than redrawing or formatting a label every frame.
    public override void _Ready()
    {
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
    // Only assign text when the displayed integer actually changes.
    private void UpdateFps()
    {
        int fps = (int)Engine.GetFramesPerSecond();
        if (fps == _lastFps) return;
        _lastFps = fps;
        Text = $"FPS: {fps}";
        AddThemeColorOverride("font_color", GetFpsColor(fps));
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
