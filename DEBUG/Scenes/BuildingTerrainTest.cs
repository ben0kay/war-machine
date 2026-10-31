// One-time isolated Terrain3D test region with a real control-map basement hole.
// Owns only DEBUG/Scenes/TerrainData; the persistent world and building prefab are untouched.
using Godot;
using System;
using System.IO;

public partial class BuildingTerrainTest : Node3D
{
    private const int RegionSize = 256;
    private const string DataDirectory = "res://DEBUG/Scenes/TerrainData";
    // PoliceStation001 sits at (128, 0, 128) inside the test region.
    // Its basement is 48m wide, from local Z=0 to +32, below ground level.
    private const int HoleMinX = 104, HoleMaxX = 152;
    private const int HoleMinZ = 128, HoleMaxZ = 160;

    // =========================================================
    // Load independently saved edits when present. Otherwise seed ONE flat
    // native region with control-map holes and save it; never overwrite edits.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        try
        {
            var terrain = GetNode<Node3D>("Terrain3D");
            var data = terrain.Get("data").AsGodotObject();
            string directory = ProjectSettings.GlobalizePath(DataDirectory);
            Directory.CreateDirectory(directory);

            bool saved = Directory.GetFiles(directory, "*.res").Length != 0;
            int regions = data.Call("get_region_count").AsInt32();
            if (!saved)
            {
                if (regions != 0)
                    throw new InvalidOperationException(
                        "BuildingTerrainTest: unsaved Terrain3D regions already exist; refusing to replace them.");
                CreateFirstRegion(data);
                data.Call("save_directory", DataDirectory);
                if (Directory.GetFiles(directory, "*.res").Length == 0)
                    throw new IOException(
                        "BuildingTerrainTest: native Terrain3D region was not saved.");
                GD.Print("BuildingTerrainTest: created isolated 256m terrain with a 48x32m basement hole.");
            }
            else if (regions == 0)
            {
                throw new InvalidOperationException(
                    "BuildingTerrainTest: saved terrain files exist but no Terrain3D region loaded. " +
                    "Check the test Terrain3D DataDirectory; saved data was not modified.");
            }

            var camera = GetNode<Camera3D>("BuildingTestPlayer/ViewPivot/Camera3D");
            terrain.Call("set_camera", camera);
            var collision = terrain.Get("collision").AsGodotObject();
            collision.Call("set_mode", 3); // FULL_GAME, as in the existing WorldController.
            collision.Call("build"); // Native collision must reflect the generated/loaded hole.

            if (!data.Call("get_control_hole", new Vector3(128, 0, 144)).AsBool())
                GD.PushWarning("BuildingTerrainTest: basement centre is not a terrain hole. " +
                    "Inspect the test region control map in the Terrain3D editor.");
            GD.Print("BuildingTerrainTest: ready, using isolated terrain data in " + DataDirectory);
        }
        catch (Exception error) { GD.PushError("[BuildingTerrainTest] " + error); }
    }

    // =========================================================
    // Import exactly one 256x256 height/control image, matching TerrainBake.cs's
    // validated native Terrain3DData.import_images() pattern. All heights are Y=0.
    private static void CreateFirstRegion(GodotObject data)
    {
        using var height = Image.CreateEmpty(RegionSize, RegionSize, false, Image.Format.Rf);
        using var control = Image.CreateEmpty(RegionSize, RegionSize, false, Image.Format.Rf);
        height.Fill(new Color(0, 0, 0, 1));
        control.Fill(new Color(0, 0, 0, 1));

        // Terrain3D's control map bit #2 (value 4) marks a *physical*
        // terrain hole. Bit reinterpretation is essential: don't use 4.0f.
        // This uses the same 32-bit RF representation as Terrain3DUtil.as_float().
        float holeBits = BitConverter.Int32BitsToSingle(4);
        for (int z = HoleMinZ; z <= HoleMaxZ; z++)
            for (int x = HoleMinX; x <= HoleMaxX; x++)
                control.SetPixel(x, z, new Color(holeBits, 0, 0, 1));

        data.Call("import_images",
            new Godot.Collections.Array { height, control, default(Variant) },
            Vector3.Zero, 0f, 1f);
        if (data.Call("get_region_count").AsInt32() != 1)
            throw new InvalidOperationException(
                "BuildingTerrainTest: expected exactly one native Terrain3D region after import.");
    }
}
