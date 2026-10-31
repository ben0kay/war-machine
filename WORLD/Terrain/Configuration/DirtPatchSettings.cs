// DTP1: configuration for non-destructive, explicitly baked organic dirt patches.
using Godot;

[Tool]
[GlobalClass]
public partial class DirtPatchSettings : Resource
{
    [ExportGroup("Preview")]
    [Export] public bool UsePreviewInGameplay { get; set; } = true;
    [ExportGroup("Distribution")]
    [Export] public int Seed { get; set; } = 73421;
    [Export(PropertyHint.Range, "100,450,10")] public float AreaHalfExtent { get; set; } = 380f;
    [Export(PropertyHint.Range, "0,300,1")] public int ClusterCount { get; set; } = 125;
    [Export(PropertyHint.Range, "1,8,1")] public int MinBlotches { get; set; } = 2;
    [Export(PropertyHint.Range, "1,8,1")] public int MaxBlotches { get; set; } = 5;
    [Export(PropertyHint.Range, "0,40,1")] public int TrailCount { get; set; } = 12;
    [ExportGroup("Appearance")]
    [Export(PropertyHint.Range, "0.25,5,0.25")] public float FeatherMetres { get; set; } = 1.7f;
    [Export(PropertyHint.Range, "0,6,0.25")] public float WarpMetres { get; set; } = 2.2f;
    [Export(PropertyHint.Range, "0.65,1,0.01")] public float MinimumNormalY { get; set; } = 0.92f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float Strength { get; set; } = 0.92f;
    [ExportGroup("Village exclusion (relative to WorldMaster village origin)")]
    [Export] public Vector2 ProtectedMin { get; set; } = new(-150f, -250f);
    [Export] public Vector2 ProtectedMax { get; set; } = new(155f, 110f);
}
