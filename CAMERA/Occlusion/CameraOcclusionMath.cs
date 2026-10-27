// Small allocation-free bounds helpers for instanced scenery, independent of physics collision.
using Godot;

public static class CameraOcclusionMath
{
    // =========================================================
    // Slab test against an expanded box approximates a narrow capsule corridor conservatively.
    public static bool Intersects(Vector3 from, Vector3 to, Aabb bounds, float radius)
    {
        Vector3 low = bounds.Position - Vector3.One * radius;
        Vector3 high = bounds.End + Vector3.One * radius;
        Vector3 direction = to - from;
        float enter = 0, leave = 1;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 0.00001f)
            { if (from[axis] < low[axis] || from[axis] > high[axis]) return false; continue; }
            float a = (low[axis] - from[axis]) / direction[axis], b = (high[axis] - from[axis]) / direction[axis];
            enter = Mathf.Max(enter, Mathf.Min(a, b)); leave = Mathf.Min(leave, Mathf.Max(a, b));
            if (enter > leave) return false;
        }
        return true;
    }

    // =========================================================
    // Preserve all rotated/scaled corners when caching world bounds for a mesh or proxy.
    public static Aabb TransformBounds(Aabb bounds, Transform3D transform)
    {
        Vector3 low = transform * bounds.Position, high = low;
        for (int i = 1; i < 8; i++)
        {
            Vector3 corner = bounds.Position + new Vector3((i & 1) != 0 ? bounds.Size.X : 0,
                (i & 2) != 0 ? bounds.Size.Y : 0, (i & 4) != 0 ? bounds.Size.Z : 0);
            Vector3 point = transform * corner; low = low.Min(point); high = high.Max(point);
        }
        return new Aabb(low, high - low);
    }
}
