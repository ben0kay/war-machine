// Immutable shared item presentation/stack data. Runtime quantities/identity belong to BackpackStorage.
using Godot;

[Tool, GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "Item";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
    [Export(PropertyHint.Range, "1,999,1")] public int MaxStack { get; set; } = 1;
    [Export(PropertyHint.Range, "0,20,0.01")] public float UnitWeightKg { get; set; }
}
