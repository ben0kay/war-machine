// AM1: compact, event-driven ammunition display beside the bottom hotbar.
// UI observes WeaponSlots and FirearmController; never owns rounds, reload timing or firing.
using Godot;

public partial class AmmoHud : Control
{
    private const float Width = 160f, Height = 66f;
    private WeaponSlots? _slots;
    private FirearmController? _weapon;
    private Label _count = null!, _status = null!;
    private Panel _frame = null!;

    // =========================================================
    // Author one lightweight noninteractive card, then sleep until weapon/ammo/resize events.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; SetProcess(false); SetPhysicsProcess(false);
        _frame = new Panel { Name = "AmmoFrame", MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_frame);
        _frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new StyleBoxFlat { BgColor = new Color(0.055f, 0.075f, 0.09f, 0.89f),
            BorderColor = new Color(0.34f, 0.44f, 0.47f, 0.42f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9, CornerRadiusBottomLeft = 9,
            CornerRadiusBottomRight = 9, ShadowColor = new Color(0, 0, 0, 0.34f), ShadowSize = 7 };
        _frame.AddThemeStyleboxOverride("panel", panel);
        AddText("AMMO", 12, new Vector2(12, 8), new Color(0.62f, 0.75f, 0.78f));
        _count = AddText("", 23, new Vector2(12, 27), new Color(0.89f, 0.95f, 0.96f));
        _status = AddText("", 10, new Vector2(80, 10), new Color(0.66f, 0.79f, 0.77f));
        _status.Size = new Vector2(69, 16); _status.HorizontalAlignment = HorizontalAlignment.Right;
        Hide();
    }

    // =========================================================
    // Slots already selected their initial gun before world HUD startup; read it immediately.
    public void Initialize(WeaponSlots slots)
    {
        Unbind();
        _slots = slots;
        _slots.SelectionChanged += OnSelectionChanged;
        GetViewport().SizeChanged += Fit;
        Fit();
        OnSelectionChanged(_slots.Current);
    }

    // =========================================================
    // Create fixed labels only once, not per shot.
    private Label AddText(string text, int fontSize, Vector2 position, Color color)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        _frame.AddChild(label);
        return label;
    }

    // =========================================================
    // Keep the panel immediately left of the centred 472px hotbar at desktop widths.
    // Narrow viewports move it just above the hotbar, to the right of the body silhouette.
    private void Fit()
    {
        float viewportWidth = GetViewport().GetVisibleRect().Size.X;
        float barLeft = viewportWidth * 0.5f - 236f;
        float x = barLeft - 12f - Width;
        float top = -84f;
        if (x < 278f)
        {
            x = Mathf.Max(8f, Mathf.Min(278f, viewportWidth - Width - 8f));
            top = -174f;
        }
        SetAnchorsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = x; OffsetRight = x + Width;
        OffsetTop = top; OffsetBottom = top + Height;
    }

    // =========================================================
    // Switch subscriptions to the selected gun only. No polling or inactive gun updates.
    private void OnSelectionChanged(FirearmController? next)
    {
        UnbindWeapon();
        if (!IsInstanceValid(next)) { Hide(); return; }
        _weapon = next;
        _weapon.AmmoChanged += Refresh;
        _weapon.ReloadStarted += Refresh;
        _weapon.ReloadCompleted += Refresh;
        _weapon.ReloadCancelled += Refresh;
        Show(); Refresh();
    }

    // =========================================================
    // Show the real remaining magazine, finite reserve or debug infinity, plus reload state.
    private void Refresh()
    {
        if (!IsInstanceValid(_weapon)) { Hide(); return; }
        var gun = _weapon!;
        string reserve = gun.HasUnlimitedReserveAmmo ? "∞" : gun.ReserveRounds.ToString();
        _count.Text = $"{gun.MagazineRounds} / {reserve}";
        _status.Text = gun.IsReloading ? "RELOADING"
            : gun.MagazineRounds == 0 ? "EMPTY"
            : gun.MagazineRounds < gun.MagazineCapacity ? "R RELOAD" : "";
        Color color = gun.IsReloading ? new Color(0.97f, 0.74f, 0.42f)
            : gun.MagazineRounds == 0 ? new Color(0.98f, 0.42f, 0.38f)
            : new Color(0.66f, 0.79f, 0.77f);
        _status.AddThemeColorOverride("font_color", color);
        _count.AddThemeColorOverride("font_color", gun.MagazineRounds == 0
            ? new Color(0.97f, 0.55f, 0.46f) : new Color(0.89f, 0.95f, 0.96f));
    }

    // =========================================================
    private void UnbindWeapon()
    {
        if (IsInstanceValid(_weapon))
        {
            _weapon!.AmmoChanged -= Refresh;
            _weapon.ReloadStarted -= Refresh;
            _weapon.ReloadCompleted -= Refresh;
            _weapon.ReloadCancelled -= Refresh;
        }
        _weapon = null;
    }

    // =========================================================
    private void Unbind()
    {
        UnbindWeapon();
        if (IsInstanceValid(_slots)) _slots!.SelectionChanged -= OnSelectionChanged;
        _slots = null;
    }

    // =========================================================
    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= Fit;
        Unbind();
    }
}
