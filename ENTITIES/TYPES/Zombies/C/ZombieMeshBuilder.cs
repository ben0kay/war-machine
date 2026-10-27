// Original procedural zombie placeholder and cached rig. Imported replacements belong in Artwork/.
using Godot;

public sealed class ZombieMeshBuilder
{
    private readonly Node3D VisualRoot;
    private Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    public Node3D LeftArm => _leftArm;
    public Node3D RightArm => _rightArm;
    public Node3D LeftLeg => _leftLeg;
    public Node3D RightLeg => _rightLeg;
    // =========================================================
    public ZombieMeshBuilder(Node3D root) { VisualRoot = root; BuildArt(); }
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly StandardMaterial3D Skin = new() { AlbedoColor = new Color(0.39f, 0.49f, 0.32f), Roughness = 1 };
    private static readonly StandardMaterial3D Shirt = new() { AlbedoColor = new Color(0.36f, 0.3f, 0.25f), Roughness = 1 };
    private static readonly StandardMaterial3D Pants = new() { AlbedoColor = new Color(0.2f, 0.25f, 0.28f), Roughness = 1 };

    // =========================================================
    // Original low-poly placeholder; shared meshes/materials, cached joints, no per-limb scripts or allocations in gait.
    private void BuildArt()
    {
        Part(VisualRoot, "Torso", new(0, 1.1f, 0), new(0.48f, 0.6f, 0.28f), Shirt);
        Part(VisualRoot, "Head", new(0, 1.56f, -0.04f), new(0.32f, 0.36f, 0.32f), Skin);
        _leftArm = Joint("LeftArm", new(-0.32f, 1.3f, 0)); _rightArm = Joint("RightArm", new(0.32f, 1.3f, 0));
        foreach (var arm in new[] { _leftArm, _rightArm })
        {
            Part(arm, "Sleeve", new(0, -0.15f, 0), new(0.17f, 0.3f, 0.2f), Shirt);
            Part(arm, "Forearm", new(0, -0.42f, 0), new(0.14f, 0.27f, 0.16f), Skin);
        }
        _leftLeg = Joint("LeftLeg", new(-0.13f, 0.82f, 0)); _rightLeg = Joint("RightLeg", new(0.13f, 0.82f, 0));
        foreach (var leg in new[] { _leftLeg, _rightLeg })
            Part(leg, "Leg", new(0, -0.4f, -0.025f), new(0.22f, 0.8f, 0.27f), Pants);
    }

    // =========================================================
    private Node3D Joint(string name, Vector3 position)
    { var joint = new Node3D { Name = name, Position = position }; VisualRoot.AddChild(joint); return joint; }

    // =========================================================
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 scale, Material material)
    { parent.AddChild(new MeshInstance3D { Name = name, Mesh = Box, Position = position, Scale = scale, MaterialOverride = material }); }
}
