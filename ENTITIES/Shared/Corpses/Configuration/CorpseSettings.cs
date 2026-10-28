// Shared ZD0 fall profile. No species anatomy, corpse TTL, pile collision or movement penalties.
using Godot;
using System;

[GlobalClass]
public partial class CorpseSettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector3 ProxySize { get; set; } = new(0.65f, 1.75f, 0.42f);
    [Export] public float PivotHeight { get; set; } = 0.95f;
    [Export] public float MassKg { get; set; } = 65;
    [Export] public float MaximumFallSeconds { get; set; } = 2.5f;
    [Export] public float MinimumFallSeconds { get; set; } = 0.8f;
    [Export] public float StableSeconds { get; set; } = 0.25f;
    [Export] public float StableSpeed { get; set; } = 0.15f;
    [Export] public float StableAngularSpeed { get; set; } = 0.3f;
    [Export] public float PushSpeed { get; set; } = 0.8f;
    [Export] public float InitialAngularSpeed { get; set; } = 2.7f;
    [Export] public float LinearDamping { get; set; } = 0.8f;
    [Export] public float AngularDamping { get; set; } = 1;
    [Export] public float MaximumTravel { get; set; } = 5;

    // =========================================================
    // Fail before visual handoff; malformed authoring never creates a permanent runaway physics body.
    public void Validate()
    {
        if (!ProxySize.IsFinite() || ProxySize.X < 0.2f || ProxySize.Y < 0.4f || ProxySize.Z < 0.2f
            || ProxySize.Length() > 4 || !InRange(PivotHeight, 0.2f, 2) || !InRange(MassKg, 1, 500)
            || !InRange(MaximumFallSeconds, 1, 3) || !InRange(MinimumFallSeconds, 0.2f, MaximumFallSeconds)
            || !InRange(StableSeconds, 0.1f, 1) || !InRange(StableSpeed, 0.01f, 1)
            || !InRange(StableAngularSpeed, 0.01f, 2) || !InRange(PushSpeed, 0, 2)
            || !InRange(InitialAngularSpeed, 1, 5) || !InRange(LinearDamping, 0, 5)
            || !InRange(AngularDamping, 0, 5) || !InRange(MaximumTravel, 2, 10))
            throw new ArgumentException("Invalid shared corpse fall profile.");
    }

    // =========================================================
    private static bool InRange(float value, float low, float high) => float.IsFinite(value) && value >= low && value <= high;
}
