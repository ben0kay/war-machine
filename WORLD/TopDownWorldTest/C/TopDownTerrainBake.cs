// Explicit offline preview-only Terrain3D authoring. Fixed output path and overwrite refusal protect existing worlds.
using Godot;
using System;
using System.IO;

public partial class TopDownTerrainBake : Node
{
    // =========================================================
    // Run this dedicated scene once, never from the playable preview's startup path.
    public override void _Ready()
    {
        try { Bake(); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // One reliable 1024m region, gentle relief and a compact hill beside the central clear walking area.
    private void Bake()
    {
        var settings = GD.Load<TopDownTestSettings>("res://WORLD/TopDownWorldTest/Configuration/TopDownTestMaster.tres");
        string directory = ProjectSettings.GlobalizePath(TopDownTestSettings.TerrainDirectory);
        Directory.CreateDirectory(directory);
        if (Directory.GetFiles(directory, "*.res").Length != 0)
            throw new InvalidOperationException("Preview terrain already exists. Bake refuses to overwrite it. Back up and remove only TopDownWorldTest/Terrain/Data/*.res to deliberately rebake.");
        const int size = TopDownTestSettings.TerrainSize;
        Vector2 hill = settings.SpawnXZ + settings.HillOffset;
        if (settings.SpawnXZ.X < 80 || settings.SpawnXZ.Y < 80 || settings.SpawnXZ.X > size - 80 || settings.SpawnXZ.Y > size - 80)
            throw new InvalidOperationException("Keep preview spawn at least 80m from the test region boundary.");
        var terrain = ClassDB.Instantiate("Terrain3D").AsGodotObject() as Node3D
            ?? throw new InvalidOperationException("Terrain3D extension unavailable; import the project in Godot .NET first.");
        terrain.Set("collision_mode", 0); terrain.Set("region_size", size);
        terrain.Set("assets", GD.Load<Resource>("res://WORLD/Terrain/TerrainAssets.tres"));
        terrain.Set("material", GD.Load<Resource>("res://WORLD/Terrain/TerrainMaterial.tres"));
        AddChild(terrain);
        var camera = new Camera3D { Position = new Vector3(settings.SpawnXZ.X, 100, settings.SpawnXZ.Y), Current = true };
        AddChild(camera); terrain.Call("set_camera", camera);
        using var noise = new FastNoiseLite { Seed = settings.Seed, Frequency = 0.009f };
        using var heightmap = Image.CreateEmpty(size, size, false, Image.Format.Rf);
        float radius = Mathf.Clamp(settings.HillRadius, 12, 48);
        for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
        {
            float r2 = new Vector2(x - hill.X, z - hill.Y).LengthSquared() / (radius * radius);
            float rounded = Mathf.Pow(Mathf.Max(0, 1 - r2), 3);
            float height = settings.GroundHeight + noise.GetNoise2D(x, z) * Mathf.Clamp(settings.Relief, 0, 0.5f)
                + rounded * Mathf.Clamp(settings.HillHeight, 0, 8);
            heightmap.SetPixel(x, z, new Color(height, 0, 0));
        }
        var data = terrain.Get("data").AsGodotObject();
        data.Call("import_images", new Godot.Collections.Array { heightmap, default(Variant), default(Variant) }, Vector3.Zero, 0f, 1f);
        data.Call("save_directory", TopDownTestSettings.TerrainDirectory);
        if (Directory.GetFiles(directory, "*.res").Length == 0) throw new IOException("Preview native terrain save produced no region file.");
        GD.Print("Preview terrain created. Open WORLD/TopDownWorldTest/topdownWorldTest.tscn and Run Current Scene (F6).");
    }
}
