// Small shared geometry helpers; generators use vertex colours and one shared surface per category.
using Godot;

public sealed class ContentMeshBuilder : System.IDisposable
{
    private readonly SurfaceTool _surface = new();
    public ContentMeshBuilder(Material material) { _surface.Begin(Mesh.PrimitiveType.Triangles); _surface.SetMaterial(material); }

    // =========================================================
    // Flat shading and height-weighted wind are baked once, never updated per object.
    public void Face(Vector3 a, Vector3 b, Vector3 c, Color color, bool wind = false)
    {
        var normal = (b - a).Cross(c - a).Normalized(); _surface.SetNormal(normal); _surface.SetColor(color);
        foreach (var vertex in new[] { a, b, c })
        { _surface.SetUV(new Vector2(0, wind ? Mathf.Clamp(vertex.Y, 0, 1) : 0)); _surface.AddVertex(vertex); }
    }

    // =========================================================
    // Pointed folded leaves and petals, double-sided through geometry for predictable rendering.
    public void Leaf(Vector3 start, Vector3 end, float width, Color color)
    {
        var axis = (end - start).Normalized(); var side = axis.Cross(Vector3.Up);
        if (side.LengthSquared() < 0.01f) side = Vector3.Right;
        side = side.Normalized() * width;
        var mid = start.Lerp(end, 0.45f) + Vector3.Up * width * 0.25f;
        Face(start, mid + side, end, color, true); Face(start, end, mid - side, color * 0.9f, true);
        Face(start, end, mid + side, color, true); Face(start, mid - side, end, color * 0.9f, true);
    }

    // =========================================================
    // Closed low-sided trunk/stem with contrasting cut ends for fallen wood.
    public void Tube(Vector3 start, Vector3 end, float radius, float tipRadius, Color bark, Color cut, int sides = 6)
    {
        var axis = (end - start).Normalized(); var side = axis.Cross(Vector3.Up);
        if (side.LengthSquared() < 0.01f) side = Vector3.Right;
        side = side.Normalized(); var up = side.Cross(axis).Normalized();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.Tau / sides, b = (i + 1) * Mathf.Tau / sides;
            var da = side * Mathf.Cos(a) + up * Mathf.Sin(a); var db = side * Mathf.Cos(b) + up * Mathf.Sin(b);
            var p = start + da * radius; var q = start + db * radius;
            var r = end + da * tipRadius; var s = end + db * tipRadius;
            Face(p, r, q, bark); Face(q, r, s, bark * 0.92f);
            Face(start, p, q, cut); Face(end, s, r, cut);
        }
    }

    // =========================================================
    public ArrayMesh Finish() => _surface.Commit();
    // =========================================================
    public void Dispose() => _surface.Dispose();
}
