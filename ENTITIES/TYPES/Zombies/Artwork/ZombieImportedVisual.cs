// Imported segmented art uses the existing C# pivot rig; no additional animation or physics callbacks.
using Godot;

public static class ZombieImportedVisual
{
    private const string Path = "res://ENTITIES/TYPES/Zombies/Artwork/ZombieFirstPass.glb";
    private static PackedScene? _scene;
    private static bool _attempted;

    // =========================================================
    // Load once; transfer direct rig children so all existing animation/anatomy/corpse paths remain valid.
    public static bool TryPopulate(Node3D root)
    {
        if (!_attempted)
        {
            _attempted = true;
            if (ResourceLoader.Exists(Path)) _scene = GD.Load<PackedScene>(Path);
            if (_scene == null) GD.PushWarning("Zombie GLB unavailable: using procedural artwork. Import the project in Godot first.");
        }
        if (_scene == null) return false;
        var imported = _scene.Instantiate<Node3D>();
        // Importers may preserve the authored scene root or introduce a wrapper.
        Node3D? rig = imported.HasNode("UpperBody") ? imported : imported.GetNodeOrNull<Node3D>("ZombieFirstPass");
        if (rig == null || !rig.HasNode("UpperBody/Head") || !rig.HasNode("UpperBody/Hips")
            || !rig.HasNode("UpperBody/LeftArm/Elbow") || !rig.HasNode("UpperBody/RightArm/Elbow")
            || !rig.HasNode("LeftLeg/Knee") || !rig.HasNode("RightLeg/Knee"))
        {
            GD.PushWarning("Zombie GLB rig does not match the animation contract; using procedural artwork.");
            imported.Free(); return false;
        }
        foreach (Node child in rig.GetChildren()) { rig.RemoveChild(child); root.AddChild(child); }
        imported.Free();
        return true;
    }
}
