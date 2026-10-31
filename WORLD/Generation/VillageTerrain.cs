// One bounded runtime height-image pass: flat village platforms, dirt roads and varied outskirts.
// Copies native height images; never saves terrain resources or bakes on gameplay launch.
using Godot;

public static class VillageTerrain
{
    // =========================================================
    // Keep every pixel of each platform flat, feather roads/shoulders, and reserve the full robot corridor.
    public static bool Apply(Node3D terrain, GodotObject data, WorldSettings settings, VillageLayout layout)
    {
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        if (!float.IsFinite(spacing) || spacing <= 0) { GD.PushError("Village requires valid terrain spacing."); return false; }
        Vector2 origin = settings.VillageOriginXZ;
        Rect2 area = new(origin + new Vector2(-240, -340), new Vector2(480, 500));
        using var noise = new FastNoiseLite { Seed = settings.Seed + 6011, Frequency = 0.028f };
        float outskirtsScale = settings.VillageOutskirtsEnabled
            ? Mathf.Clamp(settings.VillageOutskirtsScale, 0f, 2f) : 0f;
        int changed = 0;
        foreach (Variant item in data.Get("region_locations").AsGodotArray())
        {
            Vector2I location = item.AsVector2I();
            var region = data.Call("get_region", location).AsGodotObject();
            var source = (Image)region.Call("get_map", 0).AsGodotObject();
            int size = source.GetWidth();
            Vector2 corner = new(location.X * size * spacing, location.Y * size * spacing);
            if (!area.Intersects(new Rect2(corner, Vector2.One * size * spacing))) continue;
            var image = (Image)source.Duplicate();
            int minX = Mathf.Clamp(Mathf.FloorToInt((area.Position.X - corner.X) / spacing), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((area.End.X - corner.X) / spacing), 0, size - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt((area.Position.Y - corner.Y) / spacing), 0, size - 1);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt((area.End.Y - corner.Y) / spacing), 0, size - 1);
            for (int z = minZ; z <= maxZ; z++) for (int x = minX; x <= maxX; x++)
            {
                Vector2 world = corner + new Vector2(x, z) * spacing, local = world - origin;
                Color pixel = image.GetPixel(x, z);
                float protection = 0, grade = 0;
                // Extra native-vertex padding keeps interpolated ground flat at surveyed edges.
                foreach (Rect2 platform in layout.Platforms)
                    grade = Mathf.Max(grade, Weight(world, platform.Grow(spacing * 1.5f), settings.VillageBlendWidth));
                foreach (Rect2 road in layout.Roads)
                    grade = Mathf.Max(grade, Weight(world, road.Grow(spacing), settings.VillageBlendWidth));
                foreach (Rect2 corridor in layout.Patrol)
                    protection = Mathf.Max(protection, Weight(world, corridor, 16));
                float variation = noise.GetNoise2D(world.X, world.Y);
                float hills = Hill(local, new(-70, 55), 45, 5)
                    + Hill(local, new(105, 20), 52, 6) + Hill(local, new(0, -245), 65, 9);
                hills *= Mathf.Clamp(settings.VillageHillScale, 0, 2) * (1 + variation * 0.13f);
                // VW1: additional broad ridges and shallow basins only outside roads,
                // village platforms and the entire robot body/feet patrol corridor.
                float outskirts = outskirtsScale > 0f
                    ? Outskirts(local) * outskirtsScale * EdgeFade(local) * (1 + variation * 0.18f)
                    : 0f;
                float value = pixel.R + (hills + outskirts) * (1 - protection) * (1 - grade);
                value = Mathf.Lerp(value, layout.Level, grade);
                if (Mathf.Abs(value - pixel.R) < 0.00001f) continue;
                pixel.R = value; image.SetPixel(x, z, pixel); changed++;
            }
            region.Call("set_map", 0, image); region.Call("calc_height_range"); region.Set("edited", true);
        }
        if (changed == 0) return layout.ValidateSupport(data);
        data.Call("calc_height_range", false); data.Call("update_maps", 0, true, false);
        GD.Print($"Village graded {changed} native samples; outskirts "
            + $"{(outskirtsScale > 0f ? $"ON x{outskirtsScale:F1}" : "OFF")}; "
            + "collision rebuild follows once; saved files unchanged.");
        return layout.ValidateSupport(data);
    }

    // =========================================================
    // A protected rectangle plus a smooth distance shoulder, not an abrupt raised platform edge.
    private static float Weight(Vector2 p, Rect2 area, float blend)
    {
        Vector2 near = p.Clamp(area.Position, area.End);
        float t = Mathf.Clamp(p.DistanceTo(near) / Mathf.Clamp(blend, 6, 30), 0, 1);
        return 1 - t * t * (3 - 2 * t);
    }

    // =========================================================
    // VW1: broad, partially overlapping landforms around the village outskirts.
    // Offsets are relative to VillageOriginXZ; existing patrol/road/platform masks
    // are applied to the combined shape in Apply(), not separately per landform.
    private static float Outskirts(Vector2 p)
    {
        return Hill(p, new(-185f, -210f), 74f, 7.0f)
            + Hill(p, new(-82f, -282f), 77f, 6.5f)
            + Hill(p, new(160f, -272f), 78f, 7.5f)
            + Hill(p, new(180f, -98f), 66f, 4.8f)
            - Hill(p, new(-200f, -45f), 64f, 3.3f)
            - Hill(p, new(82f, 122f), 68f, 3.2f)
            - Hill(p, new(135f, -196f), 58f, 2.5f);
    }

    // =========================================================
    // Fade the new relief to zero at each border of the existing 480x500m
    // height-image edit, preventing a visible step at the untouched terrain.
    private static float EdgeFade(Vector2 p)
    {
        float border = Mathf.Min(Mathf.Min(p.X + 240f, 240f - p.X),
            Mathf.Min(p.Y + 340f, 160f - p.Y));
        float t = Mathf.Clamp(border / 28f, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    // =========================================================
    // Smooth rounded hill with cheap radius rejection outside its footprint.
    private static float Hill(Vector2 p, Vector2 center, float radius, float height)
    {
        float distanceSq = p.DistanceSquaredTo(center);
        if (distanceSq >= radius * radius) return 0f;
        float t = 1f - Mathf.Sqrt(distanceSq) / radius;
        return height * t * t * (3f - 2f * t);
    }
}
