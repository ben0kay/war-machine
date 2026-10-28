// Read-only six-region silhouette. Redraws on condition/death events, with no polling or mouse interception.
using Godot;

public partial class BodyConditionWidget : Control
{
    private static readonly string[] Names = { "Head", "Torso", "L arm", "R arm", "L leg", "R leg" };
    // Front-facing anatomy: the player's LeftArm/LeftLeg appear on the right of the silhouette.
    private static readonly Vector2[][] Shapes = {
        System.Array.Empty<Vector2>(),
        new[] { new Vector2(55, 65), new Vector2(79, 65), new Vector2(84, 99), new Vector2(77, 112), new Vector2(57, 112), new Vector2(50, 99) },
        new[] { new Vector2(83, 66), new Vector2(91, 70), new Vector2(102, 110), new Vector2(93, 114), new Vector2(82, 87) },
        new[] { new Vector2(51, 66), new Vector2(43, 70), new Vector2(32, 110), new Vector2(41, 114), new Vector2(52, 87) },
        new[] { new Vector2(70, 116), new Vector2(80, 116), new Vector2(86, 161), new Vector2(74, 161) },
        new[] { new Vector2(64, 116), new Vector2(54, 116), new Vector2(48, 161), new Vector2(60, 161) }
    };
    private readonly float[] _fractions = new float[BodyCondition.RegionCount];
    private readonly Label[] _labels = new Label[BodyCondition.RegionCount];
    private PlayerVitals? _vitals;
    private FeedbackSettings _settings = null!;
    private Label _state = null!;
    private StyleBoxFlat _background = null!;
    private float _healthy, _severe;
    private bool _dead;

    // =========================================================
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; SetProcess(false); SetPhysicsProcess(false);
        _background = new StyleBoxFlat { BgColor = new Color(0.035f, 0.05f, 0.055f, 0.82f),
            BorderColor = new Color(0.3f, 0.38f, 0.38f, 0.5f), BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9,
            CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9 };
        AddText("BODY CONDITION", new Vector2(14, 11), 12);
        for (int i = 0; i < _labels.Length; i++) _labels[i] = AddText("", new Vector2(118, 39 + i * 20), 12);
        _state = AddText("", new Vector2(14, 174), 11);
        Hide();
    }

    // =========================================================
    public void Initialize(PlayerVitals vitals, FeedbackSettings settings)
    {
        Unbind(); _vitals = vitals; _settings = (FeedbackSettings)settings.Duplicate(true);
        _healthy = float.IsFinite(_settings.BodyHealthyThreshold) ? Mathf.Clamp(_settings.BodyHealthyThreshold, 0.1f, 1) : 0.75f;
        _severe = float.IsFinite(_settings.BodySevereThreshold) ? Mathf.Clamp(_settings.BodySevereThreshold, 0.01f, _healthy) : 0.4f;
        float margin = float.IsFinite(_settings.BodyHudMargin) ? Mathf.Clamp(_settings.BodyHudMargin, 8, 60) : 22;
        // Keep above the hotbar even on narrow windows; anchors handle resize without a callback.
        SetAnchorsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = margin; OffsetRight = margin + 244; OffsetTop = -306; OffsetBottom = -104;
        for (int i = 0; i < BodyCondition.RegionCount; i++) ReadRegion((BodyRegion)i);
        _dead = vitals.IsDead; UpdateState(); QueueRedraw(); Show();
        vitals.Body.ConditionChanged += OnConditionChanged; vitals.Died += OnDied;
    }

    // =========================================================
    private Label AddText(string text, Vector2 position, int size)
    {
        var label = new Label { Text = text, Position = position, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(0.76f, 0.82f, 0.8f)); AddChild(label); return label;
    }

    // =========================================================
    private void ReadRegion(BodyRegion region)
    {
        float maximum = _vitals!.Body.Maximum(region);
        _fractions[(int)region] = maximum > 0 ? Mathf.Clamp(_vitals.Body.Current(region) / maximum, 0, 1) : 0;
        float fraction = _fractions[(int)region];
        string severity = fraction <= 0 ? "× Disabled" : fraction < _severe ? "!! Severe" : fraction < _healthy ? "! Hurt" : fraction < 1 ? "Minor" : "OK";
        _labels[(int)region].Text = $"{Names[(int)region]}  {severity}";
        _labels[(int)region].Modulate = RegionColor(fraction);
    }

    // =========================================================
    private void OnConditionChanged(BodyRegion region, float current, float maximum)
    { ReadRegion(region); UpdateState(); QueueRedraw(); }

    // =========================================================
    private void OnDied()
    { _dead = true; UpdateState(); QueueRedraw(); }

    // =========================================================
    private void UpdateState()
    {
        bool injured = false;
        foreach (float fraction in _fractions) injured |= fraction < 1;
        _state.Text = _dead ? "DEAD" : injured ? "Injured" : "No injuries";
    }

    // =========================================================
    private Color RegionColor(float fraction) => fraction <= 0 ? _settings.BodyDisabledColor
        : fraction < _severe ? _settings.BodySevereColor : fraction < _healthy ? _settings.BodyInjuredColor : _settings.BodyHealthyColor;

    // =========================================================
    public override void _Draw()
    {
        if (_vitals == null) return;
        DrawStyleBox(_background, new Rect2(Vector2.Zero, Size));
        for (int i = 0; i < _fractions.Length; i++)
        {
            Color color = RegionColor(_fractions[i]); if (_dead) color.A *= 0.45f;
            if (i == 0) DrawCircle(new Vector2(67, 49), 12, color);
            else DrawColoredPolygon(Shapes[i], color);
        }
    }

    // =========================================================
    private void Unbind()
    {
        if (IsInstanceValid(_vitals))
        { if (IsInstanceValid(_vitals!.Body)) _vitals.Body.ConditionChanged -= OnConditionChanged; _vitals.Died -= OnDied; }
        _vitals = null;
    }

    // =========================================================
    public override void _ExitTree() => Unbind();
}
