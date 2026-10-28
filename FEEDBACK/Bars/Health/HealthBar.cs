// Pure presentation widget: read-only health fraction supplied by the shared overlay.
using Godot;

public partial class HealthBar : Control
{
    private ProgressBar _progress = null!;

    // =========================================================
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _progress = GetNode<ProgressBar>("Progress");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    public void SetHealth(float current, float maximum)
    {
        float ratio = float.IsFinite(current) && float.IsFinite(maximum) && maximum > 0f
            ? Mathf.Clamp(current / maximum, 0f, 1f) : 0f;
        _progress.Value = ratio * 100f;
    }
}
