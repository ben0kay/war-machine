// Minimal camera-view supporting gloves/wrists for CM2-D; cached per firearm, no callbacks.
using Godot;

public static class ShoveHandsBuilder
{
    private static readonly SphereMesh Glove = new() { Radius = 0.5f, Height = 1, RadialSegments = 10, Rings = 5 };
    private static readonly StandardMaterial3D Material = new() { AlbedoColor = new Color(0.22f, 0.23f, 0.19f), Roughness = 0.95f };

    // =========================================================
    // Grip and support hand move rigidly with the real gun throughout the side-on push.
    public static Node3D Build(Node3D weapon)
    {
        var root = new Node3D { Name = "ShoveHands" }; weapon.AddChild(root);
        foreach (float z in new[] { 0.02f, -0.40f })
            root.AddChild(new MeshInstance3D { Mesh = Glove, MaterialOverride = Material,
                Position = new Vector3(0, -0.10f, z), Scale = new Vector3(0.13f, 0.16f, 0.14f) });
        return root;
    }
}
