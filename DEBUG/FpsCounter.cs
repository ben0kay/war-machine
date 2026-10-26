// Displays FPS at a throttled cadence. Hiding/disabling the label stops its timer; no frame polling.
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
        _lastFps = fps; Text = $"FPS: {fps}";
    }
}
