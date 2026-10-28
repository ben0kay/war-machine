// GH1: static world-space building/canopy footprints become a tiny soft mask.
// Store value-only geometry at world start; streaming may unload/reload visual prefabs safely.
using Godot;
using System.Collections.Generic;

public sealed class HazeExclusions
{
    private readonly struct Zone
    {
        public readonly Rect2 Local, World;
        public readonly Transform2D Inverse;
        public Zone(Rect2 local, Rect2 world, Transform2D inverse)
        { Local = local; World = world; Inverse = inverse; }
    }

    private readonly List<Zone> _zones = new();
    public int Count => _zones.Count;

    // =========================================================
    // Capture ONLY explicit occupied bounds, not the site, road, car park or mesh-wide bounds.
    // Works for building floors, open pump canopies and authored covered passages.
    public static HazeExclusions Capture(Node3D structures)
    {
        var result = new HazeExclusions();
        result.Visit(structures);
        return result;
    }

    // =========================================================
    // Authoring zones are building-local AABBs; sampling in transformed local XZ preserves yaw rotation.
    private void Visit(Node node)
    {
        if (node is Node3D spatial && node.HasMeta("structure_occupied_bounds"))
        {
            Aabb area = node.GetMeta("structure_occupied_bounds").AsAabb();
            if (area.Size.X > 0 && area.Size.Z > 0)
            {
                Transform3D t = spatial.GlobalTransform;
                var onGround = new Transform2D(new Vector2(t.Basis.X.X, t.Basis.X.Z),
                    new Vector2(t.Basis.Z.X, t.Basis.Z.Z), new Vector2(t.Origin.X, t.Origin.Z));
                float determinant = onGround.Determinant();
                if (Mathf.Abs(determinant) > 0.00001f)
                {
                    Rect2 local = new(new Vector2(area.Position.X, area.Position.Z),
                        new Vector2(area.Size.X, area.Size.Z));
                    Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
                    Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
                    foreach (Vector2 corner in new[] {
                        local.Position, new Vector2(local.End.X, local.Position.Y),
                        new Vector2(local.Position.X, local.End.Y), local.End
                    })
                    {
                        Vector2 world = onGround * corner;
                        min = min.Min(world); max = max.Max(world);
                    }
                    _zones.Add(new Zone(local, new Rect2(min, max - min), onGround.AffineInverse()));
                }
            }
        }
        foreach (Node child in node.GetChildren()) Visit(child);
    }

    // =========================================================
    // Rebuild a local R8 opacity image only at terrain tile crossings/authoring updates.
    // White = normal haze; black = none. A one-metre mask and linear GPU sampling soften edges.
    public void Paint(Image mask, Vector2 worldOrigin, float metresPerPixel, float edgeFade)
    {
        mask.Fill(Colors.White);
        if (_zones.Count == 0) return;
        int width = mask.GetWidth(), height = mask.GetHeight();
        Vector2 extent = worldOrigin + new Vector2((width - 1) * metresPerPixel,
            (height - 1) * metresPerPixel);
        Rect2 tile = new(worldOrigin, extent - worldOrigin);
        edgeFade = Mathf.Clamp(edgeFade, 0.1f, 5f);
        foreach (Zone zone in _zones)
        {
            Rect2 bounds = zone.World.Grow(edgeFade + metresPerPixel);
            if (!bounds.Intersects(tile)) continue;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((bounds.Position.X - worldOrigin.X) / metresPerPixel));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt((bounds.End.X - worldOrigin.X) / metresPerPixel));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((bounds.Position.Y - worldOrigin.Y) / metresPerPixel));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt((bounds.End.Y - worldOrigin.Y) / metresPerPixel));
            Vector2 half = zone.Local.Size * 0.5f, middle = zone.Local.Position + half;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 at = new(worldOrigin.X + x * metresPerPixel, worldOrigin.Y + y * metresPerPixel);
                Vector2 local = zone.Inverse * at;
                Vector2 delta = (local - middle).Abs() - half;
                Vector2 outside = new(Mathf.Max(delta.X, 0), Mathf.Max(delta.Y, 0));
                float signedDistance = outside.Length() + Mathf.Min(Mathf.Max(delta.X, delta.Y), 0);
                float opacity = Mathf.SmoothStep(0f, edgeFade, signedDistance);
                if (opacity < mask.GetPixel(x, y).R) mask.SetPixel(x, y, new Color(opacity, 0, 0));
            }
        }
    }
}
