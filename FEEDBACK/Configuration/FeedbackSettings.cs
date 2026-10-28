// Presentation-only Inspector values; actor health and damage remain elsewhere.
using Godot;

[GlobalClass]
public partial class FeedbackSettings : Resource
{
    [ExportGroup("Health bars")]
    [Export(PropertyHint.Range, "24,180,1")] public float WorldBarWidth { get; set; } = 86f;
    [Export(PropertyHint.Range, "4,24,1")] public float WorldBarHeight { get; set; } = 9f;
    [Export(PropertyHint.Range, "0.5,5,0.05")] public float WorldAnchorHeight { get; set; } = 2.1f;
    [Export(PropertyHint.Range, "80,320,1")] public float PlayerBarWidth { get; set; } = 190f;
    [Export(PropertyHint.Range, "6,28,1")] public float PlayerBarHeight { get; set; } = 14f;
    [Export(PropertyHint.Range, "0,120,1")] public float PlayerMargin { get; set; } = 22f;
    [Export(PropertyHint.Range, "1,128,1")] public int MaxVisibleBars { get; set; } = 48;
    [Export(PropertyHint.Range, "5,60,1")] public float ProjectionUpdatesPerSecond { get; set; } = 30f;
}
