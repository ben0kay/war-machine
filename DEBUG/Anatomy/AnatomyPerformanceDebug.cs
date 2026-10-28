// Explicit work-counter snapshots for the anatomy comparison; no polling, frame callbacks or benchmark claims.
using Godot;

public static class AnatomyPerformanceDebug
{
    // =========================================================
    public static void Build(VBoxContainer rows)
    {
        rows.AddChild(new Label { Text = "Deferred anatomy: compare the same crowd and camera",
            AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var reset = new Button { Text = "Reset anatomy work counters", FocusMode = Control.FocusModeEnum.None };
        var snapshot = new Button { Text = "Print anatomy work counters", FocusMode = Control.FocusModeEnum.None };
        reset.Pressed += AnatomyHitboxes.ResetCounters;
        snapshot.Pressed += () => GD.Print(AnatomyHitboxes.Diagnostics());
        rows.AddChild(reset); rows.AddChild(snapshot);
    }
}
