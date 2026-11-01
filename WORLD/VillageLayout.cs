// Startup-only village composition: prefab bounds, full-footprint survey and shared spatial reservations.
// Scenery stays reusable; terrain owns support and actors remain dormant until collision is ready.
using Godot;
using System.Collections.Generic;

public sealed class VillageLayout
{
    public readonly Godot.Collections.Array<Rect2> Clearings = new(), GrassClearings = new(), Structures = new();
    public readonly List<Rect2> Platforms = new(), Roads = new(), Patrol = new();
    public readonly List<Rect2> Sidewalks = new();
    public readonly List<Vector2> SidewalkLampPositions = new();
    private const float VillageSpineX = 35f, VillageSpineNorth = 35f, VillageSpineSouth = -165f;
    public Node3D Site { get; private set; } = null!;
    public float Level { get; private set; }
    private readonly List<Node3D> _buildings = new();

    // =========================================================
    // The authored site is unique. Existing buildings instantiate once around the independent village origin.
    public bool Prepare(Node3D root, RobotPlacement robot, GodotObject data, WorldSettings settings)
    {
        Site = root.GetNode<Node3D>("ConvenienceStoreSite001");
        Site.Position = new Vector3(settings.VillageOriginXZ.X, 0, settings.VillageOriginXZ.Y);
        _buildings.Add(Site);
        foreach (var pair in new[] { ("Building001", settings.Building001Offset), ("Building002", settings.Building002Offset),
            ("Building003", settings.Building003Offset), ("Building004", settings.Building004Offset) })
        {
            var scene = GD.Load<PackedScene>($"res://WORLD/Content/Assets/Structures/{pair.Item1}/{pair.Item1}.tscn");
            var building = scene.Instantiate<Node3D>(); building.Name = pair.Item1;
            Vector2 point = settings.VillageOriginXZ + pair.Item2;
            building.Position = new Vector3(point.X, 0, point.Y); root.AddChild(building); _buildings.Add(building);
        }
        foreach (var building in _buildings)
        {
            Rect2 footprint = Bounds(building);
            if (!float.IsFinite(footprint.Size.X) || !float.IsFinite(footprint.Size.Y)
                || footprint.Size.X <= 0 || footprint.Size.Y <= 0)
            { GD.PushError($"Village prefab has no valid generated bounds: {building.Name}."); return false; }
            foreach (Rect2 occupied in Structures)
                if (occupied.Grow(3).Intersects(footprint.Grow(3)))
                { GD.PushError($"Village footprint overlap: {building.Name}; adjust WorldMaster offsets."); return false; }
            Structures.Add(footprint); Platforms.Add(footprint.Grow(2));
        }
        // Rear service access and lot driveway remain part of the protected full-site platform.
        Vector3 roadPoint = Site.GetNode<Node3D>("SiteMarkers/RoadConnection").GlobalPosition;
        Vector3 rearPoint = Site.GetNode<Node3D>("SiteMarkers/RearServiceExit").GlobalPosition;
        Vector2 roadXZ = new(roadPoint.X, roadPoint.Z), rearXZ = new(rearPoint.X, rearPoint.Z);
        Platforms[0] = Platforms[0].Merge(new Rect2(roadXZ - new Vector2(6, 2), new Vector2(12, 6)))
            .Merge(new Rect2(rearXZ - Vector2.One * 3, Vector2.One * 6));
        AddRoad(settings.VillageOriginXZ, roadXZ - settings.VillageOriginXZ, new(0, 38), settings.VillageRoadWidth);
        AddRoad(settings.VillageOriginXZ, new(0, 35), new(35, 35), settings.VillageRoadWidth);
        AddRoad(settings.VillageOriginXZ, new(VillageSpineX, VillageSpineNorth),
            new(VillageSpineX, VillageSpineSouth), settings.VillageRoadWidth);
        for (int i = 1; i < _buildings.Count; i++)
        {
            Vector2 target = new(_buildings[i].Position.X - settings.VillageOriginXZ.X,
                Structures[i].End.Y - settings.VillageOriginXZ.Y + 4);
            float junction = Mathf.Max(-165, target.Y);
            AddRoad(settings.VillageOriginXZ, new Vector2(35, junction), target, settings.VillageRoadWidth);
        }
        for (int roadIndex = 0; roadIndex < Roads.Count; roadIndex++)
            for (int structureIndex = 0; structureIndex < Structures.Count; structureIndex++)
                if (!(roadIndex == 0 && structureIndex == 0) && Roads[roadIndex].Intersects(Structures[structureIndex]))
                { GD.PushError("Village road crosses a structure/site. Adjust layout instead of grading through it."); return false; }
        // Share the village grading/exclusion pass, and cut the concrete path at
        // east-facing driveways rather than covering them with sidewalk mesh.
        if (settings.VillageSidewalkEnabled) BuildVillageSidewalk(settings);
        foreach (Rect2 corridor in robot.GetRouteReservations(settings, 10)) Patrol.Add(corridor);
        foreach (Rect2 corridor in Patrol)
            foreach (Rect2 occupied in Structures)
                if (corridor.Intersects(occupied.Grow(2)))
                { GD.PushError("Village patrol corridor overlaps scenery. Move the patrol markers."); return false; }
        var heights = new List<float>();
        if (!Survey(data, Platforms[0], heights)) return false;
        heights.Sort(); Level = heights[heights.Count / 2];
        float min = heights[0], max = heights[^1];
        foreach (Rect2 area in Platforms)
            if (!ValidateGrade(data, area, settings.VillageMaxCutFill)) return false;
        foreach (Rect2 area in Roads)
            if (!ValidateGrade(data, area, settings.VillageMaxCutFill)) return false;
        foreach (var building in _buildings) building.Position = new Vector3(building.Position.X, Level, building.Position.Z);
        foreach (Rect2 area in Platforms) { GrassClearings.Add(area.Grow(0.3f)); Clearings.Add(area.Grow(3)); }
        foreach (Rect2 area in Roads) { GrassClearings.Add(area.Grow(0.4f)); Clearings.Add(area.Grow(3)); }
        foreach (Rect2 area in Patrol) Clearings.Add(area);
        foreach (Vector2 lamp in SidewalkLampPositions)
        {
            var baseArea = new Rect2(lamp - Vector2.One * 0.65f, Vector2.One * 1.30f);
            GrassClearings.Add(baseArea);
            Clearings.Add(baseArea.Grow(1.2f));
        }
        GD.Print($"Village survey: site min={min:F2}, max={max:F2}, target={Level:F2}; full grid <=2m incl. edges. Origin={settings.VillageOriginXZ}.");
        return true;
    }

