// One-shot rock plateau with a broad north access ramp and unclimbable cliff faces; no saved terrain edits.
using Godot;

public partial class CliffTestMesh : Node3D
{
    // =========================================================
    // Build once after positioning on sampled terrain; collision follows the exact visual triangles.
    public Godot.Collections.Array<Rect2> Build(float height, float width, float depth, float rampLength, float pathWidth)
    {
        height = Mathf.Clamp(height, 4, 16); width = Mathf.Clamp(width, 18, 40); depth = Mathf.Clamp(depth, 18, 40);
        rampLength = Mathf.Max(height * 2.5f, rampLength); pathWidth = Mathf.Clamp(pathWidth, 4, width * 0.4f);
        float x = width * 0.5f, z = depth * 0.5f, p = pathWidth * 0.5f;
        var material = (ShaderMaterial)GD.Load<ShaderMaterial>("res://WORLD/Content/Rocks/Configuration/StoneMaterial.tres").Duplicate();
        material.SetShaderParameter("moss_amount", 0.22f);
        using var builder = new ContentMeshBuilder(material);
        Vector3[] rim = { new(-p, height, -z), new(p, height, -z), new(x - 2, height, -z + 1),
            new(x, height, -z + 5), new(x - 1, height, z - 3), new(x - 5, height, z),
            new(-x + 3, height, z - 1), new(-x, height, z - 5), new(-x + 1, height, -z + 4), new(-x + 4, height, -z) };
        Vector3 inside = Vector3.Up * (height * 0.5f);
        for (int i = 0; i < rim.Length; i++)
        {
            Vector3 a = rim[i], b = rim[(i + 1) % rim.Length];
            builder.RockFace(new Vector3(0, height, 0), a, b, Colors.White, inside);
            // The first edge is the flush ramp entrance; leave it open instead of adding a wall across the path.
            if (i == 0) continue;
            Vector3 footA = new(a.X * 1.08f, -0.8f, a.Z * 1.08f), footB = new(b.X * 1.08f, -0.8f, b.Z * 1.08f);
            Vector3 mid = (a + b) * 0.5f; mid.Y = height * 0.48f; mid.X *= 1.04f; mid.Z *= 1.04f;
            builder.RockFace(a, mid, b, Colors.White, inside);
            builder.RockFace(a, footA, mid, Colors.White, inside);
            builder.RockFace(mid, footA, footB, Colors.White, inside);
            builder.RockFace(mid, footB, b, Colors.White, inside);
        }
        // Broad, straight ~22-degree path; extends just under terrain at the foot to avoid a raised step.
        Vector3 left = new(-p, -0.25f, -z - rampLength), right = new(p, -0.25f, -z - rampLength);
        Vector3 rampInside = new(0, height * 0.2f, -z - rampLength * 0.4f);
        builder.RockFace(left, right, rim[1], Colors.White, rampInside);
        builder.RockFace(left, rim[1], rim[0], Colors.White, rampInside);
        builder.RockFace(left, rim[0], new Vector3(-p, -0.8f, -z), Colors.White, rampInside);
        builder.RockFace(right, new Vector3(p, -0.8f, -z), rim[1], Colors.White, rampInside);
        var mesh = builder.Finish();
        AddChild(new MeshInstance3D { Name = "PlateauAndPath", Mesh = mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.On });
        var shape = mesh.CreateTrimeshShape(); shape.BackfaceCollision = true;
        var body = new StaticBody3D { Name = "CliffCollision", CollisionLayer = 1, CollisionMask = 0 }; AddChild(body);
        body.AddChild(new CollisionShape3D { Shape = shape });
        SetProcess(false); SetPhysicsProcess(false);
        // Include the ramp foot and whole outcrop, so procedural trees/rocks cannot block the walkable route.
        return new Godot.Collections.Array<Rect2>
        {
            new Rect2(new Vector2(GlobalPosition.X - x * 1.08f - 2, GlobalPosition.Z - z * 1.08f - 2),
                new Vector2(width * 1.08f + 4, depth * 1.08f + 4)),
            new Rect2(new Vector2(GlobalPosition.X - p - 2, GlobalPosition.Z - z - rampLength - 2),
                new Vector2(pathWidth + 4, rampLength + 4))
        };
    }
}
