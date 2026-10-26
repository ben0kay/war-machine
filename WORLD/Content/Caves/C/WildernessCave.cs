// Terrain-embedded collidable cave, following Terrain3D demo's mesh + crystal-light approach.
using Godot;

public partial class WildernessCave : Node3D
{
    [Export] public CaveSettings Settings { get; set; } = null!;
    private Vector3[] _centers = System.Array.Empty<Vector3>();
    private float[] _widths = System.Array.Empty<float>(), _heights = System.Array.Empty<float>();
    public Vector3 Entrance { get; private set; }

    // =========================================================
    // Build and embed once after saved terrain loads. Runtime maps only; no region saves or frame callbacks.
    public void Initialize(Node3D terrain, WorldConfig world)
    {
        using var noise = new CaveShapeNoise(Settings);
        var data = terrain.Get("data").AsGodotObject();
        Vector2 origin = world.SpawnXZ + Settings.SpawnOffset;
        int sections = Mathf.Clamp(Settings.Sections, 16, 96);
        var centers = new Vector3[sections + 1]; float highest = float.MinValue;
        for (int i = 0; i <= sections; i++)
        {
            float t = (float)i / sections;
            float z = origin.Y - t * Settings.Length;
            float x = origin.X + Mathf.Sin(t * Mathf.Tau * 1.1f) * Settings.Bend
                + noise.Broad(new Vector3(origin.X, 0, z)) * Settings.Bend * 0.3f * Mathf.SmoothStep(0, 0.15f, t);
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
            pos.Y = Mathf.Max(pos.Y, centers[i].Y + 0.06f)
                + noise.Broad(pos + Vector3.Right * 23) * Mathf.Clamp(Settings.FloorRelief, 0, 0.2f);
            centers[i] = pos;
        }
        _centers = centers;
        _widths = new float[sections + 1]; _heights = new float[sections + 1];
        Entrance = centers[0];
        var material = (Material)GD.Load<Material>("res://WORLD/Content/Rocks/Configuration/StoneMaterial.tres").Duplicate();
        using var inside = new ContentMeshBuilder(material);
        int arch = Mathf.Clamp(Settings.ArchSegments, 12, 40);
        var profile = new Vector2[arch + 4];
        profile[0] = new Vector2(-1, 0); profile[1] = new Vector2(1, 0);
        for (int j = 0; j <= arch; j++)
        {
            float angle = (float)j / arch * Mathf.Pi;
            profile[j + 2] = new Vector2(Mathf.Cos(angle), 0.22f + 0.78f * Mathf.Sin(angle));
        }
        profile[^1] = new Vector2(-1, 0.12f);
        var rings = new Vector3[sections + 1, profile.Length];
        for (int i = 0; i <= sections; i++)
        {
            Vector3 tangent = centers[Mathf.Min(sections, i + 1)] - centers[Mathf.Max(0, i - 1)]; tangent.Y = 0;
            var right = tangent.Normalized().Cross(Vector3.Up);
            _widths[i] = noise.Width(centers[i]); _heights[i] = noise.Height(centers[i]);
            for (int j = 0; j < profile.Length; j++)
            {
                Vector2 p = profile[j];
                Vector3 point = centers[i] + right * p.X * _widths[i] + Vector3.Up * p.Y * _heights[i];
                float relief = noise.Detail(point) * Mathf.Clamp(Settings.WallRelief, 0, 0.6f) * Mathf.SmoothStep(0, 0.3f, p.Y);
                rings[i, j] = point + (right * p.X + Vector3.Up * p.Y).Normalized() * relief;
            }
        }
        // The outer terrain and entrance use this exact cached noisy rim, never independent random offsets.
        EmbedInTerrain(terrain, data, centers, noise);
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
        BuildEntrance(data, centers, rings, profile, material, noise);
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
    // A curved noisy apron spans the mouth-to-heightfield boundary; no long extruded rock box.
    private void BuildEntrance(GodotObject data, Vector3[] centers, Vector3[,] rings, Vector2[] profile,
        Material material, CaveShapeNoise noise)
    {
        const int bands = 8;
        int count = profile.Length;
        float radius = Mathf.Max(Settings.Width * 2.5f + 3, Settings.EntranceBlendRadius);
        Vector3 forward = centers[0] - centers[1]; forward.Y = 0; forward = forward.Normalized();
        Vector3 right = Vector3.Up.Cross(forward);
        var surface = new Vector3[bands + 1, count];
        for (int j = 0; j < count; j++)
        {
            Vector2 p = profile[j];
            // Upper arch wraps behind the mouth into the hillside; floor apron extends towards the approach.
            float edgeRadius = radius * (1 + noise.Broad(centers[0] + new Vector3(p.X * 9, p.Y * 7, 13)) * 0.12f);
            float behind = p.Y > 0 ? Mathf.Sqrt(Mathf.Max(0, 1 - p.X * p.X)) * edgeRadius : -3;
            Vector3 edge = centers[0] + right * p.X * edgeRadius - forward * behind;
            float terrainHeight = data.Call("get_pixel", 0, edge).AsColor().R;
            if (!float.IsFinite(terrainHeight)) terrainHeight = centers[0].Y;
            edge.Y = terrainHeight - 0.15f;
            for (int band = 0; band <= bands; band++)
            {
                float t = (float)band / bands;
                Vector3 point = rings[0, j].Lerp(edge, t);
                float mask = Mathf.Sin(t * Mathf.Pi) * Mathf.SmoothStep(0, 0.25f, p.Y);
                point += Vector3.Up * noise.Detail(point) * 0.6f * mask;
                surface[band, j] = point;
            }
        }
        using var mesh = new ContentMeshBuilder(material);
        Vector3 center = Entrance + Vector3.Up * Settings.Height * 0.4f;
        for (int band = 0; band < bands; band++) for (int j = 0; j < count; j++)
        {
            int next = (j + 1) % count;
            if (j == 0)
            {
                // The approach floor faces the walker rather than the underside of the cliff.
                Inward(mesh, surface[band, j], surface[band + 1, j], surface[band, next], center);
                Inward(mesh, surface[band, next], surface[band + 1, j], surface[band + 1, next], center);
            }
            else
            {
                mesh.RockFace(surface[band, j], surface[band + 1, j], surface[band, next], Colors.Gray, center);
                mesh.RockFace(surface[band, next], surface[band + 1, j], surface[band + 1, next], Colors.Gray, center);
            }
        }
        // Bury a short edge skirt in the heightfield so interpolation cannot expose a thin daylight seam.
        for (int j = 0; j < count; j++)
        {
            int next = (j + 1) % count;
            Vector3 a = surface[bands, j], b = surface[bands, next];
            mesh.Face(a, b, a + Vector3.Down * 0.8f, Colors.Gray);
            mesh.Face(b, b + Vector3.Down * 0.8f, a + Vector3.Down * 0.8f, Colors.Gray);
        }
        AddSolid("OrganicEntrance", mesh.Finish());
    }

    // =========================================================
    // One bounded runtime authoring pass raises the actual heightfield over the passage; source regions stay untouched.
    private void EmbedInTerrain(Node3D terrain, GodotObject data, Vector3[] path, CaveShapeNoise noise)
    {
        float feather = Mathf.Clamp(Settings.RidgeFeather, 16, 64);
        float maxWidth = Mathf.Max(2, Settings.Width) * (1 + Mathf.Clamp(Settings.WidthNoise, 0, 0.4f)) + Mathf.Clamp(Settings.WallRelief, 0, 0.6f);
        float maxHeight = Mathf.Max(3, Settings.Height) * (1 + Mathf.Clamp(Settings.HeightNoise, 0, 0.4f)) + Mathf.Clamp(Settings.WallRelief, 0, 0.6f);
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var point in path) { minX = Mathf.Min(minX, point.X); maxX = Mathf.Max(maxX, point.X); }
        int regionSize = terrain.Get("region_size").AsInt32();
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        Vector3 entryDirection = path[1] - path[0]; entryDirection.Y = 0; entryDirection = entryDirection.Normalized();
        Vector3 entryRight = entryDirection.Cross(Vector3.Up);
        var regions = new System.Collections.Generic.Dictionary<Vector2I, (Image heights, Image control)>();
        for (int z = Mathf.FloorToInt((path[^1].Z - feather) / spacing); z <= Mathf.CeilToInt((path[0].Z + maxWidth + 3) / spacing); z++)
        for (int x = Mathf.FloorToInt((minX - feather - maxWidth) / spacing); x <= Mathf.CeilToInt((maxX + feather + maxWidth) / spacing); x++)
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
            if (distance > maxWidth + feather) continue;
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
            float front = Mathf.SmoothStep(0, 5, depth);
            float organicDistance = distance + noise.Broad(pos) * 3;
            float blend = (1 - Mathf.SmoothStep(maxWidth + 5, maxWidth + feather, organicDistance)) * front;
            float ridge = nearest.Y + maxHeight + 5 + noise.Broad(pos + Vector3.Up * 47) * 2;
            maps.heights.SetPixel(px, pz, new Color(Mathf.Lerp(old, Mathf.Max(old, ridge), blend), 0, 0));
            // Hole only at the mouth, allowing passage through the rising cliff; deeper roof is buried.
            Vector3 fromEntry = pos - path[0]; fromEntry.Y = 0;
            float entryDepth = fromEntry.Dot(entryDirection);
            if (entryDepth >= -0.6f && entryDepth < 5.5f && Mathf.Abs(fromEntry.Dot(entryRight)) < maxWidth + 0.5f)
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
            if (position.Y >= point.Y && position.Y < point.Y + _heights[i] * 0.9f
                && new Vector2(position.X - point.X, position.Z - point.Z).Length() < _widths[i] - 0.3f) return true;
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
