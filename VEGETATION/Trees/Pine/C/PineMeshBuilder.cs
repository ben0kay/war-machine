// Builds deterministic asymmetric conifers with layered needle sprays and budgeted near/far meshes; no per-leaf nodes.
using Godot;

public static class PineMeshBuilder
{
    // =========================================================
    // Two materials and one seeded skeleton per variant; only the foliage density changes across LODs.
    public static ArrayMesh CreatePine(PineSettings settings, int variation, Material woodMaterial, Material leafMaterial, int leafStride = 1)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 19) };
        float height = random.RandfRange(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight),
            Mathf.Max(settings.MinimumHeight, settings.MaximumHeight)) * settings.HeightMultiplier;
        float radius = Mathf.Max(0.05f, settings.TrunkRadius);
        using var wood = new SurfaceTool(); using var leaves = new SurfaceTool();
        wood.Begin(Mesh.PrimitiveType.Triangles); leaves.Begin(Mesh.PrimitiveType.Triangles);
        wood.SetMaterial(woodMaterial); leaves.SetMaterial(leafMaterial);
        var bark = new Color(0.25f, 0.19f, 0.14f);
        float trunkRadius = GetTrunkRadius(settings, variation);
        AddTaper(wood, Vector3.Zero, Vector3.Up * height, trunkRadius, trunkRadius * (1 - settings.TrunkTaper), bark, 8);

        bool distant = leafStride > 1;
        int tiers = Mathf.Clamp(settings.BranchTierCount, 3, 24);
        // LeafBudget counts clusters, not triangles. This cap bounds extra near-detail geometry.
        int budget = Mathf.Clamp((int)(settings.LeafBudget * Mathf.Clamp(settings.FoliageDensity, 0f, 2f)), 72, 1200);
        int nearCount = Mathf.Max(2, budget / (tiers * 6));
        int clusterCount = distant ? Mathf.Max(1, Mathf.CeilToInt((float)nearCount / Mathf.Clamp(leafStride, 2, 8))) : nearCount;
        float irregularity = Mathf.Clamp(settings.BranchIrregularity, 0f, 1f);
        float spread = Mathf.Clamp(settings.NeedleSpraySpread, 0.1f, 1f);
        float layering = Mathf.Clamp(settings.FoliageLayering, 0f, 1f);
        float droop = Mathf.Clamp(settings.BranchDroop, 0f, 0.5f);

        for (int tier = 0; tier < tiers; tier++)
        {
            float t = tier / (float)tiers;
            random.Seed = Mix(settings.Seed, variation, tier + 101);
            int branches = 5 + random.RandiRange(0, 2);
            float rotation = random.RandfRange(0, Mathf.Tau);
            float y = height * (Mathf.Lerp(0.17f, 0.94f, t) + random.RandfRange(-0.016f, 0.016f) * irregularity);
            float tierLength = height * settings.ConicalWidth * Mathf.Pow(1 - t, settings.BranchLengthFalloff);
            for (int branch = 0; branch < branches; branch++)
            {
                // Reset the random stream per branch: near and far keep identical wood silhouettes.
                random.Seed = Mix(settings.Seed + variation, tier, branch + 501);
                float angle = rotation + branch * Mathf.Tau / branches + random.RandfRange(-0.28f, 0.28f) * irregularity;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var side = new Vector3(-direction.Z, 0, direction.X);
                var start = Vector3.Up * (y + random.RandfRange(-0.012f, 0.012f) * height * irregularity);
                float length = tierLength * Mathf.Lerp(1f, random.RandfRange(0.72f, 1.23f), irregularity);
                float bend = length * (droop + random.RandfRange(-0.10f, 0.10f) * irregularity);
                var end = start + direction * length - Vector3.Up * bend;
                AddTaper(wood, start, end, radius * (1 - t) * 0.24f, 0.008f, bark, 5);

                for (int j = 0; j < clusterCount; j++)
                {
                    // Sprays grow along branches, with modest lateral offsets instead of floating cones.
                    float along = Mathf.Lerp(0.24f, 0.97f, (j + random.RandfRange(0.2f, 0.8f)) / clusterCount);
                    float fan = length * 0.24f * (1 - along * 0.45f);
                    var anchor = start.Lerp(end, along) + side * random.RandfRange(-fan, fan)
                        + Vector3.Up * random.RandfRange(-0.12f, 0.12f);
                    var axis = (direction + side * random.RandfRange(-0.7f, 0.7f)
                        + Vector3.Up * random.RandfRange(-0.25f, 0.12f)).Normalized();
                    var across = axis.Cross(Vector3.Up);
                    if (across.LengthSquared() < 0.001f) across = axis.Cross(Vector3.Right);
                    across = across.Normalized();
                    var up = across.Cross(axis).Normalized();
                    float needle = settings.NeedleClusterLength * Mathf.Lerp(1.16f, 0.58f, t)
                        * random.RandfRange(0.78f, 1.25f) * (distant ? 1.65f : 1f);
                    float width = settings.NeedleClusterThickness * (1 + 0.85f * spread)
                        * (distant ? 2f : 1f) * random.RandfRange(0.85f, 1.15f);
                    float tone = random.RandfRange(1 - settings.ColourVariation, 1 + settings.ColourVariation)
                        * Mathf.Lerp(0.83f, 1.08f, t) * Mathf.Lerp(0.90f, 1.06f, along);
                    var color = new Color(0.09f, 0.27f, 0.15f) * tone;

                    // Each blade is two-sided, opaque and anchored for the existing GPU tip wind.
                    AddNeedleSpray(leaves, anchor, axis, across, up, needle, width, color * 0.88f);
                    if (distant || random.Randf() < layering)
                    {
                        var siblingAxis = (axis + across * random.RandfRange(-0.7f, 0.7f) * spread
                            + up * random.RandfRange(-0.25f, 0.25f)).Normalized();
                        var siblingAcross = siblingAxis.Cross(up);
                        if (siblingAcross.LengthSquared() < 0.001f) siblingAcross = across;
                        siblingAcross = siblingAcross.Normalized();
                        var siblingUp = siblingAcross.Cross(siblingAxis).Normalized();
                        var siblingAnchor = anchor + across * (width * 0.22f) - axis * (needle * 0.10f);
                        AddNeedleSpray(leaves, siblingAnchor, siblingAxis, siblingAcross, siblingUp,
                            needle * (distant ? 0.85f : random.RandfRange(0.65f, 0.94f)),
                            width * (distant ? 0.85f : 0.68f), color * 1.08f);
                    }
                }
            }
        }
        var mesh = new ArrayMesh(); wood.Commit(mesh); leaves.Commit(mesh); return mesh;
    }

    // =========================================================
    // Four opaque triangles per diamond-like spray: no alpha sorting or extra surface materials.
    // UV.y stays at the branch root and rises towards the tips for shared wind movement.
    private static void AddNeedleSpray(SurfaceTool leaves, Vector3 anchor, Vector3 axis,
        Vector3 across, Vector3 up, float length, float width, Color color)
    {
        var tip = anchor + axis * length;
        var midpoint = anchor + axis * (length * 0.49f);
        var right = midpoint + across * width + up * (width * 0.16f);
        var left = midpoint - across * (width * 0.75f) - up * (width * 0.12f);
        AddLeafFace(leaves, anchor, right, tip, color, 0, 0.5f, 1);
        AddLeafFace(leaves, anchor, tip, left, color * 0.92f, 0, 1, 0.5f);
        AddLeafFace(leaves, tip, right, anchor, color * 0.92f, 1, 0.5f, 0);
        AddLeafFace(leaves, left, tip, anchor, color * 0.86f, 0.5f, 1, 0);
    }

    // =========================================================
    // Rendering and trunk collision share the same seeded thickness, including authored trunk variations.
    public static float GetTrunkRadius(PineSettings settings, int variation)
    {
        var random = new RandomNumberGenerator { Seed = Mix(settings.Seed, variation, 7741) };
        float low = Mathf.Max(0.1f, Mathf.Min(settings.MinimumTrunkThickness, settings.MaximumTrunkThickness));
        float high = Mathf.Max(low, Mathf.Max(settings.MinimumTrunkThickness, settings.MaximumTrunkThickness));
        return Mathf.Max(0.05f, settings.TrunkRadius) * Mathf.Max(0.1f, settings.TrunkThicknessMultiplier) * random.RandfRange(low, high);
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
            // Close both ends as well as the bark sides; the same helper serves trunks and branches.
            AddFace(surface, start, lowB, lowA, color);
            AddFace(surface, end, highA, highB, color);
        }
    }

    // =========================================================
    // Encode each triangle's leaf attachment weighting for independent GPU wind bending.
    private static void AddLeafFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c,
        Color color, float anchorWeight, float middleWeight, float tipWeight)
    {
        (a, b) = (b, a); (anchorWeight, middleWeight) = (middleWeight, anchorWeight);
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(0.5f, anchorWeight)); surface.AddVertex(a);
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(0f, middleWeight)); surface.AddVertex(b);
        surface.SetNormal(normal); surface.SetColor(color); surface.SetUV(new Vector2(1f, tipWeight)); surface.AddVertex(c);
    }

    // =========================================================
    // Add flat-shaded triangles, with vertex colours baked once rather than material updates per tree.
    private static void AddFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        // Keep the outward lighting normal, but submit clockwise front faces for Godot's solid wood surface.
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(a);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(c);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(b);
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
