// Terrain-embedded collidable cave, following Terrain3D demo's mesh + crystal-light approach.
using Godot;

public partial class WildernessCave : Node3D
{
    [Export] public CaveSettings Settings { get; set; } = null!;
    private Vector3[] _centers = System.Array.Empty<Vector3>();
    public Vector3 Entrance { get; private set; }

    // =========================================================
    // Build and embed once after saved terrain loads. Runtime maps only; no region saves or frame callbacks.
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
        _centers = centers;
        Entrance = centers[0];
        EmbedInTerrain(terrain, data, centers);
        var material = (Material)GD.Load<Material>("res://WORLD/Content/Rocks/Configuration/StoneMaterial.tres").Duplicate();
        using var inside = new ContentMeshBuilder(material);
        // Flat floor plus arched roof: winding without abrupt stair steps.
        Vector2[] profile = { new(-1, 0), new(1, 0), new(1, 0.5f), new(0.7f, 0.86f), new(0, 1), new(-0.7f, 0.86f), new(-1, 0.5f) };
        var rings = new Vector3[sections + 1, profile.Length];
        for (int i = 0; i <= sections; i++)
        {
            Vector3 tangent = centers[Mathf.Min(sections, i + 1)] - centers[Mathf.Max(0, i - 1)]; tangent.Y = 0;
            var right = tangent.Normalized().Cross(Vector3.Up);
            for (int j = 0; j < profile.Length; j++)
            {
                rings[i, j] = centers[i] + right * profile[j].X * Settings.Width + Vector3.Up * profile[j].Y * Settings.Height;

            }
        }
        for (int i = 0; i < sections; i++) for (int j = 0; j < profile.Length; j++)
        {
            int next = (j + 1) % profile.Length;
            Vector3 center = centers[i].Lerp(centers[i + 1], 0.5f) + Vector3.Up * Settings.Height * 0.45f;
            Inward(inside, rings[i, j], rings[i + 1, j], rings[i, next], center);
            Inward(inside, rings[i, next], rings[i + 1, j], rings[i + 1, next], center);
        }
        // Seal the far end into a small chamber rather than exposing the hollow shell.
        for (int j = 1; j < profile.Length - 1; j++)
            Inward(inside, rings[sections, 0], rings[sections, j], rings[sections, j + 1], centers[sections - 1] + Vector3.Up * 2);
        AddSolid("Tunnel", inside.Finish());
        var random = new RandomNumberGenerator { Seed = 7131 };
        var rockSettings = new ContentScatterSettings { Size = 1.6f, PrimaryColor = Colors.Gray, AccentColor = Colors.DarkGray };
        for (int i = 0; i < 6; i++)
        {
            var rock = RockGenerator.Create(rockSettings, random, material);
            var portal = new Node3D { Position = Entrance + new Vector3(i < 3 ? -Settings.Width - 0.8f : Settings.Width + 0.8f, (i % 3) * 1.7f, -0.8f) };
            AddChild(portal);
            var body = new StaticBody3D(); portal.AddChild(body);
            body.AddChild(new MeshInstance3D { Mesh = rock });
            var shape = rock.CreateTrimeshShape(); shape.BackfaceCollision = true;
            body.AddChild(new CollisionShape3D { Shape = shape });
        }
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
    // One bounded runtime authoring pass raises the actual heightfield over the passage; source regions stay untouched.
    private void EmbedInTerrain(Node3D terrain, GodotObject data, Vector3[] path)
    {
        float feather = 32;
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var point in path) { minX = Mathf.Min(minX, point.X); maxX = Mathf.Max(maxX, point.X); }
        int regionSize = terrain.Get("region_size").AsInt32();
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        var regions = new System.Collections.Generic.Dictionary<Vector2I, (Image heights, Image control)>();
        for (int z = Mathf.FloorToInt((path[^1].Z - feather) / spacing); z <= Mathf.CeilToInt((path[0].Z + 2) / spacing); z++)
        for (int x = Mathf.FloorToInt((minX - feather - Settings.Width) / spacing); x <= Mathf.CeilToInt((maxX + feather + Settings.Width) / spacing); x++)
        {
            var pos = new Vector3(x * spacing, 0, z * spacing);
            float distance = float.MaxValue; Vector3 nearest = path[0];
            for (int j = 0; j < path.Length - 1; j++)
            {
                var a = new Vector2(path[j].X, path[j].Z); var b = new Vector2(path[j + 1].X, path[j + 1].Z);
                float t = Mathf.Clamp((new Vector2(pos.X, pos.Z) - a).Dot(b - a) / (b - a).LengthSquared(), 0, 1);
                var point = path[j].Lerp(path[j + 1], t);
                float d = new Vector2(pos.X - point.X, pos.Z - point.Z).Length();
                if (d < distance) { distance = d; nearest = point; }
            }
            if (distance > Settings.Width + feather) continue;
            var key = new Vector2I(Mathf.FloorToInt((float)x / regionSize), Mathf.FloorToInt((float)z / regionSize));
            if (!regions.TryGetValue(key, out var maps))
            {
                var region = data.Call("get_regionp", pos).AsGodotObject(); if (region == null) continue;
                maps = ((Image)region.Call("get_map", 0).AsGodotObject(), (Image)region.Call("get_map", 1).AsGodotObject());
                regions[key] = maps;
            }
            int px = Mathf.PosMod(x, regionSize), pz = Mathf.PosMod(z, regionSize);
            float old = maps.heights.GetPixel(px, pz).R;
            float depth = path[0].Z - pos.Z;
            float front = Mathf.SmoothStep(0, 2.5f, depth);
            float blend = (1 - Mathf.SmoothStep(Settings.Width + 5, Settings.Width + feather, distance)) * front;
            float ridge = nearest.Y + Settings.Height + 5 + Mathf.Sin(pos.X * 0.11f + pos.Z * 0.07f) * 1.2f;
            maps.heights.SetPixel(px, pz, new Color(Mathf.Lerp(old, Mathf.Max(old, ridge), blend), 0, 0));
            // Hole only at the mouth, allowing passage through the rising cliff; deeper roof is buried.
            if (depth >= 0 && depth < 3.5f && distance < Settings.Width + 0.8f)
            {
                var control = maps.control.GetPixel(px, pz);
                int bits = System.BitConverter.SingleToInt32Bits(control.R) | 4;
                maps.control.SetPixel(px, pz, new Color(System.BitConverter.Int32BitsToSingle(bits), 0, 0));
            }
        }
        data.Call("calc_height_range", true); data.Call("update_maps", 3, true, false);
        terrain.Call("get_collision").AsGodotObject().Call("update", true);
    }

    // =========================================================
    // Event-driven shelter checks suppress camera-local precipitation inside the tunnel.
    public bool IsSheltered(Vector3 position)
    {
        for (int i = 1; i < _centers.Length - 1; i++)
        {
            var point = _centers[i];
            if (position.Y >= point.Y && position.Y < point.Y + Settings.Height
                && new Vector2(position.X - point.X, position.Z - point.Z).Length() < Settings.Width - 0.2f) return true;
        }
        return false;
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
