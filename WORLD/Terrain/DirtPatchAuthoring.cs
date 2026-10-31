// DTP1: explicit one-shot control-map painter; operates ONLY on a copied terrain directory.
using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

public partial class DirtPatchAuthoring : Node
{
    public const string PreviewDirectory = "res://WORLD/Terrain/GameplayDirtPreviewV3";
    private const string StageDirectory = "res://WORLD/Terrain/GameplayDirtPreviewV3.staging";
    private const string Manifest = "DTP1_V3_SOURCE_MANIFEST.txt";
    private const string ManifestVersion = "DTP1-v3-savefix";
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
            if (originals.Length == 0 || lines.Length != originals.Length + 1 || lines[0] != ManifestVersion) return false;
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
        catch (Exception ex) { GD.PrintErr("DTP1 V3 FAILED. See error below; original saved terrain is untouched. " + ex); GD.PushError("DTP1 V3 failed: " + ex.Message); GetTree().Quit(1); }
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
        if (!Directory.Exists(src)) throw new IOException("Original Gameplay terrain missing.");
        string[] sources = Directory.GetFiles(src, "*.res");
        if (sources.Length == 0) throw new IOException("No native .res terrain regions found.");
        // A previous failed run can leave staging behind. Clear only files proven to
        // belong to this baker; never remove original Gameplay or unrelated content.
        if (Directory.Exists(stage))
        {
            EnsureGeneratedOnly(stage, sources, true);
            Directory.Delete(stage, true);
        }
        if (Directory.Exists(dst)) EnsureGeneratedOnly(dst, sources, false);
        Directory.CreateDirectory(stage);
        var lines = new List<string> { ManifestVersion };
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

