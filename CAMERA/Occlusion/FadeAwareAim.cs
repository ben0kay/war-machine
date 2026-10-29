// AIM1: top-down cursor-only raycasts ignore *currently faded* paired building collision shapes.
// The shared firearm's muzzle/bullet ray remains independent and fully collidable.
using Godot;

public static class FadeAwareAim
{
    public const string VisualMeta = "aim_fade_visual";
    public const string FadedMeta = "aim_faded_by_camera";
    private const int MaxFadedHits = 24;
    private const float StepPastHit = 0.025f;

    // =========================================================
    // Structure authors pair each physical shape with its corresponding visual once during creation.
    // Multiple shapes may share one StaticBody3D; never exclude the whole body from an aim query.
    public static void Link(CollisionShape3D shape, MeshInstance3D visual)
    { shape.SetMeta(VisualMeta, visual); }

    // =========================================================
    // Physics-safe cursor ray: only perform extra queries when the last hit is a *known* faded
    // collision shape; stop normally on opaque walls, zombies, props, terrain or unknown shapes.
    // There are no per-frame callbacks or object registries, and the original query is restored.
    public static bool TryHit(PhysicsDirectSpaceState3D space, PhysicsRayQueryParameters3D query, out Vector3 position)
    {
        bool found = TryHitResult(space, query, out var hit);
        position = found ? hit["position"].AsVector3() : Vector3.Zero;
        return found;
    }

    // =========================================================
    // Full camera hit for interaction selection; use still requires an independent solid-world reach ray.
    public static bool TryHitResult(PhysicsDirectSpaceState3D space, PhysicsRayQueryParameters3D query,
        out Godot.Collections.Dictionary result, int maxFadedHits = MaxFadedHits)
    {
        Vector3 origin = query.From, end = query.To;
        Vector3 travel = end - origin;
        result = null!;
        if (travel.LengthSquared() < 0.000001f) return false;
        maxFadedHits = Mathf.Clamp(maxFadedHits, 0, MaxFadedHits);
        Vector3 step = travel.Normalized() * StepPastHit;
        for (int attempt = 0; attempt <= maxFadedHits; attempt++)
        {
            var hit = space.IntersectRay(query);
            if (hit.Count == 0) { query.From = origin; return false; }
            result = hit;
            Vector3 position = hit["position"].AsVector3();
            if (!IsFadedBuildingHit(hit) || attempt == maxFadedHits)
            { query.From = origin; return true; } // Conservative stop at the ray budget.
            Vector3 next = position + step;
            if ((end - next).Dot(step) <= 0f) { query.From = origin; return false; }
            query.From = next;
        }
        query.From = origin; return false;
    }

    // =========================================================
    // Ray hit shape owner, not just shared body: an opaque wall behind a faded wall remains a hit.
    // A missing/unpaired shape is always solid for cursor selection.
    private static bool IsFadedBuildingHit(Godot.Collections.Dictionary hit)
    {
        if (hit["collider"].AsGodotObject() is not CollisionObject3D body) return false;
        int shapeIndex = hit["shape"].AsInt32();
        uint ownerId = body.ShapeFindOwner(shapeIndex);
        if (body.ShapeOwnerGetOwner(ownerId) is not CollisionShape3D shape || !shape.HasMeta(VisualMeta)) return false;
        if (shape.GetMeta(VisualMeta).AsGodotObject() is not MeshInstance3D mesh
            || !GodotObject.IsInstanceValid(mesh) || !mesh.IsInsideTree()) return false;
        return mesh.HasMeta(FadedMeta) && mesh.GetMeta(FadedMeta).AsBool();
    }
}
