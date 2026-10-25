// Explicit offline terrain authoring command. Refuses existing data to protect manual sculpting.
using Godot;
using System;
using System.IO;

public partial class TerrainBake : Node
{
    // =========================================================
    // Build a new saved world only when this authoring scene is deliberately launched.
    public override void _Ready()
    {
        try { Bake(); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Generate deterministic relief, import once and save Terrain3D-native regions and instances.
    private void Bake()
    {
        var config = GD.Load<WorldConfig>("res://WORLD/Configuration/WorldMaster.tres");
        string directory = ProjectSettings.GlobalizePath(config.DataDirectory);
        Directory.CreateDirectory(directory);
        if (Directory.GetFiles(directory, "*.res").Length > 0)
            throw new InvalidOperationException("Terrain already exists. Back it up and choose an EMPTY DataDirectory in WorldMaster before baking. Never overwrite authored data.");
        if (config.Size != 1024 || config.GrassSpacing < 2)
            throw new InvalidOperationException("WE1 bake supports one 1024m region; GrassSpacing must be >= 2.");
        var terrain = ClassDB.Instantiate("Terrain3D").AsGodotObject() as Node3D
            ?? throw new InvalidOperationException("Terrain3D GDExtension unavailable.");
        var camera = new Camera3D { Current = true, Position = new Vector3(512, 200, 620) };
        AddChild(camera);
        terrain.Set("assets", GD.Load<Resource>("res://WORLD/Terrain/TerrainAssets.tres"));
        terrain.Set("material", GD.Load<Resource>("res://WORLD/Terrain/TerrainMaterial.tres"));
        AddChild(terrain);
        terrain.Set("region_size", 1024);
        terrain.Call("set_camera", camera);
        var noise = new FastNoiseLite { Seed = config.Seed, Frequency = config.NoiseFrequency };
        using var image = Image.CreateEmpty(config.Size, config.Size, false, Image.Format.Rf);
        for (int z = 0; z < config.Size; z++)
            for (int x = 0; x < config.Size; x++)
            {
                float valleyCenter = config.Size * 0.5f + 75f * Mathf.Sin(z * 0.006f);
                float ridge = 1f - Mathf.Exp(-Mathf.Pow((x - valleyCenter) / 160f, 2f));
                float height = 12f + config.Elevation * (ridge * 0.65f + (noise.GetNoise2D(x, z) + 1f) * 0.25f);
                image.SetPixel(x, z, new Color(height, 0, 0));
            }
        var data = terrain.Get("data").AsGodotObject();
        data.Call("import_images", new Godot.Collections.Array { image, default(Variant), default(Variant) }, Vector3.Zero, 0f, 1f);
        var random = new RandomNumberGenerator { Seed = (ulong)config.Seed };
        var transforms = new Godot.Collections.Array<Transform3D>();
        for (int z = 8; z < config.Size - 8; z += config.GrassSpacing)
            for (int x = 8; x < config.Size - 8; x += config.GrassSpacing)
            {
                if (random.Randf() > config.GrassCoverage) continue;
                var pos = new Vector3(x + random.RandfRange(-1.5f, 1.5f), 0, z + random.RandfRange(-1.5f, 1.5f));
                float height = (float)data.Call("get_height", pos).AsDouble();
                float dx = image.GetPixel(Mathf.Clamp((int)pos.X + 1, 0, 1023), (int)pos.Z).R - height;
                float dz = image.GetPixel((int)pos.X, Mathf.Clamp((int)pos.Z + 1, 0, 1023)).R - height;
                if (!float.IsFinite(height) || new Vector2(dx, dz).Length() > config.GrassSlopeLimit) continue;
                pos.Y = height;
                float scale = random.RandfRange(0.7f, 1.25f);
                transforms.Add(new Transform3D(new Basis(Vector3.Up, random.RandfRange(0, Mathf.Tau)).Scaled(Vector3.One * scale), pos));
            }
        terrain.Get("instancer").AsGodotObject().Call("add_transforms", 0, transforms);
        data.Call("save_directory", config.DataDirectory);
        if (Directory.GetFiles(directory, "*.res").Length == 0) throw new IOException("Terrain3D did not save regions.");
        GD.Print($"Baked {config.Size}m terrain, seed {config.Seed}, {transforms.Count} grass clumps to {config.DataDirectory}");
    }
}
