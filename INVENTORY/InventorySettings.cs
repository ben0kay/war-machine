// Backpack capacity and starter contents. Runtime storage never mutates this shared resource.
using Godot;

[Tool, GlobalClass]
public partial class InventorySettings : Resource
{
    [Export(PropertyHint.Range, "6,60,1")] public int Capacity { get; set; } = 24;
    [Export(PropertyHint.Range, "4,8,1")] public int Columns { get; set; } = 6;
    [Export] public Godot.Collections.Array<Resource> StartingItems { get; set; } = new();
}
