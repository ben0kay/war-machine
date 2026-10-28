// One backpack slot: presentation and local drag/drop only. The shared storage model owns all mutations.
using Godot;

public partial class BackpackSlot : Button
{
    public InventoryPanel Panel { get; set; } = null!;
    public int Index { get; set; }
    private TextureRect _icon = null!;
    private Label _count = null!;

    // =========================================================
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64); MouseDefaultCursorShape = CursorShape.PointingHand;
        _icon = new TextureRect { MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        AddChild(_icon); _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _icon.OffsetLeft = 9; _icon.OffsetTop = 5; _icon.OffsetRight = -9; _icon.OffsetBottom = -9;
        _count = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom };
        AddChild(_count); _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); _count.OffsetRight = -5; _count.OffsetBottom = -2;
        Pressed += () => Panel.Select(Index);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    public void Refresh(BackpackStorage.Stack? stack, bool selected)
    {
        _icon.Texture = stack?.Item.Icon;
        _count.Text = stack != null && stack.Quantity > 1 ? stack.Quantity.ToString() : "";
        TooltipText = stack == null ? "Empty pocket" : $"{stack.Item.DisplayName}\n{stack.Item.Description}\n{stack.Quantity} / {stack.Item.MaxStack}";
        AddThemeStyleboxOverride("normal", selected ? Panel.SelectedStyle : Panel.SlotStyle);
    }

    // =========================================================
    public override Variant _GetDragData(Vector2 atPosition)
    {
        var stack = Panel.Storage[Index];
        if (stack == null) return default;
        Panel.Select(Index);
        var preview = new Label { Text = stack.Item.DisplayName, MouseFilter = MouseFilterEnum.Ignore };
        preview.AddThemeColorOverride("font_color", new Color(0.95f, 0.88f, 0.68f)); SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["backpack"] = (long)Panel.GetInstanceId(), ["slot"] = Index };
    }

    // =========================================================
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!Panel.IsOpen || data.VariantType != Variant.Type.Dictionary) return false;
        var payload = data.AsGodotDictionary();
        return payload.ContainsKey("backpack") && payload.ContainsKey("slot")
            && payload["backpack"].AsInt64() == (long)Panel.GetInstanceId()
            && payload["slot"].AsInt32() >= 0 && payload["slot"].AsInt32() < Panel.Storage.Capacity;
    }

    // =========================================================
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (!_CanDropData(atPosition, data)) return;
        int source = data.AsGodotDictionary()["slot"].AsInt32();
        if (Panel.Storage.MoveOrMerge(source, Index)) Panel.Select(Index);
    }
}
