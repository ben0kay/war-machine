// Allocation-free segment versus joint-local box. Fraction stays in [0,1], including inside and parallel rays.
using Godot;

public static class VirtualAnatomyMath
{
    // =========================================================
    public static void SegmentBox(Vector3 from, Vector3 to, Aabb box, out bool hit, out float fraction, out Vector3 normal)
    {
        hit = false; fraction = 0; normal = Vector3.Zero;
        if (!from.IsFinite() || !to.IsFinite()) return;
        Vector3 delta = to - from, min = box.Position, max = box.End;
        if (!delta.IsFinite()) return;
        float near = 0, far = 1;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(delta[axis]) < 0.0000001f)
            { if (from[axis] < min[axis] || from[axis] > max[axis]) return; continue; }
            float a = (min[axis] - from[axis]) / delta[axis], b = (max[axis] - from[axis]) / delta[axis];
            float sign = -1;
            if (a > b) { (a, b) = (b, a); sign = 1; }
            if (a > near) { near = a; normal = Vector3.Zero; normal[axis] = sign; }
            far = Mathf.Min(far, b);
            if (near > far) return;
        }
        hit = true; fraction = near;
    }
}
