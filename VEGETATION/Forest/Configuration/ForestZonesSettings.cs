// Ordered inspector-editable regions; each DF2 woodland retains its own ForestSettings master.
using Godot;

[GlobalClass]
public partial class ForestZonesSettings : Resource
{
    [ExportGroup("Independent forest regions")]
    [Export] public Godot.Collections.Array<ForestSettings> Zones { get; set; } = new();
}
