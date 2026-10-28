// Conservative conversion from the fog's metric to native camera/AABB-centre visibility distances.
using Godot;
using System;

public static class WorldRenderEnvelope
{
    public const string AuthoredEndMetadata = "world_authored_range_end";
    public const string ExemptMetadata = "world_render_exempt";

    // =========================================================
    public static float Safe(float value, float fallback, float low, float high)
        => float.IsFinite(value) ? Mathf.Clamp(value, low, high) : fallback;

    // =========================================================
    // A transformed AABB contains every mesh point. Padding also protects small shader-wind displacements.
    public static Aabb WorldBounds(GeometryInstance3D geometry, float padding)
    {
        Aabb local = geometry.CustomAabb.Size != Vector3.Zero ? geometry.CustomAabb : geometry.GetAabb();
        Vector3 low = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 high = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        { Vector3 point = geometry.GlobalTransform * local.GetEndpoint(i); low = low.Min(point); high = high.Max(point); }
        float extra = Mathf.Max(0, geometry.ExtraCullMargin) + padding;
        return new Aabb(low - Vector3.One * extra, high - low + Vector3.One * (2 * extra));
    }

    // =========================================================
    // Any point inside the player-XZ fog circle must keep its entire containing instance within this native range.
    public static float End(Aabb bounds, Vector3 camera, Vector3 player, float opaqueRadius, bool cameraMetric, float buffer)
    {
        Vector3 center = bounds.GetCenter(), half = bounds.Size * 0.5f;
        double radius = half.Length();
        if (cameraMetric) return (float)(opaqueRadius + radius + buffer);
        double horizontal = new Vector2(camera.X - player.X, camera.Z - player.Z).Length();
        double footprint = new Vector2(half.X, half.Z).Length();
        double reach = opaqueRadius + horizontal + footprint;
        double vertical = (double)center.Y - camera.Y;
        return (float)(Math.Sqrt(reach * reach + vertical * vertical) + buffer);
    }

    // =========================================================
    public static float Cap(float authoredEnd, float policyEnd)
        => authoredEnd > 0 ? Mathf.Min(authoredEnd, policyEnd) : policyEnd;
}
