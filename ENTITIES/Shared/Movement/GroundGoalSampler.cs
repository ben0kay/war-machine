// Shared bounded ground/clearance/sweep sampling for wander and flee goals.
// Caller owns cached physics queries; no world scans, processing, or navigation state.
using Godot;

public static class GroundGoalSampler
{
    // =========================================================
    // Test native walkable ground, destination body clearance and unobstructed straight travel.
    public static bool TryGoal(EntityCore actor, PhysicsDirectSpaceState3D space,
        PhysicsRayQueryParameters3D ray, PhysicsShapeQueryParameters3D sweep,
        CollisionShape3D body, Vector3 desired, out Vector3 goal,
        bool strictSweep = true, float maxHeightChange = 0.75f)
    {
        goal = Vector3.Zero;
        // Zombie strict mode is unchanged. Wildlife can look further up/down a slope,
        // then rely on its real CharacterBody3D collisions to handle the local steps.
        ray.From = new Vector3(desired.X, actor.GlobalPosition.Y + (strictSweep ? 0.8f : 3f), desired.Z);
        ray.To = new Vector3(desired.X, actor.GlobalPosition.Y - (strictSweep ? 1.2f : 3f), desired.Z);
        var ground = space.IntersectRay(ray);
        if (ground.Count == 0 || ground["normal"].AsVector3().Y < Mathf.Cos(actor.FloorMaxAngle)) return false;
        Vector3 candidate = ground["position"].AsVector3() + Vector3.Up * 0.06f;
        if (Mathf.Abs(candidate.Y - actor.GlobalPosition.Y) > maxHeightChange) return false;
        Transform3D dest = body.GlobalTransform;
        dest.Origin += candidate - actor.GlobalPosition;
        sweep.Transform = dest; sweep.Motion = Vector3.Zero;
        if (space.IntersectShape(sweep, 1).Count != 0) return false;
        if (!strictSweep)
        {
            // Capsule CastMotion on uneven Terrain3D can immediately collide with the ground
            // beneath it and reject every otherwise walkable goal. A chest-height obstruction
            // ray rejects large walls/trunks; MoveAndSlide still handles actual body collision.
            ray.From = actor.GlobalPosition + Vector3.Up * 0.8f;
            ray.To = candidate + Vector3.Up * 0.8f;
            ray.CollisionMask = sweep.CollisionMask;
            bool blocked = space.IntersectRay(ray).Count > 0;
            ray.CollisionMask = 1;
            if (blocked) return false;
            goal = candidate; return true;
        }
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
        RandomNumberGenerator random, Vector3 anchor, float radius, out Vector3 goal,
        bool strictSweep = true, float maxHeightChange = 0.75f)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            float angle = random.RandfRange(0, Mathf.Tau);
            float distance = Mathf.Sqrt(random.Randf()) * Mathf.Clamp(radius, 0, 100);
            Vector3 candidate = anchor + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            if (TryGoal(actor, space, ray, sweep, body, candidate, out goal,
                strictSweep, maxHeightChange)) return true;
        }
        goal = Vector3.Zero; return false;
    }
}
