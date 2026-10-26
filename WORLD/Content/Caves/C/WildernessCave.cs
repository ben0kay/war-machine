// Separate collidable tunnel and rocky covering, following Terrain3D demo's mesh + crystal-light approach.
using Godot;

public partial class WildernessCave : Node3D
{
    [Export] public CaveSettings Settings { get; set; } = null!;
    public Vector3 Entrance { get; private set; }

    // =========================================================
    // Build once after saved terrain loads. No holes, sculpting, region saves or frame callbacks.
    public void Initialize(Node3D terrain, WorldConfig world)
    {
        var data = terrain.Get("data").AsGodotObject();
        Vector2 origin = world.SpawnXZ + Settings.SpawnOffset;
        int sections = Mathf.Clamp(Settings.Sections, 16, 96);
        var centers = new Vector3[sections + 1]; float highest = float.MinValue;
        for (int i = 0; i <= sections; i++)
        {
            float t = (float)i / sections;
            float x = origin.X + Mathf.Sin(t * Mathf.Tau * 1.1f) * Settings.Bend;
            float z = origin.Y - t * Settings.Length;
            float h = data.Call("get_height", new Vector3(x, 0, z)).AsSingle();
            if (!float.IsFinite(h)) { GD.PushError("Cave path outside saved terrain."); return; }
            highest = Mathf.Max(highest, h); centers[i] = new Vector3(x, h, z);
        }
        float entryHeight = centers[0].Y;
        for (int i = 0; i <= sections; i++)
        {
            float t = (float)i / sections;
            var pos = centers[i];
            float rise = Mathf.SmoothStep(0, 0.35f, t);
            pos.Y = Mathf.Lerp(entryHeight + 0.06f, highest + 0.5f, rise)
                + (1 - Mathf.Cos(t * Mathf.Tau * 1.5f)) * 0.5f * Settings.HeightVariation;
            pos.Y = Mathf.Max(pos.Y, centers[i].Y + 0.06f);
            centers[i] = pos;
        }
        Entrance = centers[0];
        var material = (Material)GD.Load<Material>("res://WORLD/Content/Rocks/Configuration/StoneMaterial.tres").Duplicate();
        using var inside = new ContentMeshBuilder(material); using var shell = new ContentMeshBuilder(material);
        // Flat floor plus arched roof: winding without abrupt stair steps.
        Vector2[] profile = { new(-1, 0), new(1, 0), new(1, 0.5f), new(0.7f, 0.86f), new(0, 1), new(-0.7f, 0.86f), new(-1, 0.5f) };
        var rings = new Vector3[sections + 1, profile.Length]; var outer = new Vector3[sections + 1, profile.Length];
        for (int i = 0; i <= sections; i++)
        {
            Vector3 tangent = centers[Mathf.Min(sections, i + 1)] - centers[Mathf.Max(0, i - 1)]; tangent.Y = 0;
            var right = tangent.Normalized().Cross(Vector3.Up);
            for (int j = 0; j < profile.Length; j++)
            {
                rings[i, j] = centers[i] + right * profile[j].X * Settings.Width + Vector3.Up * profile[j].Y * Settings.Height;
                outer[i, j] = centers[i] + right * profile[j].X * (Settings.Width + 3.5f)
                    + Vector3.Up * (profile[j].Y * (Settings.Height + 3) - 0.6f);
            }
        }
        for (int i = 0; i < sections; i++) for (int j = 0; j < profile.Length; j++)
        {
            int next = (j + 1) % profile.Length;
            Vector3 center = centers[i].Lerp(centers[i + 1], 0.5f) + Vector3.Up * Settings.Height * 0.45f;
            Inward(inside, rings[i, j], rings[i + 1, j], rings[i, next], center);
            Inward(inside, rings[i, next], rings[i + 1, j], rings[i + 1, next], center);
            shell.RockFace(outer[i, j], outer[i, next], outer[i + 1, j], Colors.Gray, center);
            shell.RockFace(outer[i, next], outer[i + 1, next], outer[i + 1, j], Colors.Gray, center);
            if (i == sections - 1)
            {
                shell.Face(rings[sections, j], rings[sections, next], outer[sections, j], Colors.Gray);
                shell.Face(rings[sections, next], outer[sections, next], outer[sections, j], Colors.Gray);
            }
            if (i == 0)
            {
                shell.Face(rings[0, j], outer[0, j], rings[0, next], Colors.Gray);
                shell.Face(rings[0, next], outer[0, j], outer[0, next], Colors.Gray);
            }
        }
        // Seal the far end into a small chamber rather than exposing the hollow shell.
        for (int j = 1; j < profile.Length - 1; j++)
            Inward(inside, rings[sections, 0], rings[sections, j], rings[sections, j + 1], centers[sections - 1] + Vector3.Up * 2);
        AddSolid("Tunnel", inside.Finish()); AddSolid("RockOutcrop", shell.Finish());
        for (int i = sections / 4; i < sections; i += sections / 4)
        {
            var crystal = new MeshInstance3D { Position = centers[i] + new Vector3(Settings.Width * 0.65f, 0.7f, 0),
                Mesh = new PrismMesh { Size = new Vector3(0.45f, 1.2f, 0.45f) },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = Settings.LightColor, EmissionEnabled = true, Emission = Settings.LightColor, EmissionEnergyMultiplier = 2 } };
            AddChild(crystal);
            var light = new OmniLight3D { Position = crystal.Position + Vector3.Up, LightColor = Settings.LightColor,
                LightEnergy = Settings.LightEnergy, OmniRange = 16, ShadowEnabled = true,
                DistanceFadeEnabled = true, DistanceFadeBegin = 90, DistanceFadeLength = 20 };
            AddChild(light);
        }
        var marker = new Label3D { Text = "CAVE →", Position = Entrance + new Vector3(-5, 2.5f, 4), PixelSize = 0.025f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
        AddChild(marker); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Lighting normals face the passage interior, not the surrounding rock.
    private static void Inward(ContentMeshBuilder mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 center)
    {
        if ((b - a).Cross(c - a).Dot(center - (a + b + c) / 3f) < 0) (b, c) = (c, b);
        mesh.Face(a, b, c, Colors.Gray);
    }

    // =========================================================
    private void AddSolid(string name, ArrayMesh mesh)
    {
        var body = new StaticBody3D { Name = name }; AddChild(body);
        body.AddChild(new MeshInstance3D { Mesh = mesh });
        var shape = mesh.CreateTrimeshShape(); shape.BackfaceCollision = true;
        body.AddChild(new CollisionShape3D { Shape = shape });
    }
}
