// Bounded broad-candidate refinement shared by firearms and melee cover rays. A broad hit never delivers damage.
using Godot;

public readonly struct AnatomyRayHit
{
    public readonly Node? Collider;
    public readonly Vector3 Position, Normal;
    public readonly int ShapeIndex;
    // =========================================================
    public AnatomyRayHit(Node? collider, Vector3 position, Vector3 normal, int shapeIndex)
    { Collider = collider; Position = position; Normal = normal; ShapeIndex = shapeIndex; }
}

public sealed class AnatomyRaycaster
{
    private readonly Godot.Collections.Array<Rid> _exclude = new();
    public int LastQueryCount { get; private set; }
    public static long NativeQueries { get; private set; }
    public static long BudgetStops { get; private set; }

    // =========================================================
    // Cast from the original origin, excluding only examined broad areas. This handles overlapping areas and gaps.
    // A nearest virtual hit clips subsequent queries, but intervening real walls/bodies still have priority.
    public bool TryHit(PhysicsDirectSpaceState3D space, PhysicsRayQueryParameters3D query,
        out AnatomyRayHit result, int maximumQueries = 32)
    {
        result = default; LastQueryCount = 0;
        Vector3 from = query.From, to = query.To, travel = to - from;
        float lengthSquared = travel.LengthSquared();
        if (!from.IsFinite() || !to.IsFinite() || !travel.IsFinite() || !float.IsFinite(lengthSquared) || lengthSquared < 0.00000001f) return false;
        var originalExclude = query.Exclude;
        bool inside = query.HitFromInside;
        bool refined = false;
        float best = float.PositiveInfinity;
        AnatomyRayHit precise = default;
        Vector3 lastBroadPosition = from;
        try
        {
            query.HitFromInside = true;
            for (int attempt = 0; attempt < Mathf.Clamp(maximumQueries, 1, 32); attempt++)
            {
                if (refined) query.Exclude = _exclude;
                query.To = best > 0.00001f && best < 1 ? from.Lerp(to, best) : to;
                var native = space.IntersectRay(query); LastQueryCount++; NativeQueries++;
                if (native.Count == 0) { result = precise; return float.IsFinite(best); }
                Node? collider = native["collider"].AsGodotObject() as Node;
                Vector3 position = native["position"].AsVector3();
                if (collider is not AnatomyHitboxes anatomy || anatomy.RequiresPoseSync)
                {
                    float t = (position - from).Dot(travel) / lengthSquared;
                    result = t <= best + 0.000001f
                        ? new AnatomyRayHit(collider, position, native["normal"].AsVector3(), native["shape"].AsInt32()) : precise;
                    return true;
                }
                lastBroadPosition = position;
                if (anatomy.TryRay(from, to, out float fraction, out Vector3 normal, out int part) && fraction < best)
                { best = fraction; precise = new AnatomyRayHit(anatomy, from.Lerp(to, fraction), normal, part); }
                if (!refined)
                {
                    _exclude.Clear(); foreach (Rid rid in originalExclude) _exclude.Add(rid);
                    refined = true;
                }
                _exclude.Add(anatomy.GetRid());
            }
            // Never accept an unchecked virtual hit if the native query budget runs out before cover is resolved.
            BudgetStops++; result = new AnatomyRayHit(null, lastBroadPosition, Vector3.Zero, -1); return true;
        }
        finally
        {
            if (refined) query.Exclude = originalExclude;
            query.To = to; query.HitFromInside = inside;
        }
    }

    // =========================================================
    public static void ResetCounters() { NativeQueries = BudgetStops = 0; }
}
