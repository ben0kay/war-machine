// Startup-only dirt road visuals over already-graded native terrain; physical support remains Terrain3D.
using Godot;
using System.Collections.Generic;

public static class VillageRoadMesh
{
    // =========================================================
    // One mesh with rectangle UVs; shared earthy material and no colliders, polling or extra lighting owner.
    public static void Build(Node3D parent, List<Rect2> roads, float height)
    {
        using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (Rect2 road in roads)
        {
            Vector3 a = new(road.Position.X, height + 0.025f, road.Position.Y);
            Vector3 b = new(road.End.X, height + 0.025f, road.Position.Y);
            Vector3 c = new(road.End.X, height + 0.025f, road.End.Y);
            Vector3 d = new(road.Position.X, height + 0.025f, road.End.Y);
            foreach (Vector3 p in new[] { a, b, c, a, c, d })
            { surface.SetNormal(Vector3.Up); surface.AddVertex(p); }
        }
        parent.AddChild(new MeshInstance3D { Name = "VillageRoad", Mesh = surface.Commit(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.27f, 0.245f, 0.20f),
                Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
    }
    // =========================================================
    // Small walkable ramps bridge the prefab's 12cm slab edges, without changing its reusable geometry.
    public static void BuildSiteAccess(Node3D parent, Node3D site, float level)
    {
        Vector3 rear = site.GetNode<Node3D>("SiteMarkers/RearServiceExit").GlobalPosition;
        // Marker is 0.55m outside the slab edge; bridge from that edge to the surrounding grade.
        Ramp(parent, rear + Vector3.Back * 0.55f, 2.2f, -2.55f, 0.12f, 0.015f, level, "RearExitRamp");
    }

    // =========================================================
    private static void Ramp(Node3D parent, Vector3 start, float width, float length,
        float innerHeight, float outerHeight, float level, string name)
    {
        Vector3 a = new(start.X - width * 0.5f, level + innerHeight, start.Z);
        Vector3 b = new(start.X + width * 0.5f, level + innerHeight, start.Z);
        Vector3 c = new(b.X, level + outerHeight, start.Z + length);
        Vector3 d = new(a.X, level + outerHeight, start.Z + length);
        using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var face in new[] { (a, b, c), (a, c, d) })
        {
            Vector3 normal = (face.Item2 - face.Item1).Cross(face.Item3 - face.Item1).Normalized();
            bool reverse = normal.Y < 0; if (reverse) normal = -normal;
            foreach (Vector3 p in new[] { face.Item1, reverse ? face.Item3 : face.Item2, reverse ? face.Item2 : face.Item3 })
            { surface.SetNormal(normal); surface.AddVertex(p); }
        }
        var mesh = surface.Commit();
        var body = new StaticBody3D { Name = name, CollisionLayer = 1, CollisionMask = 1 }; parent.AddChild(body);
        body.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = new StandardMaterial3D {
            AlbedoColor = new Color(0.32f, 0.31f, 0.28f), Roughness = 1,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
        body.AddChild(new CollisionShape3D { Shape = mesh.CreateTrimeshShape() });
    }

}
