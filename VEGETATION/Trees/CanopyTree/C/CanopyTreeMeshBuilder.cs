// DF1: deterministic high-canopy broadleaf geometry; reusable near/far meshes for future DF2 MultiMesh.
// Builds exactly two opaque vertex-coloured surfaces: bark/branches and irregular leaf clusters.
using Godot;

public static class CanopyTreeMeshBuilder
{
    // =========================================================
    // DF2 will call this once per variant/LOD and share the returned ArrayMesh in batches.
    public static ArrayMesh Create(CanopyTreeSettings settings, int variant, Material barkMaterial,
        Material leafMaterial, bool distant = false)
    {
        var rng = new RandomNumberGenerator { Seed = Mix(settings.Seed, variant, 23) };
        float lo = Mathf.Clamp(Mathf.Min(settings.MinimumHeight, settings.MaximumHeight), 12f, 28f);
        float hi = Mathf.Clamp(Mathf.Max(settings.MinimumHeight, settings.MaximumHeight), lo, 28f);
        float height = rng.RandfRange(lo, hi);
        float startY = height * Mathf.Clamp(settings.CrownStartFraction, 0.35f, 0.75f);
        float trunkRadius = Mathf.Clamp(settings.TrunkBaseRadius, 0.2f, 0.9f) * rng.RandfRange(0.85f, 1.12f);
        float crownRadius = Mathf.Clamp(settings.CrownRadius, 2f, 5f);
        float leafScale = Mathf.Clamp(settings.FoliageSize, 0.5f, 1.8f);
        // DF2.5: small silhouette leaf tips only in the near LOD; retain the established crown masses.
        float edgeDetail = distant ? 0f : Mathf.Clamp(settings.LeafEdgeDetail, 0f, 0.25f);
        int branchCount = Mathf.Clamp(settings.BranchCount, 7, 20);
        int clusters = Mathf.Clamp(settings.CrownClustersPerBranch, 2, 5);

        using var bark = new SurfaceTool();
        using var foliage = new SurfaceTool();
        bark.Begin(Mesh.PrimitiveType.Triangles); foliage.Begin(Mesh.PrimitiveType.Triangles);
        bark.SetMaterial(barkMaterial); foliage.SetMaterial(leafMaterial);

        // Segmented trunk has slight bends and pale bark with irregular dark patches.
        var points = new Vector3[7];
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            float drift = t * t * 0.21f;
            points[i] = new Vector3(Mathf.Sin(t * 4.7f + variant) * drift, t * height,
                Mathf.Cos(t * 3.8f + variant) * drift);
            if (i == 0) continue;
            float from = (i - 1f) / (points.Length - 1), to = t;
            AddTaper(bark, points[i - 1], points[i],
                Mathf.Lerp(trunkRadius, trunkRadius * 0.16f, from),
                Mathf.Lerp(trunkRadius, trunkRadius * 0.16f, to),
                settings.BarkColor, settings.BarkScarColor, rng, 9);
        }

