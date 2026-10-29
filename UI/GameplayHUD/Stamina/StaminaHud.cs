// PH2 read-only stamina card: bottom-right for now, event-driven, mouse-through and shared by FPS/top-down.
using Godot;

public partial class StaminaHud : Control
{
    [Export] public Vector2 CornerMargin { get; set; } = new(20, 24);
    private const float Width = 190, Height = 62;
    private PlayerStamina? _stamina;
    private Label _amount = null!, _status = null!;
    private ProgressBar _bar = null!;

    // =========================================================
    // Build once; labels/bar update only from stamina events, with no smoothing/polling callback.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; SetProcess(false); SetPhysicsProcess(false);
        var frame = new Panel { MouseFilter = MouseFilterEnum.Ignore }; AddChild(frame);
        frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(0.045f, 0.065f, 0.075f, 0.9f),
            BorderColor = new Color(0.3f, 0.43f, 0.45f, 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7 });
        AddLabel("STAMINA", new Vector2(12, 7), 11, new Color(0.65f, 0.8f, 0.8f));
        _amount = AddLabel("", new Vector2(117, 5), 14, new Color(0.9f, 0.96f, 0.95f));
        _amount.Size = new Vector2(61, 20); _amount.HorizontalAlignment = HorizontalAlignment.Right;
        _bar = new ProgressBar { Position = new Vector2(12, 29), Size = new Vector2(166, 8),
            ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_bar);
        _bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.12f, 0.18f, 0.19f) });
        _bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Colors.White });
        _status = AddLabel("", new Vector2(12, 42), 10, new Color(0.62f, 0.75f, 0.76f));
        Hide();
    }

    // =========================================================
    public void Initialize(PlayerStamina stamina)
    {
        Unbind(); _stamina = stamina; _stamina.Changed += Refresh;
        GetViewport().SizeChanged += Fit; Fit(); Refresh();
    }

    // =========================================================
    private Label AddLabel(string text, Vector2 position, int size, Color color)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        AddChild(label); return label;
    }

    // =========================================================
    // Narrow windows lift the card above the hotbar/ammo row; the margin remains easy to relocate later.
    private void Fit()
    {
        float marginX = Mathf.Max(0, CornerMargin.X), marginY = Mathf.Max(0, CornerMargin.Y);
        float width = GetViewport().GetVisibleRect().Size.X;
        if (width - Width - marginX < width * 0.5f + 248) marginY = Mathf.Max(marginY, 184);
        SetAnchorsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -Width - marginX; OffsetRight = -marginX;
        OffsetTop = -Height - marginY; OffsetBottom = -marginY;
    }

    // =========================================================
    private void Refresh()
    {
        if (!IsInstanceValid(_stamina) || !_stamina!.Enabled) { Hide(); return; }
        Show();
        _amount.Text = $"{Mathf.CeilToInt(_stamina.Current)} / {Mathf.CeilToInt(_stamina.Maximum)}";
        _bar.MaxValue = _stamina.Maximum; _bar.Value = _stamina.Current;
        _bar.Modulate = _stamina.Exhausted ? new Color(0.95f, 0.46f, 0.33f)
            : _stamina.Current < _stamina.Maximum * 0.3f ? new Color(0.95f, 0.73f, 0.35f)
            : new Color(0.41f, 0.78f, 0.69f);
        _status.Text = _stamina.IsStopped ? "INACTIVE" : _stamina.Exhausted ? "EXHAUSTED"
            : _stamina.IsExerting ? "SPRINTING" : _stamina.Current < _stamina.Maximum ? "RECOVERY" : "READY";
    }

    // =========================================================
    private void Unbind()
    {
        if (IsInstanceValid(_stamina)) _stamina!.Changed -= Refresh;
        _stamina = null; GetViewport().SizeChanged -= Fit;
    }

    // =========================================================
    public override void _ExitTree() => Unbind();
}
