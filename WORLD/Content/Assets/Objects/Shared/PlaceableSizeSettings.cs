// Shared authoring scale for imported GLB placeables; one master resource per asset type.
using Godot;

[Tool]
[GlobalClass]
public partial class PlaceableSizeSettings : Resource
{
    [ExportGroup("Model size")]
    // Native proportions at (1,1,1); X=width, Y=height, Z=length/depth.
    [Export] public Vector3 SizeMultiplier { get; set; } = Vector3.One;
}
