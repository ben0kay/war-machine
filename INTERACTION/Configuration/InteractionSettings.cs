// IX0 targeting defaults only; no speculative prop carrying, inventory, door or save configuration.
using Godot;

[GlobalClass]
public partial class InteractionSettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Key FocusKey { get; set; } = Key.E;
    [Export(PropertyHint.Range, "0.5,4,0.1")] public float ReachMeters { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float TargetIntervalSeconds { get; set; } = 0.15f;
    // World, actors, trunks and reserved interaction layer 10; exclude camera-fade areas and corpse pile contacts.
    [Export(PropertyHint.Layers3DPhysics)] public uint QueryMask { get; set; } = 543;
    [ExportGroup("IX0 validation target")]
    [Export] public bool SpawnTestSwitch { get; set; } = true;
}