        using var field = new DirtPatchField(Settings, world.VillageOriginXZ, world.SpawnXZ);
        float spacing = terrain.Get("vertex_spacing").AsSingle();
        if (!float.IsFinite(spacing) || spacing <= 0) throw new IOException("Bad Terrain3D vertex_spacing.");
        var exclusions = DirtPatchProtectedAreas.Build(world);
        int changed = 0, regions = 0, manual = 0, holes = 0;
        var expectedControlHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Variant value in locations)
        {
            Vector2I loc = value.AsVector2I();
            var region = data.Call("get_region", loc).AsGodotObject() as Resource
                ?? throw new IOException($"Terrain3D region {loc} could not be loaded.");
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
                    if (DirtPatchProtectedAreas.Contains(exclusions, p)) continue;
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
            string regionFile = Path.GetFileName(region.ResourcePath);
            if (string.IsNullOrEmpty(regionFile) || !File.Exists(Path.Combine(stage, regionFile)))
                throw new IOException($"Cannot identify staged region {loc}: {region.ResourcePath}");
            region.Call("set_map", 1, Image.CreateFromData(size, size, false, Image.Format.Rf, bytes));
            // Terrain3D 1.0.2: edited refreshes maps; modified is separately required for saving.
            region.Set("edited", true);
            region.Set("modified", true);
            expectedControlHashes.Add(regionFile, Convert.ToHexString(SHA256.HashData(bytes)));
            changed += local; regions++;
        }
        if (changed == 0) throw new IOException($"No automatic texels were painted (manual {manual}, hole {holes}). No preview published.");
        data.Call("update_maps", 1, true, false);
        data.Call("save_directory", StageDirectory);
        if (Directory.GetFiles(stage, "*.res").Length != sources.Length)
            throw new IOException("Incomplete save: staging folder retained; original terrain unchanged.");
        // Disk read-back: a positive in-memory texel count is not evidence of a successful save.
        // Ignore ResourceLoader's cache so only the re-opened native .res contents can pass.
        foreach (var entry in expectedControlHashes)
        {
            var saved = ResourceLoader.Load<Resource>(StageDirectory + "/" + entry.Key,
                "Terrain3DRegion", ResourceLoader.CacheMode.Ignore);
            if (saved == null)
                throw new IOException($"Cannot reopen saved dirt region {entry.Key}.");
            var savedMap = saved.Call("get_map", 1).AsGodotObject() as Image;
            if (savedMap == null || savedMap.GetFormat() != Image.Format.Rf ||
                !string.Equals(Convert.ToHexString(SHA256.HashData(savedMap.GetData())),
                    entry.Value, StringComparison.Ordinal))
                throw new IOException($"Dirt control pixels did not persist in {entry.Key}; staging retained, preview not published.");
        }
        PublishGeneratedPreview(stage, dst, sources);
        GD.Print($"DTP1 V3 SUCCESS: {changed} soil texels saved and read back in {regions} regions, manual skipped={manual}, holes skipped={holes}. Preview: {PreviewDirectory}. Open FPS World F6.");
    }

    // =========================================================
    // Replace only a known DTP1 V3 output. The old preview stays available until
    // the new saved data is verified and promoted. A failed publish restores it.
    private static void PublishGeneratedPreview(string stage, string destination, string[] sources)
    {
        string backup = destination + ".previous";
        // Recover an interrupted swap automatically, but only for an owned V3 preview.
        if (Directory.Exists(backup))
        {
            EnsureGeneratedOnly(backup, sources, false);
            if (!Directory.Exists(destination)) Directory.Move(backup, destination);
            else Directory.Delete(backup, true);
        }

        bool hadOld = Directory.Exists(destination);
        if (hadOld)
        {
            EnsureGeneratedOnly(destination, sources, false);
            Directory.Move(destination, backup);
        }
        try
        {
            Directory.Move(stage, destination);
            if (!HasCurrentPreview())
                throw new IOException("Published V3 preview failed manifest verification.");
        }
        catch
        {
            // Move the new generated output back to staging rather than deleting it.
            if (Directory.Exists(destination) && !Directory.Exists(stage))
                Directory.Move(destination, stage);
            if (hadOld && Directory.Exists(backup) && !Directory.Exists(destination))
                Directory.Move(backup, destination);
            throw;
        }

        if (hadOld)
        {
            try { Directory.Delete(backup, true); }
            catch (IOException error)
            { GD.PushWarning("DTP1 V3 replaced the preview, but could not remove its old generated backup: " + error.Message); }
        }
    }

    // =========================================================
    // Never wipe an unfamiliar folder. Only a flat set of known Terrain3D region
    // filenames plus this baker's manifest is considered disposable. No links.
    private static void EnsureGeneratedOnly(string directory, string[] sources, bool staging)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 ||
            Directory.GetDirectories(directory).Length != 0)
            throw new IOException("Refusing to replace a linked or non-flat directory: " + directory);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string source in sources) names.Add(Path.GetFileName(source));
        string manifestPath = Path.Combine(directory, Manifest);
        string[] files = Directory.GetFiles(directory);
        foreach (string file in files)
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to remove a linked preview file: " + file);
            string name = Path.GetFileName(file);
            if (name != Manifest && !names.Contains(name))
                throw new IOException("Unexpected file inside dirt preview; no cleanup performed: " + file);
        }
        if (!File.Exists(manifestPath))
        {
            if (staging) return; // Crash before the manifest was created.
            throw new IOException("Preview is missing the DTP1 ownership manifest: " + directory);
        }

        string[] records = File.ReadAllLines(manifestPath);
        if (records.Length == 0 || (records[0] != "DTP1-v3" && records[0] != ManifestVersion))
            throw new IOException("Preview was not created by the supported DTP1 V3 baker: " + directory);
        for (int i = 1; i < records.Length; i++)
        {
            string[] fields = records[i].Split('|');
            if (fields.Length != 2 || !names.Contains(fields[0]) ||
                fields[1].Length != 64)
                throw new IOException("Invalid DTP1 V3 manifest; refusing to delete preview: " + directory);
        }
    }

    // =========================================================
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
