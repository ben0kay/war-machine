// Shared mobile-actor defaults. Species-specific movement, art and sensing belong in their own resources.
using Godot;

[GlobalClass]
public partial class EntitySettings : Resource
{
    [ExportGroup("Identity")]
    [Export] public string TypeLabel { get; set; } = "Entity";
    [ExportGroup("Lifecycle")]
    [Export] public bool StartActive { get; set; } = true;
}
