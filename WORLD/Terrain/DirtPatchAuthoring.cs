// DTP1: explicit one-shot control-map painter; operates ONLY on a copied terrain directory.
using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

public partial class DirtPatchAuthoring : Node
{
    public const string PreviewDirectory = "res://WORLD/Terrain/GameplayDirtPreview";
    private const string StageDirectory = "res://WORLD/Terrain/GameplayDirtPreview.staging";
    private const string Manifest = "DTP1_SOURCE_MANIFEST.txt";
    private const uint Fields = (0x1Fu << 27) | (0x1Fu << 22) | (0xFFu << 14) | 1u;
    [Export] public DirtPatchSettings Settings { get; set; } = null!;

    // =========================================================
    // A preview is trustworthy only while its exact original native-region files are unchanged.
    public static bool HasCurrentPreview()
    {
        string src = ProjectSettings.GlobalizePath(WorldSettings.TerrainDirectory);
        string dst = ProjectSettings.GlobalizePath(PreviewDirectory);
        if (!Directory.Exists(src) || !File.Exists(Path.Combine(dst, Manifest))) return false;
        try
        {
            string[] originals = Directory.GetFiles(src, "*.res"), lines = File.ReadAllLines(Path.Combine(dst, Manifest));
            if (originals.Length == 0 || lines.Length != originals.Length + 1 || lines[0] != "DTP1-v1") return false;
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('|');
                if (parts.Length != 2 || parts[0] != Path.GetFileName(parts[0])
                    || !parts[0].EndsWith(".res", StringComparison.Ordinal) || hashes.ContainsKey(parts[0])) return false;
                hashes.Add(parts[0], parts[1]);
            }
            foreach (string file in originals)
            {
                string name = Path.GetFileName(file);
                if (!hashes.TryGetValue(name, out string? hash) || !File.Exists(Path.Combine(dst, name))
                    || !string.Equals(hash, Hash(file), StringComparison.Ordinal)) return false;
            }
            return Directory.GetFiles(dst, "*.res").Length == originals.Length;
        }
        catch (Exception) { return false; }
    }

    // =========================================================
    public override void _Ready()
    {
        try { Generate(); GetTree().Quit(); }
        catch (Exception ex) { GD.PushError("DTP1 failed; original saved terrain is untouched. " + ex); GetTree().Quit(1); }
    }

    // =========================================================
    // Copy first, refuse overwrite, and publish only after all native regions are safely saved.
    private void Generate()
    {
        if (!BitConverter.IsLittleEndian) throw new NotSupportedException("Terrain control bit packing needs little-endian.");
        if (Settings == null) throw new InvalidOperationException("DirtPatchMaster missing.");
        var world = GD.Load<WorldSettings>("res://WORLD/Configuration/WorldMaster.tres")
            ?? throw new InvalidOperationException("WorldMaster missing.");
        GameplayTerrainStorage.CopyLegacyBakeIfNeeded();
        string src = ProjectSettings.GlobalizePath(WorldSettings.TerrainDirectory);
        string dst = ProjectSettings.GlobalizePath(PreviewDirectory);
        string stage = ProjectSettings.GlobalizePath(StageDirectory);
        if (Directory.Exists(dst) || Directory.Exists(stage))
            throw new IOException("Dirt preview/staging already exists. Back up and delete ONLY that preview folder to regenerate.");
        if (!Directory.Exists(src)) throw new IOException("Original Gameplay terrain missing.");
        string[] sources = Directory.GetFiles(src, "*.res");
        if (sources.Length == 0) throw new IOException("No native .res terrain regions found.");
        Directory.CreateDirectory(stage);
        var lines = new List<string> { "DTP1-v1" };
        foreach (string source in sources)
        {
            string name = Path.GetFileName(source);
            File.Copy(source, Path.Combine(stage, name), false);
            lines.Add(name + "|" + Hash(source));
        }
        File.WriteAllLines(Path.Combine(stage, Manifest), lines);

        var terrain = ClassDB.Instantiate("Terrain3D").AsGodotObject() as Node3D
            ?? throw new InvalidOperationException("Terrain3D extension missing.");
        terrain.Set("region_size", WorldSettings.TerrainSize);
        terrain.Set("collision_mode", 0);
        terrain.Set("assets", GD.Load<Resource>("res://WORLD/Terrain/TerrainAssets.tres"));
        terrain.Set("material", GD.Load<Resource>("res://WORLD/Terrain/TerrainMaterial.tres"));
        terrain.Set("data_directory", StageDirectory);
        AddChild(terrain);
        var camera = new Camera3D { Position = new Vector3(world.VillageOriginXZ.X, 100, world.VillageOriginXZ.Y), Current = true };
        AddChild(camera); terrain.Call("set_camera", camera);
        var data = terrain.Get("data").AsGodotObject();
        if (data.Get("region_locations").AsGodotArray().Count == 0) data.Call("load_directory", StageDirectory);
        var locations = data.Get("region_locations").AsGodotArray();
        if (locations.Count != sources.Length) throw new IOException("Preview did not load every native terrain region.");

        using var field = new DirtPatchField(Settings, world.VillageOriginXZ);
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        if (!float.IsFinite(spacing) || spacing <= 0) throw new IOException("Bad Terrain3D vertex_spacing.");
        Rect2 village = new(world.VillageOriginXZ + Settings.ProtectedMin, Settings.ProtectedMax - Settings.ProtectedMin);
        int changed = 0, regions = 0, manual = 0, holes = 0;
        foreach (Variant value in locations)
        {
            Vector2I loc = value.AsVector2I();
            var region = data.Call("get_region", loc).AsGodotObject();
            var map = (Image)region.Call("get_map", 1).AsGodotObject();
            var height = (Image)region.Call("get_map", 0).AsGodotObject();
            int size = map.GetWidth();
            if (map.GetFormat() != Image.Format.Rf || map.GetHeight() != size ||
                height.GetWidth() != size || height.GetHeight() != size) throw new IOException("Invalid Terrain3D map dimensions.");
            Vector2 origin = new(loc.X * size * spacing, loc.Y * size * spacing);
            Rect2 rect = new(origin, Vector2.One * size * spacing);
            if (!rect.Intersects(field.Area)) continue;
            float[] coverage = new float[size * size];
            foreach (var shape in field.Marks)
            {
                if (!shape.Bounds.Intersects(rect)) continue;
                Rect2 r = shape.Bounds.Intersection(rect).Intersection(field.Area);
                int x0 = Mathf.Clamp(Mathf.FloorToInt((r.Position.X - origin.X) / spacing), 0, size - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((r.End.X - origin.X) / spacing), 0, size - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((r.Position.Y - origin.Y) / spacing), 0, size - 1);
                int z1 = Mathf.Clamp(Mathf.CeilToInt((r.End.Y - origin.Y) / spacing), 0, size - 1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = origin + new Vector2(x, z) * spacing;
                    if (world.VillageEnabled && village.HasPoint(p)) continue;
                    int index = z * size + x;
                    coverage[index] = Mathf.Max(coverage[index], field.Coverage(shape, p));
                }
            }
            byte[] bytes = map.GetData();
            if (bytes.Length != size * size * 4) throw new IOException("Not a packed R32 control map.");
            int local = 0;
            for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
            {
                int n = z * size + x, offset = n * 4;
                if (coverage[n] < .035f) continue;
                uint old = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
                if ((old & 4u) != 0) { holes++; continue; }
                if ((old & 1u) == 0) { manual++; continue; }
                float h = height.GetPixel(x, z).R;
                float east = height.GetPixel(Mathf.Min(x + 1, size - 1), z).R;
                float south = height.GetPixel(x, Mathf.Min(z + 1, size - 1)).R;
                if (!float.IsFinite(h) || !float.IsFinite(east) || !float.IsFinite(south)) continue;
                float nx = (east - h) / spacing, nz = (south - h) / spacing;
                float normalY = 1f / Mathf.Sqrt(1f + nx * nx + nz * nz);
                float fade = Mathf.Clamp((normalY - (Settings.MinimumNormalY - .055f)) / .055f, 0f, 1f);
                fade = fade * fade * (3f - 2f * fade);
                int weight = Mathf.Clamp(Mathf.RoundToInt(coverage[n] * fade * 255f), 0, 255);
                if (weight < 9) continue;
                // Only alter material IDs, blend and autoshader; preserve hole/nav/UV/reserved bits.
                uint painted = (old & ~Fields) | (1u << 27) | (2u << 22) | ((uint)weight << 14);
                if (painted == old) continue;
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), painted); local++;
            }
            if (local == 0) continue;
            region.Call("set_map", 1, Image.CreateFromData(size, size, false, Image.Format.Rf, bytes));
            region.Set("edited", true); changed += local; regions++;
        }
        if (changed == 0) throw new IOException($"No automatic texels were painted (manual {manual}, hole {holes}). No preview published.");
        data.Call("update_maps", 1, true, false);
        data.Call("save_directory", StageDirectory);
        if (Directory.GetFiles(stage, "*.res").Length != sources.Length)
            throw new IOException("Incomplete save: staging folder retained; original terrain unchanged.");
        Directory.Move(stage, dst);
        GD.Print($"DTP1: dirt preview painted {changed} texels across {regions} regions; skipped manual={manual}, holes={holes}. Test FPS F6.");
    }

    // =========================================================
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
