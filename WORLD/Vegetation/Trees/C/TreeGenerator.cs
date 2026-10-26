// Builds deterministic straight-trunk conifers with budgeted, layered foliage and connected leafy sprigs; no per-leaf nodes.
using Godot;

public static class TreeGenerator
{
    // =========================================================
    // Create two material surfaces per tree so branches and foliage can be replaced by authored art later.
    public static ArrayMesh CreatePine(TreeSettings settings, int variation, Material woodMaterial, Material leafMaterial, int leafStride = 1)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 19) };
        float height = random.RandfRange(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight), Mathf.Max(settings.MinimumHeight, settings.MaximumHeight)) * settings.HeightMultiplier;
        float radius = Mathf.Max(0.05f, settings.TrunkRadius);
        using var wood = new SurfaceTool(); using var leaves = new SurfaceTool();
        wood.Begin(Mesh.PrimitiveType.Triangles); leaves.Begin(Mesh.PrimitiveType.Triangles);
        wood.SetMaterial(woodMaterial); leaves.SetMaterial(leafMaterial);
        var bark = new Color(0.25f, 0.19f, 0.14f);
        AddTaper(wood, Vector3.Zero, Vector3.Up * height, radius, radius * (1 - settings.TrunkTaper), bark, 8);
        int tiers = Mathf.Clamp(settings.BranchTierCount, 3, 24), branches = 6;
        int count = Mathf.Clamp((int)(settings.LeafBudget * settings.FoliageDensity / (tiers * branches)), 6, 160);
        for (int tier = 0; tier < tiers; tier++)
        {
            float t = tier / (float)tiers;
            float y = height * Mathf.Lerp(0.18f, 0.95f, t);
            float length = height * settings.ConicalWidth * Mathf.Pow(1 - t, settings.BranchLengthFalloff);
            float rotation = random.RandfRange(0, Mathf.Tau);
            for (int branch = 0; branch < branches; branch++)
            {
                float angle = rotation + branch * Mathf.Tau / branches + random.RandfRange(-0.12f, 0.12f);
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var start = Vector3.Up * y;
                var end = start + direction * length * random.RandfRange(0.85f, 1.1f) - Vector3.Up * length * 0.24f;
                AddTaper(wood, start, end, radius * (1 - t) * 0.24f, 0.008f, bark, 5);
                for (int j = 0; j < count; j++)
                {
                    float along = random.RandfRange(0.18f, 1f);
                    var center = start.Lerp(end, along);
                    var side = new Vector3(-direction.Z, 0, direction.X);
                    float fan = length * 0.32f * (1 - along * 0.65f);
                    var anchor = center + side * random.RandfRange(-fan, fan) + Vector3.Up * random.RandfRange(-0.12f, 0.16f);
                    var axis = (direction + side * random.RandfRange(-1.8f, 1.8f) - Vector3.Up * 0.25f).Normalized();
                    float needle = random.RandfRange(0.25f, 0.55f) * Mathf.Min(1.6f, Mathf.Sqrt(leafStride));
                    float shade = random.RandfRange(1 - settings.ColourVariation, 1 + settings.ColourVariation);
                    var color = new Color(0.085f, 0.23f, 0.135f) * shade;
                    if (j % leafStride != 0) continue;
                    // Narrow pointed needle fans overlap in branch layers, never broadleaf crowns.
                    for (int n = -1; n <= 1; n++)
                    {
                        var tip = anchor + (axis + side * n * 0.3f) * needle;
                        AddLeafFace(leaves, anchor - side * 0.035f, anchor + side * 0.035f, tip, color, 0, 0, 1);
                        AddLeafFace(leaves, anchor - Vector3.Up * 0.025f, anchor + Vector3.Up * 0.025f, tip, color, 0, 0, 1);
                    }
                }
            }
        }
        var mesh = new ArrayMesh(); wood.Commit(mesh); leaves.Commit(mesh); return mesh;
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
