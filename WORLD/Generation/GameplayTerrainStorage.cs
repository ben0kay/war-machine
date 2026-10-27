// One-time non-destructive migration of user-local prototype terrain; no terrain regeneration or normal-world writes.
using Godot;
using System.IO;

public static class GameplayTerrainStorage
{
    // =========================================================
    // Native preview regions were not checked in, so Git cannot move a user's existing bake for them.
    public static void CopyLegacyBakeIfNeeded()
    {
        string destination = ProjectSettings.GlobalizePath(WorldSettings.TerrainDirectory);
        string legacy = ProjectSettings.GlobalizePath("res://WORLD/TopDownWorldTest/Terrain/Data");
        if (!Directory.Exists(legacy)) return;
        if (Directory.Exists(destination) && Directory.GetFiles(destination, "*.res").Length > 0) return;
        var files = Directory.GetFiles(legacy, "*.res");
        if (files.Length == 0) return;
        Directory.CreateDirectory(destination);
        foreach (string file in files) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), false);
        GD.Print("Copied existing prototype terrain to WORLD/Terrain/Gameplay; originals retained.");
    }
}
