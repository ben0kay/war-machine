// Authoring-only starter entry. Never a second live copy of an equipped weapon.
using Godot;

[Tool, GlobalClass]
public partial class InventoryStartingItem : Resource
{
    [Export] public ItemDefinition? Item { get; set; }
    [Export(PropertyHint.Range, "1,999,1")] public int Quantity { get; set; } = 1;
}
