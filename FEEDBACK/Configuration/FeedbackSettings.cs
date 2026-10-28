// Presentation-only Inspector values; actor health and damage remain elsewhere.
using Godot;

[GlobalClass]
public partial class FeedbackSettings : Resource
{
    [ExportGroup("Health bars")]
    [Export(PropertyHint.Range, "24,180,1")] public float WorldBarWidth { get; set; } = 86f;
    [Export(PropertyHint.Range, "4,24,1")] public float WorldBarHeight { get; set; } = 9f;
    [Export(PropertyHint.Range, "0.5,5,0.05")] public float WorldAnchorHeight { get; set; } = 2.1f;
    [Export(PropertyHint.Range, "1,128,1")] public int MaxVisibleBars { get; set; } = 48;
    [Export(PropertyHint.Range, "5,60,1")] public float ProjectionUpdatesPerSecond { get; set; } = 30f;
    [ExportGroup("Player body silhouette")]
    [Export(PropertyHint.Range, "8,60,1")] public float BodyHudMargin { get; set; } = 22;
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float BodyHealthyThreshold { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float BodySevereThreshold { get; set; } = 0.4f;
    [Export] public Color BodyHealthyColor { get; set; } = new(0.42f, 0.55f, 0.46f);
    [Export] public Color BodyInjuredColor { get; set; } = new(0.9f, 0.65f, 0.23f);
    [Export] public Color BodySevereColor { get; set; } = new(0.95f, 0.29f, 0.22f);
    [Export] public Color BodyDisabledColor { get; set; } = new(0.37f, 0.14f, 0.16f);
}
