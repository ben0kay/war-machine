// Geometry only: a reusable cached blocker query, at most two rays for one explicitly supplied target.
using Godot;

public sealed class VisibilityResolver
{
    private readonly PhysicsRayQueryParameters3D _ray;

    // =========================================================
    // Movement masks remain unchanged. Exclude both actor bodies even if the chosen blocker mask includes them.
    public VisibilityResolver(CollisionObject3D observer, CollisionObject3D target, uint blockerMask)
    {
        _ray = new PhysicsRayQueryParameters3D { CollisionMask = blockerMask, CollideWithAreas = false,
            HitFromInside = true, Exclude = new Godot.Collections.Array<Rid> { observer.GetRid(), target.GetRid() } };
    }

    // =========================================================
    // Caller performs range/cone gates on its physics cadence; an unobstructed head OR torso is sufficient.
    public bool CanSee(PhysicsDirectSpaceState3D space, Vector3 eye, IPerceptionTarget target)
        => Clear(space, eye, target.PerceptionHead) || Clear(space, eye, target.PerceptionTorso);

    // =========================================================
    private bool Clear(PhysicsDirectSpaceState3D space, Vector3 from, Vector3 to)
    {
        if (from.DistanceSquaredTo(to) < 0.000001f) return true;
        _ray.From = from; _ray.To = to;
        return space.IntersectRay(_ray).Count == 0;
    }
}
