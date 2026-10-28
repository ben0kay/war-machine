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
    }
}
