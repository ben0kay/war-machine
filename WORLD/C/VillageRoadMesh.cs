// Startup-only dirt road visuals over already-graded native terrain; physical support remains Terrain3D.
using Godot;
using System.Collections.Generic;

public static class VillageRoadMesh
{
    // =========================================================
    // The dirt road and each merged sidewalk finish have their own material.
    // Pedestrian support is Terrain3D; roadside lamps retain prefab collision.
    public static void Build(Node3D parent, VillageLayout layout, WorldSettings settings)
    {
        List<Rect2> roads = layout.Roads;
        float height = layout.Level;
        using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (Rect2 road in roads)
        {
            Vector3 a = new(road.Position.X, height + 0.006f, road.Position.Y);
            Vector3 b = new(road.End.X, height + 0.006f, road.Position.Y);
            Vector3 c = new(road.End.X, height + 0.006f, road.End.Y);
            Vector3 d = new(road.Position.X, height + 0.006f, road.End.Y);
            foreach (Vector3 p in new[] { a, b, c, a, c, d })
            { surface.SetNormal(Vector3.Up); surface.AddVertex(p); }
        }
        parent.AddChild(new MeshInstance3D { Name = "VillageRoad", Mesh = surface.Commit(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.27f, 0.245f, 0.20f),
                Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });

        if (layout.Sidewalks.Count == 0) return;
        using var sidewalk = new SurfaceTool(); sidewalk.Begin(Mesh.PrimitiveType.Triangles);
        using var curb = new SurfaceTool(); curb.Begin(Mesh.PrimitiveType.Triangles);
        foreach (Rect2 strip in layout.Sidewalks)
        {
            AddRect(sidewalk, strip, height + 0.022f);
            AddRect(curb, new Rect2(strip.Position, new Vector2(0.09f, strip.Size.Y)),
                height + 0.036f);
        }
        parent.AddChild(new MeshInstance3D { Name = "VillageSidewalk",
            Mesh = sidewalk.Commit(), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.43f, 0.45f, 0.45f),
                Roughness = 0.96f, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
        parent.AddChild(new MeshInstance3D { Name = "SidewalkCurbEdge",
            Mesh = curb.Commit(), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.32f, 0.35f, 0.36f),
                Roughness = 0.98f, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });

        // The exact same reusable fixture and night schedule as the store lot.
        if (layout.SidewalkLampPositions.Count == 0) return;
        var fixture = GD.Load<PackedScene>(
            "res://WORLD/Content/Assets/Objects/Static/Lighting/StreetLamp001/StreetLamp001.tscn");
        if (fixture == null) { GD.PushWarning("Village streetlamp prefab is missing."); return; }
        var lamps = new Node3D { Name = "VillageStreetLamps" };
        parent.AddChild(lamps);
        for (int i = 0; i < layout.SidewalkLampPositions.Count; i++)
        {
            Vector2 point = layout.SidewalkLampPositions[i];
            var lamp = fixture.Instantiate<Node3D>();
            lamp.Name = $"RoadsideLamp_{i + 1:00}";
            lamp.Position = new Vector3(point.X, height, point.Y);
            lamp.Rotation = new Vector3(0, -Mathf.Pi * 0.5f, 0);
            lamps.AddChild(lamp);
        }
    }

    // =========================================================
    // One merged visual-only flat concrete mesh, no new collision.
    private static void AddRect(SurfaceTool tool, Rect2 rect, float y)
    {
        Vector3 a = new(rect.Position.X, y, rect.Position.Y);
        Vector3 b = new(rect.End.X, y, rect.Position.Y);
        Vector3 c = new(rect.End.X, y, rect.End.Y);
        Vector3 d = new(rect.Position.X, y, rect.End.Y);
        foreach (Vector3 p in new[] { a, b, c, a, c, d })
        { tool.SetNormal(Vector3.Up); tool.AddVertex(p); }
    }
}
