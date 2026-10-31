// One-time isolated Terrain3D building-site region with true control-map excavation.
// Owns only DEBUG/Scenes/TerrainData/V2; the world, station prefab and legacy test data are untouched.
using Godot;
using System;
using System.IO;

public partial class BuildingTerrainTest : Node3D
{
    private const int RegionSize = 256;
    // V2 is intentional: V1 already saved an incorrect hole that the old
    // generate-once code would never replace. Keep any V1 edits intact.
    private const string DataDirectory = "res://DEBUG/Scenes/TerrainData/V2";
    // PoliceStation001 is centred at X=128 / Z=128; outer walls occupy
    // X=104..152, Z=96..160. All its ground-floor deck is self-collidable.
    // Terrain3D drops a triangle if *any* corner has the hole bit set.
    // Insets below account for that one-vertex footprint expansion:
    // the resulting absent quads end on the building's outer wall lines,
    // rather than leaving a strip of dirt inside or a gap outside the facade.
    private const int HoleMinX = 105, HoleMaxX = 151;
    private const int HoleMinZ = 97, HoleMaxZ = 159;

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
                GD.Print("BuildingTerrainTest: generated isolated 256m terrain with a 48x64m station excavation.");
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

            // Probe the cutout as well as both outside approach areas.
            // A saved user edit that changes a boundary is reported, never overwritten.
            if (!HasExpectedHole(data))
                GD.PushWarning("BuildingTerrainTest: the saved hole no longer matches the station " +
                    "footprint. Inspect the V2 Terrain3D control map and the entrance thresholds.");
            GD.Print("BuildingTerrainTest: ready, using isolated terrain data in " + DataDirectory);
        }
        catch (Exception error) { GD.PushError("[BuildingTerrainTest] " + error); }
    }

    // =========================================================
    // Import exactly one 256x256 height/control image, matching TerrainBake.cs's
    // native Terrain3DData.import_images() pattern. Terrain stays at the Y=0
    // exterior grade; inside the building the terrain is *removed*, not offset.
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
        if (!HasExpectedHole(data))
            throw new InvalidOperationException(
                "BuildingTerrainTest: terrain hole map is invalid; refusing to save a bad region.");
    }

    // =========================================================
    // One-time spatial assertions: centre of the basement, the rear ground
    // corridor and the two exterior approach pads. All test-world coordinates.
    private static bool HasExpectedHole(GodotObject data)
        => data.Call("get_control_hole", new Vector3(128, 0, 144)).AsBool()
            && data.Call("get_control_hole", new Vector3(128, 0, 110)).AsBool()
            && !data.Call("get_control_hole", new Vector3(128, 0, 164)).AsBool()
            && !data.Call("get_control_hole", new Vector3(128, 0, 92)).AsBool();
}
