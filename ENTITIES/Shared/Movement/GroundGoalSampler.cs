// Shared bounded ground/clearance/sweep sampling for wander and flee goals.
// Caller owns cached physics queries; no world scans, processing, or navigation state.
using Godot;

public static class GroundGoalSampler
{
    // =========================================================
    // Test native walkable ground, destination body clearance and unobstructed straight travel.
    public static bool TryGoal(EntityCore actor, PhysicsDirectSpaceState3D space,
        PhysicsRayQueryParameters3D ray, PhysicsShapeQueryParameters3D sweep,
        CollisionShape3D body, Vector3 desired, out Vector3 goal)
    {
        goal = Vector3.Zero;
        ray.From = new Vector3(desired.X, actor.GlobalPosition.Y + 0.8f, desired.Z);
        ray.To = new Vector3(desired.X, actor.GlobalPosition.Y - 1.2f, desired.Z);
        var ground = space.IntersectRay(ray);
        if (ground.Count == 0 || ground["normal"].AsVector3().Y < Mathf.Cos(actor.FloorMaxAngle)) return false;
        Vector3 candidate = ground["position"].AsVector3() + Vector3.Up * 0.06f;
        if (Mathf.Abs(candidate.Y - actor.GlobalPosition.Y) > 0.75f) return false;
        Transform3D dest = body.GlobalTransform;
        dest.Origin += candidate - actor.GlobalPosition;
        sweep.Transform = dest; sweep.Motion = Vector3.Zero;
        if (space.IntersectShape(sweep, 1).Count != 0) return false;
        Transform3D start = body.GlobalTransform;
        start.Origin += Vector3.Up * 0.08f;
        sweep.Transform = start; sweep.Motion = Vector3.Zero;
        if (space.IntersectShape(sweep, 1).Count != 0) return false;
        sweep.Motion = candidate - actor.GlobalPosition;
        var fraction = space.CastMotion(sweep);
        sweep.Motion = Vector3.Zero;
        if (fraction.Length < 2 || fraction[0] < 0.999f) return false;
        goal = candidate; return true;
    }

    // =========================================================
    // Four bounded tries, uniform-area radius sampling; shared between zombies and deer.
    public static bool TryWander(EntityCore actor, PhysicsDirectSpaceState3D space,
        PhysicsRayQueryParameters3D ray, PhysicsShapeQueryParameters3D sweep, CollisionShape3D body,
        RandomNumberGenerator random, Vector3 anchor, float radius, out Vector3 goal)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            float angle = random.RandfRange(0, Mathf.Tau);
            float distance = Mathf.Sqrt(random.Randf()) * Mathf.Clamp(radius, 0, 100);
            Vector3 candidate = anchor + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            if (TryGoal(actor, space, ray, sweep, body, candidate, out goal)) return true;
        }
        goal = Vector3.Zero; return false;
    }
}
