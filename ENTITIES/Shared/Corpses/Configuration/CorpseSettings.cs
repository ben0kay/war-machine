// Shared bounded corpse fall, pile, aging and budget settings; no species anatomy.
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

    [ExportGroup("Pile slowdown")]
    [Export] public float PileSpeedMultiplier { get; set; } = 0.8f;
    [Export] public float MinimumPileSpeedMultiplier { get; set; } = 0.65f;
    [ExportGroup("Lifetime")]
    [Export] public float LifetimeSeconds { get; set; } = 300;
    [Export] public float SinkSeconds { get; set; } = 8;
    [Export] public float SinkDepth { get; set; } = 2.2f;
    [ExportGroup("Budgets")]
    [Export] public int MaximumCorpses { get; set; } = 96;
    [Export] public int MaximumActiveFalls { get; set; } = 8;
    [Export] public float PhysicalFallDistance { get; set; } = 45;

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
        if (!InRange(PileSpeedMultiplier, 0.65f, 1) || !InRange(MinimumPileSpeedMultiplier, 0.5f, PileSpeedMultiplier)
            || !InRange(LifetimeSeconds, 1, 3600) || !InRange(SinkSeconds, 0.1f, 30) || !InRange(SinkDepth, 0.5f, 5)
            || MaximumCorpses < 1 || MaximumCorpses > 512 || MaximumActiveFalls < 1 || MaximumActiveFalls > 32
            || MaximumActiveFalls > MaximumCorpses || !InRange(PhysicalFallDistance, 1, 500))
            throw new ArgumentException("Invalid corpse lifetime/pile/budget profile.");
    }

    // =========================================================
    private static bool InRange(float value, float low, float high) => float.IsFinite(value) && value >= low && value <= high;
}
