// DF0: bounded startup-only forest footprint survey, pine reservation and optional visual outline.
// No world terrain writes, physics queries, scene scans, streaming loop or new tree generation.
using Godot;

public static class ForestZone
{
    // =========================================================
    // Survey the full footprint + feather band, existing reserved scenery and a walk-in approach.
    // On failure, the caller must NOT exclude existing pines.
    public static bool TrySurvey(GodotObject data, ForestSettings settings, WorldSettings world,
        Godot.Collections.Array<Rect2> reserved, out Rect2 footprint, out string report)
    {
        footprint = default;
        report = "";
        Vector2 center = settings.CenterXZ, size = settings.SizeXZ;
        if (!center.IsFinite() || !size.IsFinite() || size.X < 16f || size.Y < 16f
            || size.X > 192f || size.Y > 320f)
        { report = "Invalid centre or size (width 16–192m, north/south length 16–320m)."; return false; }
        footprint = new Rect2(center - size * 0.5f, size);
        float band = Mathf.Clamp(settings.TransitionWidth, 0f, 24f);
        Rect2 sampleArea = footprint.Grow(band);
        const float edgeMargin = 3f;
        if (sampleArea.Position.X < edgeMargin || sampleArea.Position.Y < edgeMargin
            || sampleArea.End.X >= WorldSettings.TerrainSize - edgeMargin
            || sampleArea.End.Y >= WorldSettings.TerrainSize - edgeMargin)
        { report = "Footprint/transition leaves the 1024m gameplay terrain."; return false; }

        // DF2.5: a long forest can border village lots or roads. Those reservations exclude
        // individual tree candidates; a small edge overlap must not reject the whole strip.
        int valid = 0, steep = 0, total = 0, excluded = 0;
        float min = float.PositiveInfinity, max = float.NegativeInfinity, worstSlope = 0f;
        float step = Mathf.Clamp(settings.SurveyStep, 2f, 12f);
        float allowedSlope = Mathf.Clamp(settings.MaximumSlope, 0.1f, 1.5f);
        int nx = Mathf.CeilToInt(sampleArea.Size.X / step);
        int nz = Mathf.CeilToInt(sampleArea.Size.Y / step);
        for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
        {
            Vector2 p = sampleArea.Position + new Vector2(sampleArea.Size.X * x / nx, sampleArea.Size.Y * z / nz);
            if (IsReserved(p, reserved)) { excluded++; continue; }
            if (!TryTerrainSample(data, p, out float h, out float slope))
            { report = $"Missing Terrain3D data at XZ {p}."; return false; }
            min = Mathf.Min(min, h); max = Mathf.Max(max, h);
            worstSlope = Mathf.Max(worstSlope, slope); total++;
            if (slope > allowedSlope) steep++; else valid++;
        }
        // Preserve enough actual forest area; don't approve a strip dominated by reservations.
        if (total < Mathf.CeilToInt((total + excluded) * 0.4f))
        { report = $"Too little open forest ground ({total}/{total + excluded} free samples)."; return false; }
        if (valid < Mathf.CeilToInt(total * 0.8f))
        { report = $"Terrain too steep over {steep}/{total} open samples (slope limit {allowedSlope:F2})."; return false; }

        // The original village-side forest retains its historical access route test.
        // Independent woods survey local terrain, without requiring a cross-map straight path.
        float length = 0f;
        if (settings.RequireVillageApproach)
        {
            float entranceZ = Mathf.Clamp(settings.TrailEntryZ,
                footprint.Position.Y + 8f, footprint.End.Y - 8f);
            Vector2 entrance = new(footprint.Position.X, entranceZ);
            Vector2 approach = world.VillageEnabled ? world.VillageOriginXZ + new Vector2(35, 35) : world.SpawnXZ;
            length = approach.DistanceTo(entrance);
            int sections = Mathf.Max(1, Mathf.CeilToInt(length / 4f));
            for (int i = 0; i <= sections; i++)
            {
                Vector2 p = approach.Lerp(entrance, (float)i / sections);
                if (!TryTerrainSample(data, p, out _, out float slope) || slope > allowedSlope)
                { report = $"Village-to-forest approach not walkable at XZ {p} (max slope {allowedSlope:F2})."; return false; }
            }
        }
        report = $"XZ {center}, size {size}, extent {footprint}, terrain {min:F1}–{max:F1}m, "
            + $"slope max {worstSlope:F2}, suitable {valid}/{total}, reserved {excluded}, "
            + (settings.RequireVillageApproach ? $"access {length:F0}m." : "independent woodland.");
        return true;
    }

    // =========================================================
    // Shared point test: retain village buildings, roads, robot paths and spawn clearings.
    private static bool IsReserved(Vector2 point, Godot.Collections.Array<Rect2> reserved)
    {
        foreach (Rect2 blocked in reserved)
            if (blocked.HasPoint(point)) return true;
        return false;
    }

    // =========================================================
    // Reuse the saved Terrain3D height API. Two 2m-neighbour samples approximate a local grade.
    private static bool TryTerrainSample(GodotObject data, Vector2 p, out float height, out float slope)
    {
        height = data.Call("get_height", new Vector3(p.X, 0, p.Y)).AsSingle();
        float east = data.Call("get_height", new Vector3(p.X + 2, 0, p.Y)).AsSingle();
        float south = data.Call("get_height", new Vector3(p.X, 0, p.Y + 2)).AsSingle();
        slope = new Vector2(east - height, south - height).Length() * 0.5f;
        return float.IsFinite(height) && float.IsFinite(east) && float.IsFinite(south) && float.IsFinite(slope);
    }

    // =========================================================
    // Optional startup-only terrain-following line perimeter. No CPU callbacks, collision or camera effect.
    public static void ShowFootprint(Node3D parent, GodotObject data, Rect2 area)
    {
        var material = new StandardMaterial3D {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.18f, 1f, 0.37f), NoDepthTest = true
        };
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
        Vector2 a = area.Position, b = area.End;
        Edge(mesh, data, new Vector2(a.X, a.Y), new Vector2(b.X, a.Y));
        Edge(mesh, data, new Vector2(b.X, a.Y), new Vector2(b.X, b.Y));
        Edge(mesh, data, new Vector2(b.X, b.Y), new Vector2(a.X, b.Y));
        Edge(mesh, data, new Vector2(a.X, b.Y), new Vector2(a.X, a.Y));
        mesh.SurfaceEnd();
        var outline = new MeshInstance3D {
            Name = "DenseForestDF0Outline", Mesh = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        outline.SetMeta("world_render_exempt", true);
        parent.AddChild(outline);
    }

    // =========================================================
    // Bounded 4m sections avoid drawing a straight border underground on sloped terrain.
    private static void Edge(ImmediateMesh mesh, GodotObject data, Vector2 start, Vector2 end)
    {
        int count = Mathf.Max(1, Mathf.CeilToInt(start.DistanceTo(end) / 4f));
        for (int i = 0; i < count; i++)
        {
            Vector2 a = start.Lerp(end, (float)i / count), b = start.Lerp(end, (float)(i + 1) / count);
            float ay = data.Call("get_height", new Vector3(a.X, 0, a.Y)).AsSingle();
            float by = data.Call("get_height", new Vector3(b.X, 0, b.Y)).AsSingle();
            if (!float.IsFinite(ay) || !float.IsFinite(by)) continue;
            mesh.SurfaceAddVertex(new Vector3(a.X, ay + 0.3f, a.Y));
            mesh.SurfaceAddVertex(new Vector3(b.X, by + 0.3f, b.Y));
        }
    }
}
