// Shared closed, textured anatomical low-poly contours for both infected variants.
// Cached once as static meshes; no actor-specific mesh generation or extra runtime processing.
using Godot;

public static class ZombieHumanGeometry
{
    // Ring data is (radiusX, normalized Y, radiusZ). Human contours have jaws,
    // cheekbones, a real shoulder slope, narrower hips and tapered muscular limbs.
    public static readonly Mesh Torso = Contour(new Vector3[] {
        new(0.33f, -0.50f, 0.36f), new(0.34f, -0.33f, 0.37f),
        new(0.43f, -0.08f, 0.45f), new(0.50f, 0.27f, 0.50f),
        new(0.44f, 0.39f, 0.42f), new(0.24f, 0.50f, 0.25f) });
    public static readonly Mesh Pelvis = Contour(new Vector3[] {
        new(0.30f, -0.50f, 0.38f), new(0.50f, -0.18f, 0.50f),
        new(0.43f, 0.39f, 0.42f), new(0.39f, 0.50f, 0.37f) });
    public static readonly Mesh Skull = Contour(new Vector3[] {
        new(0.27f, -0.50f, 0.34f), new(0.34f, -0.34f, 0.40f),
        new(0.43f, -0.15f, 0.45f), new(0.50f, 0.13f, 0.50f),
        new(0.45f, 0.38f, 0.46f), new(0.30f, 0.50f, 0.33f) });
    public static readonly Mesh Jaw = Contour(new Vector3[] {
        new(0.29f, -0.50f, 0.35f), new(0.44f, -0.18f, 0.45f),
        new(0.50f, 0.33f, 0.50f), new(0.42f, 0.50f, 0.44f) });
    public static readonly Mesh UpperLimb = Contour(new Vector3[] {
        new(0.30f, -0.50f, 0.34f), new(0.41f, -0.22f, 0.44f),
        new(0.50f, 0.28f, 0.50f), new(0.44f, 0.50f, 0.45f) });
    public static readonly Mesh LowerLimb = Contour(new Vector3[] {
        new(0.28f, -0.50f, 0.33f), new(0.36f, -0.23f, 0.40f),
        new(0.50f, 0.22f, 0.50f), new(0.45f, 0.50f, 0.45f) });
    public static readonly Mesh Palm = Contour(new Vector3[] {
        new(0.37f, -0.50f, 0.39f), new(0.50f, -0.20f, 0.45f),
        new(0.44f, 0.23f, 0.45f), new(0.30f, 0.50f, 0.33f) });
    public static readonly Mesh Shoe = Contour(new Vector3[] {
        new(0.45f, -0.50f, 0.49f), new(0.50f, -0.26f, 0.50f),
        new(0.45f, 0.30f, 0.40f), new(0.30f, 0.50f, 0.32f) });
    private static readonly BoxMesh Box = new() { Size = Vector3.One };

    // =========================================================
    // Match existing part names to preserve ZombieAppearance's spawn-seeded texture categories.
    // Tiny facial wounds and sockets remain their original independent material pieces.
    public static Mesh Shape(string name, Mesh original) => name switch
    {
        "SlouchedTorso" or "LeanTorso" => Torso,
        "Hips" or "Waist" => Pelvis,
        "Skull" => Skull,
        "Jaw" => Jaw,
        "ShoulderShape" or "NarrowShoulders" or "Shoulder"
            or "RaggedSleeve" or "Sleeve" or "Thigh" or "LongThigh" => UpperLimb,
        "ExposedNeck" or "BentNeck" or "ExposedForearm" or "Forearm"
            or "Shin" or "LongShin" or "KneeJoint" or "Finger" => LowerLimb,
        "Hand" => Palm,
        "ShoeUpper" or "Shoe" => Shoe,
        "ShoeToe" or "Toe" or "LeftSocket" or "RightSocket" or "LeftEye" or "RightEye"
            or "OpenMouth" or "Brow" or "HairPatch" => Box,
        _ => original
    };

    // =========================================================
    // Each hull has connected sides AND closed top/bottom caps, with UVs so existing
    // 64px skin/fabric/blood textures stay visible. Flat normals avoid round toy shading.
    private static ArrayMesh Contour(Vector3[] rings)
    {
        using var tool = new SurfaceTool();
        tool.Begin(Mesh.PrimitiveType.Triangles);
        const int sides = 10;
        for (int r = 0; r < rings.Length - 1; r++)
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Point(rings[r], i, sides), b = Point(rings[r + 1], i, sides);
                Vector3 c = Point(rings[r], i + 1, sides), d = Point(rings[r + 1], i + 1, sides);
                float u = i / (float)sides, next = (i + 1) / (float)sides;
                Tri(tool, a, b, c, new(u, rings[r].Y + 0.5f),
                    new(u, rings[r + 1].Y + 0.5f), new(next, rings[r].Y + 0.5f));
                Tri(tool, b, d, c, new(u, rings[r + 1].Y + 0.5f),
                    new(next, rings[r + 1].Y + 0.5f), new(next, rings[r].Y + 0.5f));
            }
        Vector3 bottom = rings[0], top = rings[rings.Length - 1];
        for (int i = 0; i < sides; i++)
        {
            Vector3 a = Point(bottom, i, sides), b = Point(bottom, i + 1, sides);
            Vector3 c = Point(top, i, sides), d = Point(top, i + 1, sides);
            Tri(tool, new(0, bottom.Y, 0), a, b, new(0.5f, 0.5f),
                new(0.5f + a.X, 0.5f + a.Z), new(0.5f + b.X, 0.5f + b.Z));
            Tri(tool, new(0, top.Y, 0), d, c, new(0.5f, 0.5f),
                new(0.5f + d.X, 0.5f + d.Z), new(0.5f + c.X, 0.5f + c.Z));
        }
        return tool.Commit();
    }

    // =========================================================
    private static Vector3 Point(Vector3 ring, int i, int sides)
    {
        float angle = Mathf.Tau * i / sides;
        return new(Mathf.Cos(angle) * ring.X, ring.Y, Mathf.Sin(angle) * ring.Z);
    }

    // =========================================================
    // Godot renders clockwise-facing triangles. Natural-order cross product is the outward
    // normal, so emit a,c,b with matched UVs; otherwise the character looks cut away.
    private static void Tri(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c,
        Vector2 uvA, Vector2 uvB, Vector2 uvC)
    {
        tool.SetNormal((b - a).Cross(c - a).Normalized());
        tool.SetUV(uvA); tool.AddVertex(a);
        tool.SetUV(uvC); tool.AddVertex(c);
        tool.SetUV(uvB); tool.AddVertex(b);
    }
}
