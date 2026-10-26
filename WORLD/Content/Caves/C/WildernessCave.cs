// Terrain-embedded collidable cave, following Terrain3D demo's mesh + crystal-light approach.
using Godot;

public partial class WildernessCave : Node3D
{
    [Export] public CaveSettings Settings { get; set; } = null!;
    private const int FloorSegments = 6;
    private readonly System.Collections.Generic.List<Passage> _passages = new();
    private Vector3[] _centers = System.Array.Empty<Vector3>();
    public Vector3 Entrance { get; private set; }

    // =========================================================
    // Build and embed once after saved terrain loads. Runtime maps only; no region saves or frame callbacks.
    public void Initialize(Node3D terrain, WorldConfig world)
    {
        using var noise = new CaveShapeNoise(Settings);
        var data = terrain.Get("data").AsGodotObject();
        Vector2 origin = world.SpawnXZ + Settings.SpawnOffset;
        int sections = Mathf.Clamp(Settings.Sections, 16, 144);
        float entryHeight = data.Call("get_height", new Vector3(origin.X, 0, origin.Y)).AsSingle();
        if (!float.IsFinite(entryHeight)) { GD.PushError("Cave entrance outside saved terrain."); return; }
        float length = Mathf.Clamp(Settings.Length, 30, 260);
        var centers = CaveRouteBuilder.Build(Settings, noise,
            new Vector3(origin.X, entryHeight + 0.06f, origin.Y), Vector3.Forward,
            length, sections, Settings.DescentDepth, 0);
        _centers = centers;
        Entrance = centers[0];
        var material = (Material)GD.Load<Material>("res://WORLD/Content/Rocks/Configuration/StoneMaterial.tres").Duplicate();
        using var inside = new ContentMeshBuilder(material);
        int arch = Mathf.Clamp(Settings.ArchSegments, 12, 40);
        var profile = new Vector2[FloorSegments + arch + 3];
        for (int j = 0; j <= FloorSegments; j++) profile[j] = new Vector2(-1 + 2f * j / FloorSegments, 0);
        for (int j = 0; j <= arch; j++)
        {
            float angle = (float)j / arch * Mathf.Pi;
            profile[j + FloorSegments + 1] = new Vector2(Mathf.Cos(angle), 0.22f + 0.78f * Mathf.Sin(angle));
        }
        profile[^1] = new Vector2(-1, 0.12f);
        var main = CreatePassage(centers, profile, noise, 1, Settings.MainChamberCount, length);
        var rings = main.Rings;
        _passages.Clear(); _passages.Add(main);
        CreateBranches(main, profile, noise);
        // The outer terrain and entrance use this exact cached noisy rim, never independent random offsets.
        foreach (var passage in _passages)
            if (!HasNaturalCover(data, passage)) EmbedInTerrain(terrain, data, passage, noise);
        data.Call("calc_height_range", true); data.Call("update_maps", 3, true, false);
        terrain.Call("get_collision").AsGodotObject().Call("update", true);
        for (int i = 0; i < sections; i++) for (int j = 0; j < profile.Length; j++)
        {
            int next = (j + 1) % profile.Length;
            Vector3 center = centers[i].Lerp(centers[i + 1], 0.5f) + Vector3.Up * Settings.Height * 0.45f;
            Inward(inside, rings[i, j], rings[i + 1, j], rings[i, next], center);
            Inward(inside, rings[i, next], rings[i + 1, j], rings[i + 1, next], center);
        }
        // Main passage stays open at its far end; one shared chamber joins both branch ports.
        for (int branch = 1; branch < _passages.Count; branch++) BuildPassage(inside, _passages[branch], true);
        BuildJunction(inside, main, _passages[1], _passages[2], noise);
        AddSolid("Tunnel", inside.Finish());
        var exterior = (ShaderMaterial)GD.Load<ShaderMaterial>("res://WORLD/Content/Caves/Configuration/CaveExterior.tres").Duplicate();
        exterior.SetShaderParameter("texture_scale", Mathf.Clamp(Settings.ExteriorTextureScale, 0.05f, 2));
        exterior.SetShaderParameter("normal_strength", Mathf.Clamp(Settings.ExteriorNormalStrength, 0, 1));
        BuildEntrance(data, centers, rings, profile, exterior, noise);
        for (int p = 0; p < _passages.Count; p++)
        {
            var passage = _passages[p]; int step = passage.Centers.Length / 4;
            for (int i = step; i < passage.Centers.Length - 1; i += step)
                AddCrystal(passage.Centers[i], passage.Widths[i], p == 0);
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
            // Nested profiles stay ordered in the mouth plane; the old ground-projected loop folded over itself.
            float outerHeight = Mathf.Max(3, Settings.Height) * (1 + Mathf.Clamp(Settings.HeightNoise, 0, 0.4f))
                + Mathf.Clamp(Settings.WallRelief, 0, 0.6f) + 1.5f;
            Vector3 edge = centers[0] + right * p.X * radius;
            if (p.Y <= 0)
            {
                edge += forward * 3;
                edge.Y = data.Call("get_pixel", 0, edge).AsColor().R - 0.1f;
            }
            else
            {
                edge.Y = centers[0].Y + p.Y * outerHeight;
                // Find a buried landing behind the opening without collapsing the ordered vertical rim.
                for (int step = 1; step <= 48; step++)
                {
                    edge.X = centers[0].X + right.X * p.X * radius - forward.X * step * 0.5f;
                    edge.Z = centers[0].Z + right.Z * p.X * radius - forward.Z * step * 0.5f;
                    float terrainHeight = data.Call("get_pixel", 0, edge).AsColor().R;
                    if (float.IsFinite(terrainHeight) && terrainHeight > edge.Y + 0.3f) break;
                }
            }
            if (!float.IsFinite(edge.Y)) edge.Y = centers[0].Y;
            for (int band = 0; band <= bands; band++)
            {
                float t = (float)band / bands;
                Vector3 point = rings[0, j].Lerp(edge, t);
                float mask = Mathf.Sin(t * Mathf.Pi) * Mathf.SmoothStep(0, 0.25f, p.Y);
                point += Vector3.Up * noise.Detail(point) * 0.1f * mask;
                surface[band, j] = point;
            }
        }
        using var mesh = new ContentMeshBuilder(material);
        Vector3 center = Entrance + Vector3.Up * Settings.Height * 0.4f;
        for (int band = 0; band < bands; band++) for (int j = 0; j < count; j++)
        {
            int next = (j + 1) % count;
            if (j < FloorSegments)
            {
                // The approach floor faces the walker rather than the underside of the cliff.
                Inward(mesh, surface[band, j], surface[band + 1, j], surface[band, next], center);
                Inward(mesh, surface[band, next], surface[band + 1, j], surface[band + 1, next], center);
            }
            else
            {
                FrontFace(mesh, surface[band, j], surface[band + 1, j], surface[band, next], forward);
                FrontFace(mesh, surface[band, next], surface[band + 1, j], surface[band + 1, next], forward);
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
    private void EmbedInTerrain(Node3D terrain, GodotObject data, Passage passage, CaveShapeNoise noise)
    {
        float feather = Mathf.Clamp(Settings.RidgeFeather, 16, 64), relief = Mathf.Clamp(Settings.WallRelief, 0, 0.6f);
        Vector3[] path = passage.Centers;
        // Bound BOTH horizontal axes by every centre and its local expanded chamber width.
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        for (int i = 0; i < path.Length; i++)
        {
            float radius = passage.Widths[i] + relief + feather + 5;
            minX = Mathf.Min(minX, path[i].X - radius); maxX = Mathf.Max(maxX, path[i].X + radius);
            minZ = Mathf.Min(minZ, path[i].Z - radius); maxZ = Mathf.Max(maxZ, path[i].Z + radius);
        }
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        int regionSize = terrain.Get("region_size").AsInt32();
        Vector3 entryForward = path[1] - path[0]; entryForward.Y = 0; entryForward = entryForward.Normalized();
        Vector3 entryRight = entryForward.Cross(Vector3.Up);
        bool isMain = path == _centers;
        var regions = new System.Collections.Generic.Dictionary<Vector2I, (Image heights, Image control)>();
        for (int z = Mathf.FloorToInt(minZ / spacing); z <= Mathf.CeilToInt(maxZ / spacing); z++)
        for (int x = Mathf.FloorToInt(minX / spacing); x <= Mathf.CeilToInt(maxX / spacing); x++)
        {
            Vector3 pos = new(x * spacing, 0, z * spacing);
            float best = float.MaxValue, width = 0, height = 0;
            Vector3 nearest = path[0];
            for (int j = 0; j < path.Length - 1; j++)
            {
                Vector2 a = new(path[j].X, path[j].Z), b = new(path[j + 1].X, path[j + 1].Z);
                Vector2 edge = b - a;
                float u = Mathf.Clamp((new Vector2(pos.X, pos.Z) - a).Dot(edge)
                    / Mathf.Max(0.0001f, edge.LengthSquared()), 0, 1);
                Vector3 candidate = path[j].Lerp(path[j + 1], u);
                float d2 = new Vector2(pos.X - candidate.X, pos.Z - candidate.Z).LengthSquared();
                if (d2 >= best) continue;
                best = d2; nearest = candidate;
                width = Mathf.Lerp(passage.Widths[j], passage.Widths[j + 1], u) + relief;
                height = Mathf.Lerp(passage.Heights[j], passage.Heights[j + 1], u) + relief;
            }
            if (best > (width + feather + 5) * (width + feather + 5)) continue;
            Vector3 mouthOffset = pos - Entrance; mouthOffset.Y = 0;
            float mouthDepth = mouthOffset.Dot(entryForward);
            bool mouth = isMain && mouthDepth >= -0.6f && mouthDepth < 5.5f
                && Mathf.Abs(mouthOffset.Dot(entryRight)) < width + 0.5f;
            float distance = Mathf.Sqrt(best), organic = distance + noise.Broad(pos) * 3;
            if (organic > width + feather && !mouth) continue;
            float forwardDistance = (pos - path[0]).Dot(entryForward);
            float startBlend = isMain ? Mathf.SmoothStep(0, 5, forwardDistance) : 1;
            float blend = (1 - Mathf.SmoothStep(width + 5, width + feather, organic)) * startBlend;
            if (blend <= 0 && !mouth) continue;
            var key = new Vector2I(Mathf.FloorToInt((float)x / regionSize), Mathf.FloorToInt((float)z / regionSize));
            if (!regions.TryGetValue(key, out var maps))
            {
                var region = data.Call("get_regionp", pos).AsGodotObject();
                if (region == null) continue;
                maps = ((Image)region.Call("get_map", 0).AsGodotObject(), (Image)region.Call("get_map", 1).AsGodotObject());
                regions[key] = maps;
            }
            int px = Mathf.PosMod(x, regionSize), pz = Mathf.PosMod(z, regionSize);
            float old = maps.heights.GetPixel(px, pz).R;
            float roof = nearest.Y + height + Mathf.Max(3, Settings.MinimumRoofCover) + 1
                + noise.Broad(pos + Vector3.Up * 47) * 2;
            // Only lift insufficient surface cells in the loaded runtime maps; never save regions.
            if (old < roof && blend > 0)
                maps.heights.SetPixel(px, pz, new Color(Mathf.Lerp(old, roof, blend), 0, 0));
            if (!mouth) continue;
            var control = maps.control.GetPixel(px, pz);
            int bits = System.BitConverter.SingleToInt32Bits(control.R) | 4;
            maps.control.SetPixel(px, pz, new Color(System.BitConverter.Int32BitsToSingle(bits), 0, 0));
        }
    }

    // =========================================================
    // Event-driven shelter checks suppress camera-local precipitation inside the tunnel.
    public bool IsSheltered(Vector3 position)
    {
        foreach (var passage in _passages)
        for (int i = 1; i < passage.Centers.Length; i++)
        {
            var point = passage.Centers[i];
            if (position.Y >= point.Y - 0.2f && position.Y < point.Y + passage.Heights[i] * 0.9f
                && new Vector2(position.X - point.X, position.Z - point.Z).Length() < passage.Widths[i] - 0.3f) return true;
        }
        if (_junctionPolygon.Length > 0 && position.Y >= _junctionFloor - 0.2f && position.Y < _junctionFloor + Settings.Height
            && Geometry2D.IsPointInPolygon(new Vector2(position.X, position.Z), _junctionPolygon)) return true;
        return false;
    }

    // =========================================================
    // A consistent exterior orientation avoids alternating front/back triangles across the entrance apron.
    private static void FrontFace(ContentMeshBuilder mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        if ((b - a).Cross(c - a).Dot(outward) < 0) (b, c) = (c, b);
        mesh.Face(a, b, c, Colors.Gray);
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
