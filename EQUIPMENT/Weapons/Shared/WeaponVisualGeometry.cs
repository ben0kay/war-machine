// Shared, shape-only firearm detailing. Three reusable beveled profiles replace many flat rectangular blocks.
// No shaders, imported textures, dynamic material writes, animation, or gameplay code; meshes are generated once.
using Godot;

public static class WeaponVisualGeometry
{
    public static readonly BoxMesh Box = new() { Size = Vector3.One };
    public static readonly Mesh Chamfer = Profile(1f, 1f);
    public static readonly Mesh TaperForward = Profile(0.76f, 1f);
    public static readonly Mesh TaperRear = Profile(1f, 0.78f);
    private static readonly CylinderMesh Round = new()
    { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 12 };

    // =========================================================
    // Scale a shared beveled solid; optional local pitch/roll makes grips and stocks less box-like.
    public static MeshInstance3D Part(Node3D parent, string name, Vector3 position, Vector3 size,
        Material material, Mesh? shape = null, Vector3? rotation = null)
    {
        var mesh = new MeshInstance3D { Name = name, Mesh = shape ?? Chamfer, Position = position,
            Scale = size, Rotation = rotation ?? Vector3.Zero, MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        parent.AddChild(mesh); return mesh;
    }

    // =========================================================
    // Low-sided cylinder along -Z; diameter/length refer to the visible weapon-space dimensions.
    public static void Tube(Node3D parent, string name, Vector3 position, float diameter, float length, Material material)
    {
        parent.AddChild(new MeshInstance3D { Name = name, Mesh = Round, Position = position,
            Rotation = new Vector3(Mathf.Pi * 0.5f, 0, 0), Scale = new Vector3(diameter, length, diameter),
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    // =========================================================
    // A shared 8-sided cross-section with two narrow bevel transitions at each Z end.
    // Front (-Z) and back (+Z) can have different breadths for believable tapered stocks and shells.
    private static ArrayMesh Profile(float front, float back)
    {
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        var rings = new Vector3[4][];
        rings[0] = Ring(-0.5f, front * 0.90f);
        rings[1] = Ring(-0.39f, front);
        rings[2] = Ring(0.39f, back);
        rings[3] = Ring(0.5f, back * 0.90f);
        for (int r = 0; r < 3; r++)
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                Face(surface, rings[r][i], rings[r][next], rings[r + 1][next]);
                Face(surface, rings[r][i], rings[r + 1][next], rings[r + 1][i]);
            }
        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            Face(surface, new Vector3(0, 0, -0.5f), rings[0][next], rings[0][i]);
            Face(surface, new Vector3(0, 0, 0.5f), rings[3][i], rings[3][next]);
        }
        return surface.Commit();
    }

    // =========================================================
    // Clockwise eight-corner perimeter in XY, with a small 45-degree chamfer at each corner.
    private static Vector3[] Ring(float z, float scale)
    {
        float h = scale * 0.5f, c = scale * 0.105f;
        return new Vector3[] {
            new(-h + c, -h, z), new(h - c, -h, z),
            new(h, -h + c, z), new(h, h - c, z),
            new(h - c, h, z), new(-h + c, h, z),
            new(-h, h - c, z), new(-h, -h + c, z)
        };
    }

    // =========================================================
    // Flat normals give clean mechanical edge breaks with minimal geometry and no normal-map textures.
    private static void Face(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        surface.SetNormal((b - a).Cross(c - a).Normalized());
        surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
    }
}
