// DTP1 visibility fix: precise conservative pads for the current authored village, not a giant no-dirt rectangle.
// This offline authoring helper mirrors the surveyed site, road and building layout; no runtime simulation ownership.
using Godot;
using System.Collections.Generic;

public static class DirtPatchProtectedAreas
{
    // =========================================================
    // Reserve real site/structure footprints, access roads, robot route and player arrival.
    // Recheck these values against VillageLayout whenever a building/site is relocated or resized.
    public static List<Rect2> Build(WorldSettings settings)
    {
        var areas = new List<Rect2>(32);
        if (!settings.VillageEnabled) return areas;
        Vector2 origin = settings.VillageOriginXZ;

        // PARK3 is 56x36m; the extended store, rear service access and driveway make its full footprint longer.
        // The old 74x96m rectangle extended far beyond the asphalt and removed the
        // small grassy corridor before spawn. The current store/lot spans roughly
        // X +/-28m, Z -38..+24m, with the narrow driveway guarded separately below.
        AddPad(areas, origin, new Vector2(32f, 32f), new Vector2(0f, -9f));
        AddPad(areas, origin, new Vector2(17f, 15f), settings.Building001Offset); // Depot 20x14m
        AddPad(areas, origin, new Vector2(17f, 17f), settings.Building002Offset); // Lodge, porch and stairs
        AddPad(areas, origin, new Vector2(22f, 28f), settings.Building003Offset); // Gas station and forecourt
        AddPad(areas, origin, new Vector2(17f, 16f), settings.Building004Offset); // Field office

        // Keep the native road surfaces/shoulders clear. Width includes a small authoring safety buffer.
        float roadWidth = Mathf.Clamp(settings.VillageRoadWidth, 4f, 8f) + 9f;
        Connect(areas, origin, new Vector2(0, 24), new Vector2(0, 38), roadWidth);
        Connect(areas, origin, new Vector2(0, 35), new Vector2(35, 35), roadWidth);
        Connect(areas, origin, new Vector2(35, 35), new Vector2(35, -165), roadWidth);
        ConnectBuilding(areas, origin, settings.Building001Offset, 11f, roadWidth);
        ConnectBuilding(areas, origin, settings.Building002Offset, 11f, roadWidth);
        ConnectBuilding(areas, origin, settings.Building003Offset, 23f, roadWidth);
        ConnectBuilding(areas, origin, settings.Building004Offset, 11f, roadWidth);

        // The robot's current closed route is centred at (-115,-50), 40x90m plus margin.
        AddPad(areas, origin, new Vector2(33f, 57f), new Vector2(-115f, -50f));
        AddPad(areas, settings.SpawnXZ, new Vector2(10f, 10f), Vector2.Zero);
        return areas;
    }

    // =========================================================
    public static bool Contains(List<Rect2> areas, Vector2 point)
    {
        foreach (Rect2 area in areas) if (area.HasPoint(point)) return true;
        return false;
    }

    private static void AddPad(List<Rect2> areas, Vector2 origin, Vector2 half, Vector2 offset)
    {
        Vector2 p = origin + offset;
        areas.Add(new Rect2(p - half, half * 2f));
    }

    private static void ConnectBuilding(List<Rect2> areas, Vector2 origin, Vector2 building, float front, float width)
    {
        float targetZ = building.Y + front;
        float junction = Mathf.Max(-165f, targetZ);
        Connect(areas, origin, new Vector2(35f, junction), new Vector2(building.X, targetZ), width);
    }

    // =========================================================
    // Mirrors VillageLayout.AddRoad's two axis-aligned legs with positive-area rectangles.
    private static void Connect(List<Rect2> areas, Vector2 origin, Vector2 from, Vector2 to, float width)
    {
        Vector2 corner = new(to.X, from.Y);
        AddLeg(areas, origin + from, origin + corner, width);
        AddLeg(areas, origin + corner, origin + to, width);
    }

    private static void AddLeg(List<Rect2> areas, Vector2 a, Vector2 b, float width)
    {
        if (a.DistanceSquaredTo(b) < .01f) return;
        Vector2 half = Vector2.One * (width * .5f);
        Vector2 low = a.Min(b) - half, high = a.Max(b) + half;
        areas.Add(new Rect2(low, high - low));
    }
}
