// PERF0: simple allocation-free Godot Control graph of measured frame intervals.
using Godot;

public partial class PerformanceFrameGraph : Control
{
    private static readonly float[] Guides = { 16.67f, 33.33f, 50f };
    private float[]? _history;
    private int _next, _count;

    // =========================================================
    public void ShowHistory(float[] history, int next, int count)
    {
        _history = history; _next = next; _count = count;
        QueueRedraw();
    }

    // =========================================================
    public override void _Draw()
    {
        var bounds = new Rect2(Vector2.Zero, Size);
        DrawRect(bounds, new Color(0.07f, 0.11f, 0.13f, 1f));
        if (_history == null || _count < 2) return;

        float maxMs = 50f;
        for (int i = 0; i < _count; i++)
        {
            int at = (_next - _count + i + _history.Length) % _history.Length;
            maxMs = Mathf.Max(maxMs, _history[at] * 1.15f);
        }
        float h = Mathf.Max(1f, Size.Y - 4f), width = Mathf.Max(1f, Size.X - 4f);
        foreach (float guide in Guides)
        {
            float y = 2f + h * (1f - Mathf.Clamp(guide / maxMs, 0f, 1f));
            DrawLine(new Vector2(2f, y), new Vector2(2f + width, y),
                new Color(0.55f, 0.63f, 0.65f, 0.25f), 1f);
        }
        Vector2 previous = Vector2.Zero;
        for (int i = 0; i < _count; i++)
        {
            int at = (_next - _count + i + _history.Length) % _history.Length;
            float ms = _history[at];
            var point = new Vector2(2f + i * width / Mathf.Max(1f, _count - 1),
                2f + h * (1f - Mathf.Clamp(ms / maxMs, 0f, 1f)));
            if (i > 0) DrawLine(previous, point, ms >= 40f ? new Color(1f, 0.46f, 0.35f) :
                new Color(0.38f, 0.89f, 0.68f), 1.8f, true);
            previous = point;
        }
    }
}
