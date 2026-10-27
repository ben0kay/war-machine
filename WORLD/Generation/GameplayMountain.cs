// One bounded startup sculpt in native Terrain3D heightmaps: rounded summit, rough cliffs and a curved natural approach.
// Runtime maps/collision are updated once before actors/foliage initialize; saved terrain files are never written.
using Godot;

public static class GameplayMountain
{
    // =========================================================
    // Edit only overlapping regions, using private height-image copies rather than thousands of native per-pixel calls.
    public static Godot.Collections.Array<Rect2> Apply(Node3D terrain, GodotObject data, WorldSettings settings)
    {
        var pathClearings = new Godot.Collections.Array<Rect2>();
        if (!settings.CliffEnabled) return pathClearings;
        Vector2 center = settings.SpawnXZ + settings.CliffOffset;
        float width = Mathf.Clamp(settings.CliffWidth, 32, 64), depth = Mathf.Clamp(settings.CliffDepth, 28, 56);
        float height = Mathf.Clamp(settings.CliffHeight, 6, 18);
        float length = Mathf.Max(height * 3, settings.CliffRampLength);
        float pathWidth = Mathf.Clamp(settings.CliffPathWidth, 8, 14);
        float startZ = -depth * 0.5f - length, endZ = -depth * 0.4f;
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        if (spacing <= 0) { GD.PushError("Mountain sculpt requires positive terrain vertex spacing."); return pathClearings; }
        Rect2 area = new(center + new Vector2(-width * 0.65f, startZ - 2), new Vector2(width * 1.3f, depth * 0.65f - startZ + 4));
        using var noise = new FastNoiseLite { Seed = settings.Seed + 3197, Frequency = 0.075f };
        int changed = 0;
        foreach (Variant item in data.Get("region_locations").AsGodotArray())
        {
            Vector2I location = item.AsVector2I();
            var region = data.Call("get_region", location).AsGodotObject();
            var source = (Image)region.Call("get_map", 0).AsGodotObject();
            int size = source.GetWidth();
            Vector2 origin = new(location.X * size * spacing, location.Y * size * spacing);
            if (!area.Intersects(new Rect2(origin, Vector2.One * size * spacing))) continue;
            var image = (Image)source.Duplicate();
            int minX = Mathf.Clamp(Mathf.FloorToInt((area.Position.X - origin.X) / spacing), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((area.End.X - origin.X) / spacing), 0, size - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt((area.Position.Y - origin.Y) / spacing), 0, size - 1);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt((area.End.Y - origin.Y) / spacing), 0, size - 1);
            for (int z = minZ; z <= maxZ; z++) for (int x = minX; x <= maxX; x++)
            {
                Vector2 world = origin + new Vector2(x, z) * spacing;
                float rise = Height(world - center, width, depth, height, pathWidth, startZ, endZ, noise.GetNoise2D(world.X, world.Y));
                if (rise <= 0) continue;
                Color pixel = image.GetPixel(x, z); pixel.R += rise; image.SetPixel(x, z, pixel); changed++;
            }
            region.Call("set_map", 0, image); region.Call("calc_height_range"); region.Set("edited", true);
        }
        if (changed == 0) { GD.PushError("Mountain sculpt found no saved terrain at its configured position."); return pathClearings; }
        data.Call("calc_height_range", false);
        data.Call("update_maps", 0, true, false);
        // Full-game collision must follow the new native surface before any moving actor is activated.
        terrain.Get("collision").AsGodotObject().Call("build");
        // Short overlapping boxes follow the curved route and a walkable strip across the top; slopes stay vegetated.
        Vector2 previous = center + new Vector2(0, startZ);
        for (int i = 1; i <= 12; i++)
        {
            float t = i / 12f;
            Vector2 next = center + new Vector2(3 * Mathf.Sin(t * Mathf.Pi), Mathf.Lerp(startZ, endZ, t));
            Vector2 low = previous.Min(next) - Vector2.One * (pathWidth * 0.5f);
            Vector2 high = previous.Max(next) + Vector2.One * (pathWidth * 0.5f);
            pathClearings.Add(new Rect2(low, high - low)); previous = next;
        }
        pathClearings.Add(new Rect2(center + new Vector2(-pathWidth * 0.5f, endZ), new Vector2(pathWidth, depth * 0.5f - endZ)));
        GD.Print($"Terrain3D mountain sculpted {changed} height samples at {center}; native collision rebuilt, saved files unchanged.");
        return pathClearings;
    }

    // =========================================================
    // Elliptical, noise-shaped cliffs blend with a broad climbing ridge; the summit is gently domed, not a flat slab.
    private static float Height(Vector2 point, float width, float depth, float height, float pathWidth,
        float startZ, float endZ, float noise)
    {
        float angle = Mathf.Atan2(point.Y, point.X);
        float irregular = 1 + 0.045f * Mathf.Sin(angle * 3 + 0.4f) + 0.025f * Mathf.Sin(angle * 5);
        float radius = new Vector2(point.X / (width * 0.5f), point.Y / (depth * 0.5f)).Length() / irregular;
        float cliff = 1 - Smooth(0.80f, 1, radius);
        float summit = height * cliff * (1 + 0.05f * (1 - Smooth(0, 0.78f, radius)));
        float t = Mathf.Clamp((point.Y - startZ) / (endZ - startZ), 0, 1);
        float bend = 3 * Mathf.Sin(t * Mathf.Pi);
        float shoulder = 1 - Smooth(pathWidth * 0.5f, pathWidth * 0.5f + 6, Mathf.Abs(point.X - bend));
        float approach = point.Y < startZ ? 0 : height * 1.05f * Smooth(0, 1, t) * shoulder;
        // End the approach ridge inside the mountain, never continuing it beyond the far cliff.
        approach *= 1 - Smooth(endZ, endZ + depth * 0.4f, point.Y);
        float rise = Mathf.Max(summit, approach);
        return Mathf.Max(0, rise + noise * 0.22f * Smooth(0, 2, rise));
    }

    // =========================================================
    private static float Smooth(float from, float to, float value)
    { float t = Mathf.Clamp((value - from) / (to - from), 0, 1); return t * t * (3 - 2 * t); }
}