        // Branches start above eye level and fan outward/upward into an open, uneven crown.
        float baseAngle = rng.RandfRange(0f, Mathf.Tau);
        for (int branch = 0; branch < branchCount; branch++)
        {
            float level = (branch + rng.RandfRange(0.1f, 0.9f)) / branchCount;
            float anchorY = Mathf.Lerp(startY, height * 0.91f, level);
            float angle = baseAngle + branch * 2.3999632f + rng.RandfRange(-0.28f, 0.28f);
            Vector3 outward = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float reach = crownRadius * rng.RandfRange(0.57f, 1.12f)
                * Mathf.Lerp(1f, 0.65f, level);
            Vector3 origin = points[0].Lerp(points[^1], anchorY / height);
            Vector3 elbow = origin + outward * reach * 0.54f + Vector3.Up * rng.RandfRange(0.38f, 1.0f);
            Vector3 tip = origin + outward * reach + Vector3.Up * rng.RandfRange(0.9f, 2.2f);
            float branchRadius = trunkRadius * Mathf.Lerp(0.29f, 0.13f, level);
            AddTaper(bark, origin, elbow, branchRadius, branchRadius * 0.60f,
                settings.BarkColor, settings.BarkScarColor, rng, 6);
            AddTaper(bark, elbow, tip, branchRadius * 0.60f, 0.025f,
                settings.BarkColor, settings.BarkScarColor, rng, 5);

            // Always consume the same random choices for near and distant art; only emitted
            // leaf geometry differs, keeping both levels recognisably the same tree.
            for (int clump = 0; clump < clusters; clump++)
            {
                float along = Mathf.Lerp(0.52f, 1.02f, (clump + rng.RandfRange(0.2f, 0.8f)) / clusters);
                Vector3 center = elbow.Lerp(tip, along)
                    + outward.Cross(Vector3.Up) * rng.RandfRange(-0.8f, 0.8f)
                    + Vector3.Up * rng.RandfRange(-0.48f, 0.80f);
                float radius = leafScale * rng.RandfRange(0.90f, 1.45f);
                float width = radius * rng.RandfRange(0.83f, 1.32f);
                float depth = radius * rng.RandfRange(0.86f, 1.26f);
                float tall = radius * rng.RandfRange(0.65f, 1.0f);
                Color shade = settings.LeafColor.Lerp(settings.LeafAccentColor, rng.RandfRange(0.02f, 0.67f));
                if (distant && clump % 2 == 1) continue;
                float scale = distant ? Mathf.Clamp(settings.FarFoliageScale, 1f, 1.6f) : 1f;
                // Clump topology must not consume the skeleton RNG: near/far keep identical branches.
                var detailRng = new RandomNumberGenerator { Seed = Mix(settings.Seed + variant * 101, branch, clump + 501) };
                AddCrownClump(foliage, center, new Vector3(width, tall, depth) * scale,
                    shade, detailRng, distant ? 6 : 8, edgeDetail);
            }
        }

