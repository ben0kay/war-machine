// Temporary original low-poly rifle art only. Gameplay is entirely in the shared firearm controller.
using Godot;

public partial class RifleMeshBuilder : Node3D
{
    // =========================================================
    // Grip origin is at the root; the muzzle and all barrels point toward local -Z.
    public override void _Ready()
    {
        var metal = new StandardMaterial3D { AlbedoColor = new Color(0.12f, 0.16f, 0.18f), Metallic = 0.65f, Roughness = 0.55f };
        var stock = new StandardMaterial3D { AlbedoColor = new Color(0.26f, 0.3f, 0.24f), Roughness = 0.9f };
        Part("Receiver", new(0, 0.045f, -0.15f), new(0.095f, 0.13f, 0.42f), metal);
        Part("Stock", new(0, 0.015f, 0.22f), new(0.09f, 0.16f, 0.30f), stock);
        Part("Grip", new(0, -0.09f, 0.015f), new(0.07f, 0.18f, 0.09f), stock);
        Part("Magazine", new(0, -0.12f, -0.17f), new(0.065f, 0.22f, 0.12f), metal);
        Part("Handguard", new(0, 0.035f, -0.43f), new(0.105f, 0.105f, 0.27f), stock);
        Part("Barrel", new(0, 0.055f, -0.67f), new(0.035f, 0.035f, 0.25f), metal);
        Part("FrontSight", new(0, 0.105f, -0.72f), new(0.025f, 0.09f, 0.035f), metal);
        Part("RearSight", new(0, 0.13f, -0.04f), new(0.055f, 0.035f, 0.07f), metal);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Replace this visual child with an Artwork scene later; keep the root and Muzzle marker contract.
    private void Part(string name, Vector3 position, Vector3 size, Material material)
    { AddChild(new MeshInstance3D { Name = name, Position = position, Mesh = new BoxMesh { Size = size }, MaterialOverride = material }); }
}
