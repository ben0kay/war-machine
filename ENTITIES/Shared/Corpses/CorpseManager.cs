// World-local, species-independent visual handoff. No idle processing, full-world scans or timers.
using Godot;
using System;

public partial class CorpseManager : Node
{
    private Node3D _root = null!;
    private CorpseSettings _profile = null!;
    public int SpawnedCount { get; private set; }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(Node3D root, CorpseSettings settings)
    { settings.Validate(); _root = root; _profile = (CorpseSettings)settings.Duplicate(true); }

    // =========================================================
    // Existing materials/meshes are transferred, never rebuilt or duplicated. Call on the deferred main-thread death handoff.
    public bool TrySpawn(Node3D visual, Transform3D at, ICorpsePoseAdapter pose, Vector3 direction, Vector3 velocity,
        uint seed, CorpseSettings? profile = null)
    {
        if (!IsInstanceValid(_root) || !_root.IsInsideTree() || !IsInstanceValid(visual) || visual.IsQueuedForDeletion()) return false;
        var settings = (CorpseSettings)(profile ?? _profile).Duplicate(true); settings.Validate();
        if (!settings.Enabled || !at.Origin.IsFinite() || !at.Basis.X.IsFinite() || !at.Basis.Y.IsFinite() || !at.Basis.Z.IsFinite()) return false;
        var body = new CorpseBody { Name = $"Corpse{++SpawnedCount}" };
        _root.AddChild(body);
        try { body.Initialize(visual, at, pose, direction, velocity, seed, settings); return true; }
        catch (Exception error)
        { GD.PushWarning($"Corpse handoff failed: {error.Message}"); body.QueueFree(); return false; }
    }
}