        // Fill the upper silhouette, with irregular breaks between crowns for sun patches.
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.Tau / 5f + baseAngle;
            var center = points[^1] - Vector3.Up * rng.RandfRange(0.9f, 2f)
                + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * rng.RandfRange(0.35f, 1.4f);
            float radius = leafScale * rng.RandfRange(0.98f, 1.35f);
            Color topShade = settings.LeafColor.Lerp(settings.LeafAccentColor, rng.RandfRange(0.15f, 0.6f));
            var detailRng = new RandomNumberGenerator { Seed = Mix(settings.Seed + variant * 101, i, 901) };
            AddCrownClump(foliage, center, new Vector3(radius, radius * 0.76f, radius),
                topShade, detailRng, distant ? 6 : 8, edgeDetail);
        }
        var mesh = new ArrayMesh();
        bark.Commit(mesh); foliage.Commit(mesh);
        // Explicit post-commit binding: the two surfaces MUST retain different materials
        // when reused by MultiMesh, regardless of SurfaceTool's material state.
        mesh.SurfaceSetMaterial(0, barkMaterial);
        mesh.SurfaceSetMaterial(1, leafMaterial);
        return mesh;
    }

    // =========================================================
    // Rounded low-poly crown stays intact; a few short pointed edge facets suggest leaves at near LOD.
    private static void AddCrownClump(SurfaceTool surface, Vector3 center, Vector3 radius,
        Color color, RandomNumberGenerator random, int sides, float edgeDetail)
    {
        var lower = new Vector3[sides]; var upper = new Vector3[sides];
        float phase = random.RandfRange(0f, Mathf.Tau);
        for (int i = 0; i < sides; i++)
        {
            float a = phase + i * Mathf.Tau / sides;
            float wobble = random.RandfRange(0.78f, 1.16f);
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            lower[i] = center + new Vector3(cs * radius.X * 0.78f * wobble,
                -radius.Y * random.RandfRange(0.38f, 0.59f), sn * radius.Z * 0.78f * wobble);
            upper[i] = center + new Vector3(cs * radius.X * wobble,
                radius.Y * random.RandfRange(0.13f, 0.45f), sn * radius.Z * wobble);
        }
        Vector3 bottom = center - Vector3.Up * radius.Y * 0.96f;
        Vector3 top = center + Vector3.Up * radius.Y * 0.94f;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            Color shade = color * random.RandfRange(0.88f, 1.10f);
            AddFace(surface, bottom, lower[i], lower[next], shade * 0.81f);
            AddFace(surface, lower[i], upper[i], lower[next], shade * 0.94f);
            AddFace(surface, lower[next], upper[i], upper[next], shade);
            AddFace(surface, upper[i], top, upper[next], shade * 1.05f);
        }
        // Three subtle outer-edge leaf facets per near clump (~9% extra foliage triangles).
        // The far LOD adds none; no new surfaces, cards, transparency or material variants.
        if (edgeDetail <= 0f) return;
        for (int i = 0; i < sides; i += 3)
        {
            int next = (i + 1) % sides;
            Vector3 rim = (upper[i] + upper[next]) * 0.5f;
            Vector3 outward = new(rim.X - center.X, 0f, rim.Z - center.Z);
            if (outward.LengthSquared() < 0.001f) continue;
            outward = outward.Normalized();
            float length = (radius.X + radius.Z) * 0.5f * edgeDetail
                * random.RandfRange(0.75f, 1.15f);
            Vector3 tip = rim + outward * length
                + Vector3.Up * radius.Y * edgeDetail * random.RandfRange(-0.3f, 0.5f);
            Vector3 left = upper[i].Lerp(lower[i], 0.12f);
            Vector3 right = upper[next].Lerp(lower[next], 0.12f);
            AddFace(surface, left, tip, right, color * random.RandfRange(0.93f, 1.04f));
        }
    }

    // =========================================================
    // Few-sided tapered wood tubes with seed-driven pale/dark face variation; no mesh child per branch.
    private static void AddTaper(SurfaceTool surface, Vector3 from, Vector3 to, float r0, float r1,
        Color bark, Color scar, RandomNumberGenerator rng, int sides)
    {
        Vector3 axis = (to - from).Normalized();
        Vector3 right = axis.Cross(Vector3.Forward).Normalized();
        if (right.LengthSquared() < 0.01f) right = axis.Cross(Vector3.Right).Normalized();
        Vector3 other = axis.Cross(right).Normalized();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.Tau / sides, b = (i + 1) * Mathf.Tau / sides;
            Vector3 ra = right * Mathf.Cos(a) + other * Mathf.Sin(a);
            Vector3 rb = right * Mathf.Cos(b) + other * Mathf.Sin(b);
            Vector3 a0 = from + ra * r0, b0 = from + rb * r0;
            Vector3 a1 = to + ra * r1, b1 = to + rb * r1;
            Color face = rng.Randf() < 0.12f ? bark.Lerp(scar, 0.45f)
                : bark * rng.RandfRange(0.89f, 1.10f);
            AddFace(surface, a0, b0, a1, face);
            AddFace(surface, a1, b0, b1, face);
        }
    }

    // =========================================================
    // One flat-shaded clockwise triangle; two-sided foliage is handled by its material.
    private static void AddFace(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(a);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(c);
        surface.SetNormal(normal); surface.SetColor(color); surface.AddVertex(b);
    }

    // =========================================================
    // Stable per-variant seed independent of preview instance order and later DF2 chunk order.
    private static ulong Mix(int seed, int variant, int salt)
    {
        unchecked
        {
            uint x = (uint)seed ^ ((uint)(variant + 1) * 0x9E3779B9u) ^ ((uint)salt * 0x85EBCA6Bu);
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
            return x;
        }
    }
}
