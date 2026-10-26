// Optional nonvisual cone sensor; degrees are full spread, with a bounded LOS budget.
using Godot;

[GlobalClass]
public partial class ScanSensorSettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public float Range { get; set; } = 45;
    [Export] public float FullAngleDegrees { get; set; } = 35;
    [Export] public float IntervalSeconds { get; set; } = 0.2f;
    [Export] public bool RequireLineOfSight { get; set; } = true;
    [Export] public int RaysPerTick { get; set; } = 2;
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 1;
}
