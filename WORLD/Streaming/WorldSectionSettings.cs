// Logical section/diagnostic defaults only. WS0 never loads or unloads world content.
using Godot;

[GlobalClass]
public partial class WorldSectionSettings : Resource
{
    [Export] public string WorldId { get; set; } = "war-machine-main";
    [Export] public Vector2 Origin { get; set; } = Vector2.Zero;
    [Export(PropertyHint.Range, "32,512,1")] public float SectionSize { get; set; } = 128;
    [Export] public Rect2 LogicalBounds { get; set; } = new(0, 0, 1024, 1024);
    [Export] public bool ShowHeading { get; set; } = true;
    [Export] public bool ShowGrid { get; set; }
}
