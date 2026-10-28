// Shared one-time procedural structure primitives, collision boxes and gable surfaces.
// Mesh construction only; no placement, equipment, actor behaviour or per-frame callbacks.
using Godot;

public static class StructureGeometry
{
    private static readonly BoxMesh Cube = new() { Size = Vector3.One };

    // =========================================================
    public static Node3D Group(Node parent, string name)
    {
        var group = new Node3D { Name = name };
        parent.AddChild(group); return group;
    }

    // =========================================================
    public static StaticBody3D Body(Node parent)
    {
        var body = new StaticBody3D { Name = "Collision", CollisionLayer = 1, CollisionMask = 1 };
        parent.AddChild(body); return body;
    }

    // =========================================================
    public static StandardMaterial3D Paint(Color color, float metallic = 0f) =>
        new() { AlbedoColor = color, Roughness = 0.84f, Metallic = metallic };

    // =========================================================
    // Instance one cached cube, scale visually, and create a matching box collider only for solid parts.
    public static void Box(Node3D visual, StaticBody3D? collision, string name,
        Vector3 center, Vector3 size, Material material, float pitch = 0f)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0) return;
        visual.AddChild(new MeshInstance3D { Name = name, Mesh = Cube, Position = center, Scale = size,
            Rotation = new Vector3(pitch, 0, 0), MaterialOverride = material });
        if (collision == null) return;
        collision.AddChild(new CollisionShape3D { Name = name + "Shape", Position = center,
            Rotation = new Vector3(pitch, 0, 0), Shape = new BoxShape3D { Size = size } });
    }

    // =========================================================
    // True front/back wall spans let callers deliberately leave clear physical door/window openings.
    public static void WallX(Node3D visual, StaticBody3D body, string name, float a, float b,
        float z, float bottom, float top, Material material, float thickness = 0.23f)
    {
        if (b <= a || top <= bottom) return;
        Box(visual, body, name, new((a + b) * 0.5f, (top + bottom) * 0.5f, z),
            new(b - a, top - bottom, thickness), material);
    }

    // =========================================================
    // True left/right side wall spans.
    public static void WallZ(Node3D visual, StaticBody3D body, string name, float x,
        float a, float b, float bottom, float top, Material material, float thickness = 0.23f)
    {
        if (b <= a || top <= bottom) return;
        Box(visual, body, name, new(x, (top + bottom) * 0.5f, (a + b) * 0.5f),
            new(thickness, top - bottom, b - a), material);
    }

    // =========================================================
    // Single low-sided cylinder for chimneys, fuel pipe joints, lamps or antennas.
    public static void Cylinder(Node3D visual, string name, Vector3 center, float radius,
        float height, Material material, int sides = 10)
    {
        visual.AddChild(new MeshInstance3D { Name = name, Position = center,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius,
                Height = height, RadialSegments = sides }, MaterialOverride = material });
    }

    // =========================================================
    // Two sloped planar roof slabs across Z; shadowed and physical. Ridge runs along X.
    public static void GableRoof(Node3D roof, StaticBody3D body, float width, float depth,
        float wallHeight, float rise, Material roofMat, Material ridgeMat, int ribs = 0)
    {
        float half = depth * 0.5f + 0.5f;
        float slant = Mathf.Sqrt(half * half + rise * rise);
        float pitch = Mathf.Atan2(rise, half);
        for (int side = -1; side <= 1; side += 2)
        {
            float angle = side * pitch;
            Vector3 center = new(0, wallHeight + rise * 0.5f, side * half * 0.5f);
            Box(roof, body, "SlopedRoof", center, new(width + 1.1f, 0.20f, slant + 0.17f),
                roofMat, angle);
            Vector3 normal = new(0, Mathf.Cos(angle), Mathf.Sin(angle));
            for (int i = 0; i < ribs; i++)
            {
                float x = -(width + 0.75f) * 0.5f + i * (width + 0.75f) / Mathf.Max(1, ribs - 1);
                Box(roof, null, "RoofSeam", center + normal * 0.145f + Vector3.Right * x,
                    new(0.07f, 0.06f, slant + 0.14f), ridgeMat, angle);
            }
        }
        Box(roof, null, "Ridge", new(0, wallHeight + rise + 0.11f, 0),
            new(width + 1.2f, 0.21f, 0.34f), ridgeMat);
    }

    // =========================================================
    // Flat-shaded triangular end caps at the outer X faces, double-sided for interior readability.
    public static void GableEnd(Node3D visual, string name, float x, float halfDepth,
        float wallHeight, float rise, Material material)
    {
        using var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles); st.SetMaterial(material);
        Vector3 a = new(x, wallHeight, -halfDepth), b = new(x, wallHeight, halfDepth);
        Vector3 c = new(x, wallHeight + rise, 0);
        Face(st, a, b, c); Face(st, c, b, a);
        visual.AddChild(new MeshInstance3D { Name = name, Mesh = st.Commit() });
    }

    // =========================================================
    private static void Face(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c)
    {
        st.SetNormal((b - a).Cross(c - a).Normalized());
        st.AddVertex(a); st.AddVertex(b); st.AddVertex(c);
    }
}
