// F1 preview distances only, independent of actual IX interaction and physics tuning.
using Godot;

[GlobalClass]
public partial class PlaceableSpawnSettings : Resource
{
    [Export(PropertyHint.Range, "1.5,7,0.1")] public float SpawnDistance { get; set; } = 3.0f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float BookHoverHeight { get; set; } = 0.95f;
}
