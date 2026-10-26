// Builds deterministic tapered-bark oaks with budgeted, layered foliage and connected leafy sprigs; no per-leaf nodes.
using Godot;

public static class TreeGenerator
{
    // =========================================================
    // Create two material surfaces per tree so branches and foliage can be replaced by authored art later.
    public static ArrayMesh CreateOak(TreeSettings settings, int variation, Material woodMaterial, Material leafMaterial, int leafStride = 1)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 19) };
        float height = random.RandfRange(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight), Mathf.Max(settings.MinimumHeight, settings.MaximumHeight));
        height = Mathf.Clamp(height, 4, 24);
        float trunkRadius = Mathf.Max(0.1f, settings.TrunkRadius) * random.RandfRange(0.8f, 1.2f);
        float clusterSize = Mathf.Clamp(settings.LeafClusterSize, 0.4f, 4);
        int count = Mathf.Clamp(settings.BranchCount, 3, 16), twigs = Mathf.Clamp(settings.TwigsPerBranch, 2, 6);
        float innerScale = Mathf.Clamp(settings.InnerCanopyScale, 0, 1.5f);
        int clustersPerBranch = twigs + (innerScale > 0.01f ? 1 : 0);
        int perCluster = Mathf.Min(Mathf.Clamp(settings.LeavesPerCluster, 24, 256), Mathf.Clamp(settings.LeafBudget, 500, 8000) / (count * clustersPerBranch + 2));
        float leafLength = Mathf.Clamp(settings.LeafLength, 0.08f, 1.5f), leafWidth = Mathf.Clamp(settings.LeafWidth, 0.04f, 0.8f);
        leafStride = Mathf.Clamp(leafStride, 1, 8);
        var woodColor = new Color(0.31f, 0.23f, 0.15f);
        using var wood = new SurfaceTool();
        using var leaves = new SurfaceTool();
        wood.Begin(Mesh.PrimitiveType.Triangles); leaves.Begin(Mesh.PrimitiveType.Triangles);
        wood.SetMaterial(woodMaterial); leaves.SetMaterial(leafMaterial);
        var bottom = Vector3.Zero;
        var middle = new Vector3(random.RandfRange(-0.22f, 0.22f), height * 0.4f, random.RandfRange(-0.22f, 0.22f));
        var upper = middle + new Vector3(random.RandfRange(-0.6f, 0.6f), height * 0.42f, random.RandfRange(-0.6f, 0.6f));
        var tip = upper + new Vector3(random.RandfRange(-0.3f, 0.3f), height * 0.18f, random.RandfRange(-0.3f, 0.3f));
        AddTaper(wood, bottom, middle, trunkRadius * 1.16f, trunkRadius * 0.74f, woodColor);
        AddTaper(wood, middle, upper, trunkRadius * 0.74f, trunkRadius * 0.27f, woodColor);
        AddTaper(wood, upper, tip, trunkRadius * 0.27f, trunkRadius * 0.06f, woodColor);

        // Lower, layered branches and several leafy twigs form an irregular crown instead of a bare pole.
        for (int i = 0; i < count; i++)
        {
            float fraction = (i + random.RandfRange(-0.2f, 0.2f)) / count;
            float t = Mathf.Lerp(0.3f, 0.78f, fraction);
            Vector3 start = t < 0.4f ? bottom.Lerp(middle, t / 0.4f) : middle.Lerp(upper, (t - 0.4f) / 0.42f);
            float angle = i * 2.399963f + random.RandfRange(-0.4f, 0.4f);
            var spread = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var side = new Vector3(-spread.Z, 0, spread.X);
            float length = height * random.RandfRange(0.28f, 0.38f) * (1.05f - fraction * 0.25f);
            var elbow = start + spread * length * 0.55f + Vector3.Up * length * 0.18f;
            var end = start + spread * length + Vector3.Up * length * random.RandfRange(0.35f, 0.55f);
            float radius = trunkRadius * random.RandfRange(0.18f, 0.3f);
            AddTaper(wood, start, elbow, radius, radius * 0.55f, woodColor);
            AddTaper(wood, elbow, end, radius * 0.55f, radius * 0.06f, woodColor);
            if (innerScale > 0.01f)
            {
                float foliage = clusterSize * innerScale;
                AddLeafCluster(wood, leaves, elbow + Vector3.Up * 0.3f, new Vector3(foliage, foliage * 0.7f, foliage),
                    random, perCluster, leafLength, leafWidth, leafStride, woodColor);
            }
            for (int j = 0; j < twigs; j++)
            {
                float along = Mathf.Lerp(0.15f, 1f, (j + 0.5f) / twigs);
                var twigStart = elbow.Lerp(end, along);
                float handed = j % 2 == 0 ? 1f : -1f;
                var twigEnd = twigStart + side * handed * random.RandfRange(0.45f, 1.05f)
                    + spread * random.RandfRange(0.25f, 0.65f) + Vector3.Up * random.RandfRange(0.2f, 0.65f);
                AddTaper(wood, twigStart, twigEnd, radius * 0.24f, 0.008f, woodColor, 5);
                float foliage = clusterSize * random.RandfRange(0.52f, 0.72f);
                AddLeafCluster(wood, leaves, twigEnd, new Vector3(foliage, foliage * 0.7f, foliage), random,
                    perCluster, leafLength, leafWidth, leafStride, woodColor);
            }
        }
        AddLeafCluster(wood, leaves, tip, new Vector3(clusterSize, clusterSize * 0.65f, clusterSize), random,
            perCluster * 2, leafLength, leafWidth, leafStride, woodColor);
        var mesh = new ArrayMesh();
        wood.Commit(mesh); leaves.Commit(mesh);
        return mesh;
    }

    // =========================================================
    // Build tapered branches with bounded side counts and flat-shaded bark, without separate scene nodes.
    private static void AddTaper(SurfaceTool surface, Vector3 start, Vector3 end, float bottomRadius, float topRadius, Color color, int sides = 7)
    {
        Vector3 axis = (end - start).Normalized();
        Vector3 right = axis.Cross(Vector3.Forward).Normalized();
        if (right.LengthSquared() < 0.01f) right = axis.Cross(Vector3.Right).Normalized();
        Vector3 forward = axis.Cross(right).Normalized();
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
    // Populate connected fine sprigs with smaller folded leaves. Far LOD skips leaves without changing RNG or attachment points.
    private static void AddLeafCluster(SurfaceTool wood, SurfaceTool surface, Vector3 center, Vector3 radius,
        RandomNumberGenerator random, int count, float leafLength, float leafWidth, int leafStride, Color woodColor)
    {
        const int perSprig = 8;
        for (int shoot = 0, index = 0; index < count; shoot++)
        {
            Vector3 radial = (RandomDirection(random) + Vector3.Up * 0.2f).Normalized();
            var end = center + new Vector3(radial.X * radius.X, radial.Y * radius.Y, radial.Z * radius.Z) * random.RandfRange(0.65f, 1f);
            var axis = (end - center).Normalized();
            var side = axis.Cross(Vector3.Up);
            if (side.LengthSquared() < 0.01f) side = axis.Cross(Vector3.Right);
            side = side.Normalized();
            if (leafStride == 1 || shoot % 2 == 0) AddTaper(wood, center, end, 0.009f, 0.0025f, woodColor, 3);
            for (int j = 0; j < perSprig && index < count; j++, index++)
            {
                var anchor = center.Lerp(end, Mathf.Clamp((j + 1f) / perSprig + random.RandfRange(-0.04f, 0.04f), 0.05f, 1));
                float handed = j % 2 == 0 ? 1f : -1f;
                var outward = (side * handed + axis * 0.35f + Vector3.Up * 0.3f + RandomDirection(random) * 0.22f).Normalized();
                var across = outward.Cross(Vector3.Up);
                if (across.LengthSquared() < 0.01f) across = outward.Cross(Vector3.Right);
                across = across.Normalized();
                float lodScale = Mathf.Min(2f, Mathf.Sqrt(leafStride)); // Coarser distant leaves keep crown coverage with fewer polygons.
                float length = leafLength * random.RandfRange(0.8f, 1.2f) * lodScale, width = leafWidth * random.RandfRange(0.8f, 1.15f) * lodScale;
                var shoulders = anchor + outward * length * 0.48f;
                var tip = anchor + outward * length - Vector3.Up * length * 0.08f;
                var left = shoulders + across * width * 0.5f;
                var right = shoulders - across * width * 0.5f;
                var ridge = shoulders + outward.Cross(across).Normalized() * width * 0.18f;
                float variation = random.RandfRange(0.85f, 1.12f);
                var color = new Color(random.RandfRange(0.14f, 0.2f) * variation, random.RandfRange(0.28f, 0.4f) * variation,
                    random.RandfRange(0.065f, 0.12f) * variation);
                if (index % leafStride != 0) continue;
                AddLeafFace(surface, anchor, left, ridge, color, 0f, 0.48f, 0.48f);
                AddLeafFace(surface, left, tip, ridge, color, 0.48f, 1f, 0.48f);
                AddLeafFace(surface, tip, right, ridge, color, 1f, 0.48f, 0.48f);
                AddLeafFace(surface, right, anchor, ridge, color, 0.48f, 0f, 0.48f);
            }
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