    // =========================================================
    // Startup support/road measurements are now value data; streamed buildings must not remain referenced here.
    public void ReleaseStartupBuildingReferences() => _buildings.Clear();

    // =========================================================
    // Reusable art is built synchronously at Ready; collect actual transformed corners once, not per frame.
    public static Rect2 Bounds(Node3D root)
    {
        Vector2 low = new(float.PositiveInfinity, float.PositiveInfinity), high = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Node child in root.FindChildren("*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)child;
            if (mesh.Mesh == null) continue;
            Aabb bounds = mesh.GetAabb();
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = mesh.GlobalTransform * bounds.GetEndpoint(i);
                Vector2 xz = new(p.X, p.Z); low = low.Min(xz); high = high.Max(xz);
            }
        }
        return new Rect2(low, high - low);
    }

    // =========================================================
    // Short axis-aligned dirt connections, joined with overlapping ends; no general road framework.
    private void AddRoad(Vector2 origin, Vector2 a, Vector2 b, float width)
    {
        width = Mathf.Clamp(width, 4, 8);
        Vector2 corner = new(b.X, a.Y);
        foreach (var pair in new[] { (a, corner), (corner, b) })
        {
            if (pair.Item1.DistanceSquaredTo(pair.Item2) < 0.01f) continue;
            Vector2 low = pair.Item1.Min(pair.Item2) - Vector2.One * width * 0.5f;
            Vector2 high = pair.Item1.Max(pair.Item2) + Vector2.One * width * 0.5f;
            Roads.Add(new Rect2(origin + low, high - low));
        }
    }

    // =========================================================
    // One eastern concrete sidewalk along the established north/south road.
    // Splits are derived from the existing road rectangles, not a second
    // hard-coded list of store/gas station entrance coordinates.
    private void BuildVillageSidewalk(WorldSettings settings)
    {
        Vector2 origin = settings.VillageOriginXZ;
        float halfRoad = Mathf.Clamp(settings.VillageRoadWidth, 4f, 8f) * 0.5f;
        float width = Mathf.Clamp(settings.VillageSidewalkWidth, 1.5f, 3f);
        float leftX = origin.X + VillageSpineX + halfRoad + 0.04f;
        float south = origin.Y + VillageSpineSouth, north = origin.Y + VillageSpineNorth;
        var intersections = new List<(float Start, float End)>();
        foreach (Rect2 branch in Roads)
        {
            // The main vertical road (and any branch heading west) stops
            // short of this x-range. Only actual east-side crossings cut it.
            if (branch.End.X <= leftX || branch.Position.X >= leftX + width
                || branch.End.Y <= south || branch.Position.Y >= north) continue;
            intersections.Add((Mathf.Max(south, branch.Position.Y - 0.65f),
                Mathf.Min(north, branch.End.Y + 0.65f)));
        }
        intersections.Sort((a, b) => a.Start.CompareTo(b.Start));
        float cursor = south;
        foreach (var crossing in intersections)
        {
            AddSidewalkSection(leftX, width, cursor, crossing.Start);
            cursor = Mathf.Max(cursor, crossing.End);
        }
        AddSidewalkSection(leftX, width, cursor, north);

        // Fixed 28m-style cadence with deliberate gaps at junctions;
        // don't shift lamps into a driveway just to preserve their count.
        if (!settings.VillageStreetLampsEnabled) return;
        float spacing = Mathf.Clamp(settings.VillageLampSpacing, 18f, 40f);
        float poleX = leftX + width + 0.36f; // verge; walking strip stays free
        for (float z = south + 13f; z <= north - 13f; z += spacing)
        {
            bool allowed = false;
            foreach (Rect2 section in Sidewalks)
                if (z >= section.Position.Y + 1.8f && z <= section.End.Y - 1.8f)
                { allowed = true; break; }
            if (!allowed) continue;
            Vector2 point = new(poleX, z);
            foreach (Rect2 occupied in Structures)
                if (occupied.Grow(1f).HasPoint(point)) { allowed = false; break; }
            if (allowed) SidewalkLampPositions.Add(point);
        }
    }

    // =========================================================
    // Visual-only concrete, real terrain support. Adding it to the authored
    // platforms makes the normal grade and grass clearings cover its width.
    private void AddSidewalkSection(float x, float width, float fromZ, float toZ)
    {
        if (toZ - fromZ < 2f) return;
        var section = new Rect2(new Vector2(x, fromZ),
            new Vector2(width, toZ - fromZ));
        foreach (Rect2 occupied in Structures)
            if (section.Intersects(occupied.Grow(0.4f)))
            { GD.PushWarning("Village sidewalk span omitted near building footprint."); return; }
        Sidewalks.Add(section);
        Platforms.Add(section);
    }

    // =========================================================
    // Include all perimeter endpoints and a <=2m interior grid; NaN/missing terrain never passes.
    private static bool Survey(GodotObject data, Rect2 area, List<float> heights)
    {
        int columns = Mathf.CeilToInt(area.Size.X / 2), rows = Mathf.CeilToInt(area.Size.Y / 2);
        for (int z = 0; z <= rows; z++) for (int x = 0; x <= columns; x++)
        {
            Vector2 p = area.Position + area.Size * new Vector2((float)x / columns, (float)z / rows);
            float height = data.Call("get_height", new Vector3(p.X, 0, p.Y)).AsSingle();
            if (!float.IsFinite(height)) { GD.PushError("Village footprint extends beyond valid saved terrain."); return false; }
            heights.Add(height);
        }
        return heights.Count > 0;
    }

    // =========================================================
    private bool ValidateGrade(GodotObject data, Rect2 area, float maximum)
    {
        var heights = new List<float>();
        if (!Survey(data, area, heights)) return false;
        foreach (float h in heights)
            if (Mathf.Abs(h - Level) > Mathf.Clamp(maximum, 0.5f, 12))
            { GD.PushError("Village cut/fill limit exceeded. Relocate layout or explicitly adjust its grading limit."); return false; }
        return true;
    }

    // =========================================================
    // Validate the complete platform again after native edits, not merely at the root or four corners.
    public bool ValidateSupport(GodotObject data)
    {
        foreach (Rect2 area in Platforms)
        {
            var heights = new List<float>(); if (!Survey(data, area, heights)) return false;
            foreach (float height in heights) if (Mathf.Abs(height - Level) > 0.002f)
            { GD.PushError("Village platform support tolerance failed; actors will remain dormant."); return false; }
        }
        return true;
    }
}
