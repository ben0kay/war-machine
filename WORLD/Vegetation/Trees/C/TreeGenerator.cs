// Builds deterministic tapered-bark oaks with hundreds of individually shaped leaf polygons; no per-leaf nodes.
using Godot;

public static class TreeGenerator
{
    // =========================================================
    // Create two material surfaces per tree so branches and foliage can be replaced by authored art later.
    public static ArrayMesh CreateOak(TreeSettings settings, int variation, Material woodMaterial, Material leafMaterial)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 19) };
        float height = random.RandfRange(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight),
            Mathf.Max(settings.MinimumHeight, settings.MaximumHeight));
        float trunkRadius = Mathf.Max(0.1f, settings.TrunkRadius) * random.RandfRange(0.8f, 1.2f);
        float clusterSize = Mathf.Max(0.4f, settings.LeafClusterSize);
        int leavesPerCluster = Mathf.Clamp(settings.LeavesPerCluster, 12, 128);
        float leafLength = Mathf.Clamp(settings.LeafLength, 0.2f, 1.5f);
        float leafWidth = Mathf.Clamp(settings.LeafWidth, 0.08f, 0.8f);
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

        // Offset branches support hundreds of separate, tapering leaves, not opaque geometric canopy balls.
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
                new Vector3(foliage * 1.05f, foliage * 0.85f, foliage), random,
                leavesPerCluster, leafLength, leafWidth);
            if (i % 2 == 0)
                AddLeafCluster(leaves, elbow + Vector3.Up * foliage * 0.5f,
                    new Vector3(foliage * 0.76f, foliage * 0.88f, foliage * 0.78f), random,
                    Mathf.Max(12, leavesPerCluster / 2), leafLength, leafWidth);
        }
        AddLeafCluster(leaves, tip + Vector3.Up * clusterSize * 0.18f,
            new Vector3(clusterSize * 1.15f, clusterSize * 1.05f, clusterSize * 1.15f), random,
            leavesPerCluster * 2, leafLength, leafWidth);

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
    // Place individual pointed leaf diamonds throughout a loose irregular crown, with no per-leaf nodes.
    private static void AddLeafCluster(SurfaceTool surface, Vector3 center, Vector3 radius,
        RandomNumberGenerator random, int count, float leafLength, float leafWidth)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 radial = RandomDirection(random);
            float depth = Mathf.Pow(random.Randf(), 0.55f); // Dense outer shell with some interior foliage.
            var anchor = center + new Vector3(radial.X * radius.X, radial.Y * radius.Y, radial.Z * radius.Z) * depth;
            var outward = (radial * 0.66f + Vector3.Up * 0.2f + RandomDirection(random) * 0.55f).Normalized();
            var spin = RandomDirection(random);
            Vector3 side = outward.Cross(spin).Normalized();
            if (side.LengthSquared() < 0.01f) side = outward.Cross(Vector3.Right).Normalized();
            float length = leafLength * random.RandfRange(0.65f, 1.3f);
            float width = leafWidth * random.RandfRange(0.65f, 1.15f);
            var start = anchor - outward * (length * 0.24f);
            var shoulders = start + outward * (length * 0.45f);
            var left = shoulders + side * (width * 0.5f);
            var right = shoulders - side * (width * 0.5f);
            var tip = start + outward * length + Vector3.Up * (length * random.RandfRange(-0.09f, 0.14f));
            float variation = random.RandfRange(0.8f, 1.15f);
            var shade = new Color(random.RandfRange(0.13f, 0.2f), random.RandfRange(0.28f, 0.43f), random.RandfRange(0.055f, 0.13f));
            var color = new Color(shade.R * variation, shade.G * variation, shade.B * variation);

            // UV.y encodes stiffness: attached base is fixed, shoulders bend, outer tip flutters most.
            AddLeafFace(surface, start, left, tip, color, 0f, 0.45f, 1f);
            AddLeafFace(surface, start, tip, right, color, 0f, 1f, 0.45f);
        }
    }

    // =========================================================
    // Generate deterministic leaf orientations without allocations or a runtime leaf update loop.
    private static Vector3 RandomDirection(RandomNumberGenerator random)
    {
        var direction = new Vector3(random.RandfRange(-1f, 1f), random.RandfRange(-1f, 1f), random.RandfRange(-1f, 1f));
        return direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector3.Up;
    }

    // =========================================================
    // Encode each triangle's leaf attachment weighting for independent GPU wind bending.
    private static void AddLeafFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c,
        Color color, float anchorWeight, float middleWeight, float tipWeight)
    {
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(0.5f, anchorWeight)); surface.AddVertex(a);
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(0f, middleWeight)); surface.AddVertex(b);
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(1f, tipWeight)); surface.AddVertex(c);
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
