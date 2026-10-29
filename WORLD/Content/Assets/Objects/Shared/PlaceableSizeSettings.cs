// Shared authoring scale for imported GLB placeables; one master resource per asset type.
using Godot;

[Tool]
[GlobalClass]
public partial class PlaceableSizeSettings : Resource
{
    [ExportGroup("Model size")]
    // Native proportions at (1,1,1); X=width, Y=height, Z=length/depth.
    [Export] public Vector3 SizeMultiplier { get; set; } = Vector3.One;

    [ExportGroup("Visual rendering (SOC1A)")]
    // Int-backed Inspector dropdown: F1 only interprets exported C# enum variants
    // on visual scene roots; this stays a master setting, not a palette variant.
    // 0 preserves imported cast-shadow settings; 1 disables; 2 forces normal shadow casting.
    [Export(PropertyHint.Enum, "Inherit,Disabled,Enabled")]
    public int ShadowMode { get; set; } = 0;
}
