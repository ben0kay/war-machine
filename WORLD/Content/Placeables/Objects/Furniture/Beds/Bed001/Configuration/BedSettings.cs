// Bed001 authoring dimensions. Fabric palette is selected per instance on BedMesh.
using Godot;

[Tool]
[GlobalClass]
public partial class BedSettings : Resource
{
    [ExportGroup("Bed dimensions (metres)")]
    [Export(PropertyHint.Range, "0.9,1.6,0.01")] public float Width { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "1.8,2.4,0.01")] public float Length { get; set; } = 2.10f;
}
