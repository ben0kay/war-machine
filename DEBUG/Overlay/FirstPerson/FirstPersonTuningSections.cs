// Small event-driven disclosure/scroll helpers shared by the FPS debug panels.
using Godot;

public static class FirstPersonTuningSections
{
    // =========================================================
    // Size the panel from expanded content, with scrolling only beyond the viewport cap.
    public static VBoxContainer AddScroll(VBoxContainer parent, float maximumHeight)
    {
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(350, 80),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rows.AddThemeConstantOverride("separation", 8);
        parent.AddChild(scroll); scroll.AddChild(rows);
        rows.MinimumSizeChanged += () => scroll.CustomMinimumSize = new Vector2(350,
            Mathf.Clamp(rows.GetCombinedMinimumSize().Y, 80, maximumHeight));
        return rows;
    }

    // =========================================================
    // Visibility changes only on clicks; preserve expanded state when F8/F9 panels are reopened.
    public static VBoxContainer Add(VBoxContainer parent, string title)
    {
        var button = new Button { Text = "+ " + title, ToggleMode = true,
            FocusMode = Control.FocusModeEnum.None, Alignment = HorizontalAlignment.Left };
        var body = new VBoxContainer { Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 6);
        parent.AddChild(button); parent.AddChild(body);
        button.Toggled += open => { body.Visible = open; button.Text = (open ? "− " : "+ ") + title; };
        return body;
    }
}
