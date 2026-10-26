// Builds reusable, deterministic low-poly oak meshes (wood and leafy canopy); runs only during initialization/settings changes.
using Godot;

public static class TreeGenerator
{
    private static readonly Vector3[] Icosahedron =
    {
        new(-1, 1.618034f, 0), new(1, 1.618034f, 0), new(-1, -1.618034f, 0), new(1, -1.618034f, 0),
        new(0, -1, 1.618034f), new(0, 1, 1.618034f), new(0, -1, -1.618034f), new(0, 1, -1.618034f),
        new(1.618034f, 0, -1), new(1.618034f, 0, 1), new(-1.618034f, 0, -1), new(-1.618034f, 0, 1)
    };
    private static readonly int[] Faces =
    {
        0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
        1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
        3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
        4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
    };

    // =========================================================
    // Create two material surfaces per tree so branches and foliage can be replaced by authored art later.
    public static ArrayMesh CreateOak(TreeSettings settings, int variation, Material woodMaterial, Material leafMaterial)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 19) };
        float height = random.RandfRange(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight),
            Mathf.Max(settings.MinimumHeight, settings.MaximumHeight));
        float trunkRadius = Mathf.Max(0.1f, settings.TrunkRadius) * random.RandfRange(0.8f, 1.2f);
        float clusterSize = Mathf.Max(0.4f, settings.LeafClusterSize);
        var woodColor = new Color(0.34f, 0.23f, 0.14f);
        using var wood = new SurfaceTool();
        using var leaves = new SurfaceTool();
        wood.Begin(Mesh.PrimitiveType.Triangles);
        leaves.Begin(Mesh.PrimitiveType.Triangles);
        wood.SetMaterial(woodMaterial); leaves.SetMaterial(leafMaterial);

        // A gently crooked, tapering trunk; all positions are local to the tree's base.
        var bottom = Vector3.Zero;
        var middle = new Vector3(random.RandfRange(-0.18f, 0.18f), height * 0.48f, random.RandfRange(-0.18f, 0.18f));
        var upper = middle + new Vector3(random.RandfRange(-0.5f, 0.5f), height * 0.37f, random.RandfRange(-0.5f, 0.5f));
        var tip = upper + new Vector3(random.RandfRange(-0.25f, 0.25f), height * 0.15f, random.RandfRange(-0.25f, 0.25f));
        AddTaper(wood, bottom, middle, trunkRadius * 1.16f, trunkRadius * 0.74f, woodColor);
        AddTaper(wood, middle, upper, trunkRadius * 0.74f, trunkRadius * 0.27f, woodColor);
        AddTaper(wood, upper, tip, trunkRadius * 0.27f, trunkRadius * 0.06f, woodColor);

        // Offset branches rise from different points on the trunk; overlapping leaf masses build an irregular crown.
        int count = Mathf.Clamp(settings.BranchCount, 3, 16);
        for (int i = 0; i < count; i++)
        {
            float fraction = (i + random.RandfRange(-0.25f, 0.25f)) / count;
            float y = height * Mathf.Lerp(0.42f, 0.83f, fraction);
            float angle = i * 2.399963f + random.RandfRange(-0.5f, 0.5f);
            var start = new Vector3(Mathf.Lerp(middle.X, upper.X, 0.4f), y, Mathf.Lerp(middle.Z, upper.Z, 0.4f));
            float length = height * random.RandfRange(0.24f, 0.37f);
            var spread = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var elbow = start + spread * length * 0.6f + Vector3.Up * length * 0.27f;
            var end = start + spread * length + Vector3.Up * length * random.RandfRange(0.42f, 0.7f);
            float radius = trunkRadius * random.RandfRange(0.18f, 0.31f);
            AddTaper(wood, start, elbow, radius, radius * 0.60f, woodColor);
            AddTaper(wood, elbow, end, radius * 0.60f, radius * 0.07f, woodColor);
            float foliage = clusterSize * random.RandfRange(0.75f, 1.2f);
            AddLeafCluster(leaves, end + Vector3.Up * foliage * 0.23f,
                new Vector3(foliage * 1.05f, foliage * 0.85f, foliage), random);
            if (i % 2 == 0)
                AddLeafCluster(leaves, elbow + Vector3.Up * foliage * 0.5f,
                    new Vector3(foliage * 0.76f, foliage * 0.88f, foliage * 0.78f), random);
        }
        AddLeafCluster(leaves, tip + Vector3.Up * clusterSize * 0.18f,
            new Vector3(clusterSize * 1.15f, clusterSize * 1.05f, clusterSize * 1.15f), random);

        var mesh = new ArrayMesh();
        wood.Commit(mesh); leaves.Commit(mesh);
        return mesh;
    }

    // =========================================================
    // Build a tapered 7-sided branch with flat-shaded bark, without separate scene nodes or draw calls.
    private static void AddTaper(SurfaceTool surface, Vector3 start, Vector3 end, float bottomRadius, float topRadius, Color color)
    {
        Vector3 axis = (end - start).Normalized();
        Vector3 right = axis.Cross(Vector3.Forward).Normalized();
        if (right.LengthSquared() < 0.01f) right = axis.Cross(Vector3.Right).Normalized();
        Vector3 forward = axis.Cross(right).Normalized();
        const int sides = 7;
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.Tau / sides, b = (i + 1) * Mathf.Tau / sides;
            Vector3 radialA = right * Mathf.Cos(a) + forward * Mathf.Sin(a);
            Vector3 radialB = right * Mathf.Cos(b) + forward * Mathf.Sin(b);
            Vector3 lowA = start + radialA * bottomRadius, lowB = start + radialB * bottomRadius;
            Vector3 highA = end + radialA * topRadius, highB = end + radialB * topRadius;
            AddFace(surface, lowA, lowB, highA, color);
            AddFace(surface, highA, lowB, highB, color);
        }
    }

    // =========================================================
    // Build a subtly distorted 20-face leaf cluster with natural green shade differences.
    private static void AddLeafCluster(SurfaceTool surface, Vector3 center, Vector3 radius, RandomNumberGenerator random)
    {
        float red = random.RandfRange(0.12f, 0.22f);
        float green = random.RandfRange(0.29f, 0.45f);
        float blue = random.RandfRange(0.07f, 0.16f);
        var color = new Color(red, green, blue);
        var tilt = new Vector3(random.RandfRange(-0.2f, 0.2f), 0, random.RandfRange(-0.2f, 0.2f));
        var vertices = new Vector3[Icosahedron.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 unit = Icosahedron[i].Normalized();
            float roughness = random.RandfRange(0.86f, 1.14f);
            vertices[i] = center + new Vector3(unit.X * radius.X, unit.Y * radius.Y, unit.Z * radius.Z) * roughness
                + tilt * unit.Y;
        }
        for (int i = 0; i < Faces.Length; i += 3)
            AddFace(surface, vertices[Faces[i]], vertices[Faces[i + 1]], vertices[Faces[i + 2]], color);
    }

    // =========================================================
    // Add flat-shaded triangles, with vertex colours baked once rather than material updates per tree.
    private static void AddFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(a);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(b);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(c);
    }

    // =========================================================
    // Hash seeds independently of scene traversal order to make variants stable across restarts.
    private static ulong Mix(int seed, int x, int z)
    {
        unchecked
        {
            uint hash = (uint)seed ^ ((uint)x * 0x9E3779B9u) ^ ((uint)z * 0x85EBCA6Bu);
            hash ^= hash >> 16; hash *= 0x7FEB352Du; hash ^= hash >> 15; hash *= 0x846CA68Bu; hash ^= hash >> 16;
            return hash;
        }
    }
}
