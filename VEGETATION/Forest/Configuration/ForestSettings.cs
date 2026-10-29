// DF0: one authored, reversible dense-forest footprint; no trees or per-frame behaviour.
using Godot;

[GlobalClass]
public partial class ForestSettings : Resource
{
    [ExportGroup("DF0 test footprint")]
    [Export] public bool Enabled { get; set; } = false;
    [Export] public bool ShowDebugOutline { get; set; } = false;
    // Absolute world X/Z coordinates, independent of the moving player spawn.
    [Export] public Vector2 CenterXZ { get; set; } = new(667f, 567f);
    [Export] public Vector2 SizeXZ { get; set; } = new(96f, 96f);
    [Export(PropertyHint.Range, "0,24,1")] public float TransitionWidth { get; set; } = 12f;
    [Export(PropertyHint.Range, "2,12,1")] public float SurveyStep { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.1,1.5,0.05")] public float MaximumSlope { get; set; } = 0.75f;
}
