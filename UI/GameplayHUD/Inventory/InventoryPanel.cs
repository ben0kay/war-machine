// Live-world backpack window. UI observes per-player storage; opening never pauses the scene tree.
using Godot;
using System.Collections.Generic;

public partial class InventoryPanel : Control
{
    public BackpackStorage Storage { get; private set; } = null!;
    public bool IsOpen => Visible;
    public StyleBoxFlat SlotStyle { get; private set; } = null!;
    public StyleBoxFlat SelectedStyle { get; private set; } = null!;
    private PlayerController _player = null!;
    private PanelContainer _pack = null!;
    private GridContainer _grid = null!;
    private ScrollContainer _scroll = null!;
    private Label _capacity = null!, _name = null!, _description = null!, _facts = null!;
    private TextureRect _detailIcon = null!;
    private readonly List<BackpackSlot> _slots = new();
    private bool _initialized, _restoreCapture, _previousBlock;
    private int _selected = -1, _columns;

    // =========================================================
    public override void _Ready()
    { SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop; Hide(); SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Shared world composition binds exactly once after actor/equipment initialization, in either view.
    public void Initialize(PlayerController player, PlayerBackpack backpack)
    {
        if (_initialized) return;
        _player = player; Storage = backpack.Storage; _columns = Mathf.Clamp(backpack.Settings.Columns, 4, 8);
        Build(); _initialized = true; Storage.Changed += OnStorageChanged;
        GetViewport().SizeChanged += Fit; Fit(); Refresh();
    }

    // =========================================================
    // I toggles, Escape closes. Ordinary mouse events remain available to Godot GUI drag/drop.
    public override void _Input(InputEvent input)
    {
        if (!_initialized || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.PhysicalKeycode == Key.I) { if (IsOpen) Close(); else Open(); GetViewport().SetInputAsHandled(); }
        else if (IsOpen && key.PhysicalKeycode == Key.Escape) { Close(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    public override void _UnhandledInput(InputEvent input)
    { if (IsOpen) GetViewport().SetInputAsHandled(); }

    // =========================================================
    private void Open()
    {
        _previousBlock = _player.GameplayInputBlocked;
        _restoreCapture = Input.MouseMode == Input.MouseModeEnum.Captured;
        _player.SetGameplayInputBlocked(true); Input.MouseMode = Input.MouseModeEnum.Visible;
        Show(); Fit(); Refresh(); _slots[0].GrabFocus();
    }

    // =========================================================
    // Close button/I/Escape restores capture only if the user originally had it and still has focus.
    private void Close(bool restore = true)
    {
        if (!IsOpen) return;
        Hide(); GetViewport().GuiReleaseFocus();
        if (IsInstanceValid(_player)) _player.SetGameplayInputBlocked(_previousBlock);
        Input.MouseMode = restore && _restoreCapture && !_player.IsDead && GetWindow().HasFocus()
            ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        _restoreCapture = false;
    }

    // =========================================================
    public override void _Notification(int what)
    { if (what == NotificationApplicationFocusOut) _restoreCapture = false; }

    // =========================================================
    // Closed inventory does not redraw; opening explicitly reads the current model.
    private void OnStorageChanged() { if (IsOpen) Refresh(); }

    // =========================================================
    public void Select(int slot) { _selected = slot; Refresh(); }

    // =========================================================
    private void Refresh()
    {
        int used = 0, count = 0; float weight = 0;
        for (int i = 0; i < Storage.Capacity; i++)
        {
            var stack = Storage[i]; _slots[i].Refresh(stack, i == _selected);
            if (stack == null) continue;
            used++; count += stack.Quantity;
            if (float.IsFinite(stack.Item.UnitWeightKg)) weight += Mathf.Max(0, stack.Item.UnitWeightKg) * stack.Quantity;
        }
        _capacity.Text = $"{used} / {Storage.Capacity} pockets   ·   {count} items   ·   {weight:0.00} kg";
        var item = Storage[_selected];
        _detailIcon.Texture = item?.Item.Icon; _name.Text = item?.Item.DisplayName ?? "Your field pack";
        _description.Text = item?.Item.Description ?? "Select an item to inspect it. Drag items between pockets to rearrange or combine matching stacks.";
        _facts.Text = item == null ? "" : $"Quantity  {item.Quantity} / {Mathf.Clamp(item.Item.MaxStack, 1, 999)}\nStack weight  {Mathf.Max(0, item.Item.UnitWeightKg) * item.Quantity:0.00} kg";
    }

    // =========================================================
    private void Fit()
    {
        if (_pack == null) return;
        Vector2 viewport = GetViewportRect().Size;
        _pack.Size = new Vector2(Mathf.Max(300, Mathf.Min(760, viewport.X - 24)), Mathf.Max(300, Mathf.Min(550, viewport.Y - 24)));
        _pack.Position = (viewport - _pack.Size) * 0.5f;
        _grid.Columns = viewport.X < 760 ? 4 : _columns;
    }

    // =========================================================
    // Canvas, stitched-looking pockets and straps use native controls/styles; item icons are reusable SVGs.
    private void Build()
    {
        var shade = new ColorRect { Color = new Color(0.015f, 0.02f, 0.02f, 0.48f), MouseFilter = MouseFilterEnum.Stop };
        AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _pack = new PanelContainer { Name = "FieldPack" }; AddChild(_pack);
        _pack.AddThemeStyleboxOverride("panel", Style(new Color(0.17f, 0.19f, 0.145f), new Color(0.40f, 0.43f, 0.31f), 18, 2));
        var margin = new MarginContainer(); _pack.AddChild(margin);
        foreach (string edge in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 22);
        var rows = new VBoxContainer(); rows.AddThemeConstantOverride("separation", 14); margin.AddChild(rows);
        var straps = new HBoxContainer(); rows.AddChild(straps);
        foreach (string text in new[] { "▰  FIELD GEAR", "" })
        { var strap = Label(text, 12); strap.Modulate = new Color(0.69f, 0.71f, 0.54f); straps.AddChild(strap); }
        var header = new HBoxContainer(); rows.AddChild(header);
        var title = Label("BACKPACK", 27); title.SizeFlagsHorizontal = SizeFlags.ExpandFill; header.AddChild(title);
        var close = new Button { Text = "Close  [I]", MouseDefaultCursorShape = CursorShape.PointingHand }; header.AddChild(close); close.Pressed += () => Close();
        var live = Label("The world keeps moving while you pack.", 13); live.Modulate = new Color(0.77f, 0.76f, 0.63f); rows.AddChild(live);
        var content = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 18); rows.AddChild(content);
        _scroll = new ScrollContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        content.AddChild(_scroll); _grid = new GridContainer { Columns = _columns };
        _grid.AddThemeConstantOverride("h_separation", 7); _grid.AddThemeConstantOverride("v_separation", 7); _scroll.AddChild(_grid);
        SlotStyle = Style(new Color(0.10f, 0.12f, 0.09f), new Color(0.36f, 0.39f, 0.28f), 7, 1);
        SelectedStyle = Style(new Color(0.23f, 0.26f, 0.17f), new Color(0.83f, 0.74f, 0.43f), 7, 2);
        var hover = Style(new Color(0.20f, 0.23f, 0.16f), new Color(0.65f, 0.65f, 0.45f), 7, 1);
        for (int i = 0; i < Storage.Capacity; i++)
        {
            var slot = new BackpackSlot { Name = $"Pocket{i + 1:00}", Panel = this, Index = i };
            _grid.AddChild(slot); slot.AddThemeStyleboxOverride("hover", hover); slot.AddThemeStyleboxOverride("focus", SelectedStyle);
            _slots.Add(slot);
        }
        var details = new VBoxContainer { CustomMinimumSize = new Vector2(175, 0) }; details.AddThemeConstantOverride("separation", 12); content.AddChild(details);
        _detailIcon = new TextureRect { CustomMinimumSize = new Vector2(0, 80), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered }; details.AddChild(_detailIcon);
        _name = Label("", 18); _name.AutowrapMode = TextServer.AutowrapMode.WordSmart; details.AddChild(_name);
        _description = Label("", 14); _description.AutowrapMode = TextServer.AutowrapMode.WordSmart; details.AddChild(_description);
        _facts = Label("", 13); _facts.Modulate = new Color(0.73f, 0.76f, 0.64f); details.AddChild(_facts);
        _capacity = Label("", 14); rows.AddChild(_capacity);
        var hint = Label("Drag to rearrange or merge   ·   I / Esc to close", 12); hint.Modulate = new Color(0.73f, 0.76f, 0.64f); rows.AddChild(hint);
    }

    // =========================================================
    private static Label Label(string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color(0.91f, 0.89f, 0.78f)); return label;
    }

    // =========================================================
    private static StyleBoxFlat Style(Color fill, Color border, int radius, int width)
    {
        return new StyleBoxFlat { BgColor = fill, BorderColor = border, BorderWidthLeft = width, BorderWidthTop = width,
            BorderWidthRight = width, BorderWidthBottom = width, CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius };
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        Close(false); Storage.Changed -= OnStorageChanged; GetViewport().SizeChanged -= Fit; _initialized = false;
    }
}
