// Explicit in-world injury buttons. Uses the existing zombie registry only on clicks; no ticks, scans or HP/balance changes.
using Godot;

public static class ZombieInjuryDebug
{
    // =========================================================
    public static void Build(VBoxContainer rows, ZombieVisionDebug registry, PlayerController player)
    {
        ZombieController? selected = null;
        var status = new Label { Text = "Select a living zombie within 12m", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        rows.AddChild(status);
        Add(rows, "Select nearest zombie (12m)", () =>
        {
            selected = registry.FindNearest(player.GlobalPosition, 12);
            status.Text = GodotObject.IsInstanceValid(selected) ? $"Selected: {selected!.Name}" : "No living zombie within 12m";
        });
        void Injure(BodyRegion region, LimbState state)
        {
            if (!GodotObject.IsInstanceValid(selected) || !selected!.IsActive || selected.Health <= 0)
            { status.Text = "Select a living zombie first"; return; }
            selected.DebugInjure(region, state);
            status.Text = $"{selected.Name}: {selected.Mobility} (HP unchanged)";
        }
        Add(rows, "Damage left leg (limp)", () => Injure(BodyRegion.LeftLeg, LimbState.Damaged));
        Add(rows, "Sever left leg (shuffle)", () => Injure(BodyRegion.LeftLeg, LimbState.Severed));
        Add(rows, "Sever both legs (crawl)", () =>
        { Injure(BodyRegion.LeftLeg, LimbState.Severed); Injure(BodyRegion.RightLeg, LimbState.Severed); });
        Add(rows, "Disable both arms (no crawl)", () =>
        { Injure(BodyRegion.LeftArm, LimbState.Disabled); Injure(BodyRegion.RightArm, LimbState.Disabled); });
        rows.AddChild(new Label { Text = "Debug only · no healing/reset · select another for a fresh test",
            AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }

    // =========================================================
    private static void Add(VBoxContainer rows, string text, System.Action action)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        rows.AddChild(button); button.Pressed += action;
    }
}
