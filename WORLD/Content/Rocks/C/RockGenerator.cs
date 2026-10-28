// Eleven low-poly rock silhouettes: nine natural scatter stones plus two optional cover boulders.
// Temporary visual geometry stays in C/. The same mesh assets can be reused in top-down and FPS worlds.
using Godot;

public static class RockGenerator
{
    // =========================================================
    // One shared mesh is built per silhouette; per-instance rotation and scaling remain in ContentScatter.
    public static Mesh Create(ContentScatterSettings settings, RandomNumberGenerator random,
        Material material, int variant = 0)
    {
        using var mesh = new ContentMeshBuilder(material);
        int type = ((variant % 11) + 11) % 11;

        // Horizontal footprint and height are RELATIVE to RockMaster.Size.
        // Tiny loose stones through big, readable boulders—not nine copies of the same cone.
        float radiusX = 1f, radiusZ = 1f, height = 0.8f, cap = 0.55f;
        switch (type)
        {
            case 0: radiusX = 0.38f; radiusZ = 0.45f; height = 0.27f; cap = 0.78f; break; // loose stone
            case 1: radiusX = 0.64f; radiusZ = 0.60f; height = 0.64f; cap = 0.52f; break; // fieldstone
            case 2: radiusX = 0.84f; radiusZ = 0.76f; height = 0.88f; cap = 0.61f; break; // angular block
            case 3: radiusX = 1.35f; radiusZ = 0.57f; height = 0.52f; cap = 0.78f; break; // long low stone
            case 4: radiusX = 0.86f; radiusZ = 0.87f; height = 1.17f; cap = 0.40f; break; // broken upright
            case 5: radiusX = 1.19f; radiusZ = 1.08f; height = 0.39f; cap = 0.88f; break; // broad flat slab
            case 6: radiusX = 0.67f; radiusZ = 0.73f; height = 1.57f; cap = 0.36f; break; // tall angular stone
            case 7: radiusX = 1.29f; radiusZ = 1.10f; height = 1.16f; cap = 0.60f; break; // large boulder
            case 8: radiusX = 1.54f; radiusZ = 1.25f; height = 0.91f; cap = 0.72f; break; // heavy outcrop
            case 9: radiusX = 1.55f; radiusZ = 0.92f; height = 1.82f; cap = 0.78f; break; // broad standing-height cover boulder
            case 10: radiusX = 2.05f; radiusZ = 1.05f; height = 1.22f; cap = 0.86f; break; // long crouch-height cover outcrop
        }

        float size = Mathf.Max(0.1f, settings.Size);
        // One rotation-independent asymmetry per variant; per-instance yaw/scale is applied later.
        radiusX *= size * random.RandfRange(0.90f, 1.12f);
        radiusZ *= size * random.RandfRange(0.88f, 1.12f);
        height *= size * random.RandfRange(0.91f, 1.11f)
            * (type >= 9 ? Mathf.Clamp(settings.RockCoverHeightMultiplier, 0.7f, 1.6f) : 1f);
        int sides = random.RandiRange(7, 11);
        var foot = new Vector3[sides];
        var middle = new Vector3[sides];
        var shoulder = new Vector3[sides];
        var crown = new Vector3[sides];
        float angular = Mathf.Clamp(settings.RockAngularVariation, 0f, 1f);
        float flatness = Mathf.Clamp(settings.RockCrownFlatness, 0f, 1f);
        bool slabProfile = type == 0 || type == 3 || type == 5 || type == 8 || type == 10;
        bool fractureProfile = type == 2 || type == 4 || type == 6 || type == 7 || type == 9 || type == 10;
        int chippedCorner = random.RandiRange(0, sides - 1);
        // Broad flat crowns for slabs, broken upper edges for angular stones; no extra polygons.
        // Cover variants have broad shoulders and enough vertical mass for blocking body-height rays.
        float crownCentreRatio = 1f - flatness * (slabProfile ? 0.10f : 0.05f);
        float bendX = radiusX * random.RandfRange(-0.17f, 0.17f);
        float bendZ = radiusZ * random.RandfRange(-0.17f, 0.17f);

        for (int i = 0; i < sides; i++)
        {
            float angle = (i + random.RandfRange(-0.08f, 0.08f)) * Mathf.Tau / sides;
            float irregularity = random.RandfRange(0.79f, 1.15f);
            float x = Mathf.Cos(angle) * radiusX * irregularity;
            float z = Mathf.Sin(angle) * radiusZ * irregularity;
            float shoulderBump = random.RandfRange(0.82f, 1.15f);
            float facet = random.RandfRange(-0.15f, 0.15f) * angular;
            float chip = fractureProfile && i == chippedCorner ? angular * 0.20f : 0f;
            float shoulderRadius = shoulderBump * 0.87f * (1f + facet - chip);
            float crownRadius = cap * (1f + facet * 0.60f - chip * 0.75f);
            float shoulderY = random.RandfRange(0.68f, 0.82f)
                + random.RandfRange(-0.08f, 0.08f) * angular;
            float roughCrownY = random.RandfRange(0.89f, 0.98f);
            float levelCrownY = crownCentreRatio + random.RandfRange(-0.045f, 0.045f) * angular;
            // The old central peak made broad rocks dome-shaped. This preserves flatter,
            // slightly fractured tops without increasing mesh triangles or collider count.
            float crownY = Mathf.Lerp(roughCrownY, levelCrownY, flatness);
            if (chip > 0f) crownY -= chip * 0.20f;
            // Below-ground skirt conceals small terrain sampling differences.
            foot[i] = new Vector3(x * 0.80f, -0.13f * size, z * 0.80f);
            middle[i] = new Vector3(x, height * random.RandfRange(0.22f, 0.36f), z);
            shoulder[i] = new Vector3(x * shoulderRadius + bendX * 0.45f,
                height * shoulderY, z * shoulderRadius + bendZ * 0.45f);
            crown[i] = new Vector3(x * crownRadius + bendX, height * crownY,
                z * crownRadius + bendZ);
        }

        var centre = new Vector3(bendX * 0.45f, height * 0.40f, bendZ * 0.45f);
        var crownCentre = new Vector3(bendX, height * crownCentreRatio, bendZ);
        var bottomCentre = new Vector3(0, -0.13f * size, 0);
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            var colour = settings.PrimaryColor.Lerp(settings.AccentColor, random.RandfRange(0.08f, 0.48f));
            // Four separated angular tiers make a rugged silhouette with a broad crown, not a cone.
            Connect(mesh, foot[i], foot[next], middle[i], middle[next], colour * 0.75f, centre);
            Connect(mesh, middle[i], middle[next], shoulder[i], shoulder[next], colour, centre);
            Connect(mesh, shoulder[i], shoulder[next], crown[i], crown[next], colour * 1.04f, centre);
            mesh.RockFace(crown[i], crownCentre, crown[next], colour * 1.08f, centre);
            mesh.RockFace(bottomCentre, foot[i], foot[next], colour * 0.70f, centre);
        }
        return mesh.Finish();
    }

    // =========================================================
    // Two flat-shaded triangles per angular side; outward normals are fixed by ContentMeshBuilder.
    private static void Connect(ContentMeshBuilder mesh, Vector3 lowA, Vector3 lowB,
        Vector3 highA, Vector3 highB, Color colour, Vector3 centre)
    {
        mesh.RockFace(lowA, highA, lowB, colour, centre);
        mesh.RockFace(lowB, highA, highB, colour * 0.93f, centre);
    }
}
