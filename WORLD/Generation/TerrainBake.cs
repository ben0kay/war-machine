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
        if ((config.Size < 1024 || config.Size > 2048 || config.Size % 1024 != 0) || config.GrassSpacing < 2)
            throw new InvalidOperationException("Bake supports 1024m or 2048m landscapes; GrassSpacing must be >= 2.");
        var terrain = ClassDB.Instantiate("Terrain3D").AsGodotObject() as Node3D
            ?? throw new InvalidOperationException("Terrain3D GDExtension unavailable.");
        var camera = new Camera3D { Current = true, Position = new Vector3(config.SpawnXZ.X, 300, config.SpawnXZ.Y) };
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
                float valleyCenter = config.Size * 0.5f + 140f * Mathf.Sin(z * 0.003f);
                float fromCenter = Mathf.Abs(x - valleyCenter);
                float ridge = Mathf.SmoothStep(config.ValleyWidth * 0.6f, config.ValleyWidth * 2f, fromCenter);
                float mountain = 12f + config.Elevation * (ridge * 0.75f + (noise.GetNoise2D(x, z) + 1f) * 0.22f);
                float meadow = 1 - Mathf.SmoothStep(config.ValleyWidth * 0.55f, config.ValleyWidth, fromCenter);
                // Broad connected meadow floors with low rolling relief, blended into mountain shoulders.
                float floor = 24f + noise.GetNoise2D(x * 0.45f, z * 0.45f) * config.MeadowRelief;
                float height = Mathf.Lerp(mountain, floor, meadow * config.MeadowBlend);
                image.SetPixel(x, z, new Color(height, 0, 0));
            }
        var data = terrain.Get("data").AsGodotObject();
        data.Call("import_images", new Godot.Collections.Array { image, default(Variant), default(Variant) }, Vector3.Zero, 0f, 1f);
        data.Call("save_directory", config.DataDirectory);
        if (Directory.GetFiles(directory, "*.res").Length == 0) throw new IOException("Terrain3D did not save regions.");
        GD.Print($"Baked {config.Size}m terrain, seed {config.Seed}, GPU grass runtime placement to {config.DataDirectory}");
    }
}
