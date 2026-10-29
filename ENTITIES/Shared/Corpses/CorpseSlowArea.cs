// One shallow, non-solid footprint per settled corpse. Signals own modifiers; no process callbacks.
using Godot;
using System.Collections.Generic;

public partial class CorpseSlowArea : Area3D
{
    private readonly Dictionary<ulong, Node3D> _movers = new();
    private float _multiplier, _floor;
    // =========================================================
    public void Initialize(Vector3 size, float multiplier, float floor)
    {
        _multiplier = multiplier; _floor = floor;
        CollisionLayer = 0; CollisionMask = 6; Monitorable = false;
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        BodyEntered += Enter; BodyExited += Leave;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    private void Enter(Node3D body)
    {
        if (body is not ICorpseSlowable mover || !mover.CorpseSlowdownEnabled) return;
        _movers[body.GetInstanceId()] = body;
        mover.SetCorpseSlowdown(GetInstanceId(), _multiplier, _floor);
    }
    // =========================================================
    private void Leave(Node3D body)
    {
        if (body is ICorpseSlowable mover) mover.RemoveCorpseSlowdown(GetInstanceId());
        _movers.Remove(body.GetInstanceId());
    }
    // =========================================================
    // Explicit removal clears penalties immediately, not a physics-frame-late exit signal.
    public void Release()
    {
        foreach (Node3D body in _movers.Values)
            if (IsInstanceValid(body) && body is ICorpseSlowable mover) mover.RemoveCorpseSlowdown(GetInstanceId());
        _movers.Clear();
        SetDeferred(Area3D.PropertyName.Monitoring, false);
    }
    // =========================================================
    public override void _ExitTree() => Release();
}
