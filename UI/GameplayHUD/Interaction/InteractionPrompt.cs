// IX0 read-only contextual text. Event-driven and mouse-through, with no HUD polling.
using Godot;

public partial class InteractionPrompt : Label
{
    private PlayerInteraction? _owner;
    // =========================================================
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; HorizontalAlignment = HorizontalAlignment.Center;
        AddThemeFontSizeOverride("font_size", 14);
        AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.95f));
        AddThemeConstantOverride("shadow_offset_x", 1); AddThemeConstantOverride("shadow_offset_y", 2);
        SetAnchorsPreset(LayoutPreset.Center);
        OffsetLeft = -260; OffsetRight = 260; OffsetTop = 36; OffsetBottom = 62;
        SetProcess(false); SetPhysicsProcess(false); Hide();
    }
    // =========================================================
    public void Initialize(PlayerInteraction owner)
    {
        if (IsInstanceValid(_owner)) _owner!.PromptChanged -= Refresh;
        _owner = owner; _owner.PromptChanged += Refresh;
    }
    // =========================================================
    private void Refresh(string prompt) { Text = prompt; Visible = !string.IsNullOrEmpty(prompt); }
    // =========================================================
    public override void _ExitTree()
    { if (IsInstanceValid(_owner)) _owner!.PromptChanged -= Refresh; _owner = null; }
}
