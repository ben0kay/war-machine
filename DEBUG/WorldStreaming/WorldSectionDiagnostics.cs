// WS0 display only: top-centre section heading plus F3 read-only X/Z map. No continuous update callbacks.
using Godot;

public partial class WorldSectionDiagnostics : Control
{
    public WorldManifest Manifest { get; private set; } = null!;
    private PlayerController _player = null!;
    private Label _heading = null!;
    private SectionMap _map = null!;
    private SectionCoordinate _cell;
    private bool _initialized, _inside;

    // =========================================================
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore;
        SetProcess(false); SetPhysicsProcess(false);
        _heading = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_heading); _heading.SetAnchorsPreset(LayoutPreset.TopWide);
        _heading.OffsetLeft = 0; _heading.OffsetRight = 0; _heading.OffsetTop = 12; _heading.OffsetBottom = 40;
        _heading.AddThemeFontSizeOverride("font_size", 18);
        _heading.AddThemeColorOverride("font_shadow_color", Colors.Black); _heading.AddThemeConstantOverride("shadow_outline_size", 4);
        _map = new SectionMap { MouseFilter = MouseFilterEnum.Ignore }; AddChild(_map);
        _map.AnchorLeft = _map.AnchorRight = 0.5f;
        _map.OffsetLeft = -150; _map.OffsetRight = 150; _map.OffsetTop = 52; _map.OffsetBottom = 414;
        _map.Hide();
    }

    // =========================================================
    public void Initialize(PlayerController player, WorldManifest manifest, WorldSectionSettings settings)
    {
        if (_initialized) return;
        _player = player; Manifest = manifest; _initialized = true;
        _heading.Visible = settings.ShowHeading; _map.Visible = settings.ShowGrid;
        OnMoved(player.GlobalPosition); Refresh();
        // Hidden diagnostics retain only F3 input handling; movement notifications are detached entirely.
        UpdateSubscription();
    }

    private bool _subscribed, _viewportSubscribed;
    // =========================================================
    private void UpdateSubscription()
    {
        bool needed = _heading.Visible || _map.Visible;
        if (_map.Visible != _viewportSubscribed)
        {
            if (_map.Visible) { GetViewport().SizeChanged += FitMap; FitMap(); }
            else GetViewport().SizeChanged -= FitMap;
            _viewportSubscribed = _map.Visible;
        }
        if (needed == _subscribed) return;
        if (needed) { _player.PositionChanged += OnMoved; OnMoved(_player.GlobalPosition); }
        else _player.PositionChanged -= OnMoved;
        _subscribed = needed;
    }

    // =========================================================
    // Scale the optional map to the window on explicit resize only, keeping its centre under the heading.
    private void FitMap()
    {
        Vector2 viewport = GetViewportRect().Size;
        float scale = Mathf.Clamp(Mathf.Min((viewport.X - 24) / 300, (viewport.Y - 64) / 362), 0.1f, 1);
        _map.PivotOffset = new Vector2(150, 0); _map.Scale = Vector2.One * scale;
    }

    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || input is not InputEventKey key || !key.Pressed || key.Echo || key.PhysicalKeycode != Key.F3) return;
        _map.Visible = !_map.Visible; UpdateSubscription();
        if (_map.Visible) Refresh();
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Player motion signals are already supplied by gameplay; only boundary/status changes update the display.
    private void OnMoved(Vector3 position)
    {
        var point = new Vector2(position.X, position.Z); SectionCoordinate next = Manifest.SectionAt(point);
        bool inside = Manifest.Contains(point);
        if (next == _cell && inside == _inside) return;
        _cell = next; _inside = inside; Refresh();
    }

    // =========================================================
    private void Refresh()
    {
        _heading.Text = $"Section ({_cell.X}, {_cell.Z})" + (_inside ? "" : " · outside world bounds");
        if (_map.Visible) _map.UpdateMap(Manifest, _cell);
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (_subscribed && IsInstanceValid(_player)) _player.PositionChanged -= OnMoved;
        if (_viewportSubscribed) GetViewport().SizeChanged -= FitMap;
        _subscribed = _viewportSubscribed = _initialized = false;
    }
}
