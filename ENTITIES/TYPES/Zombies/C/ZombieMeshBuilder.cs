// Procedural low-poly zombie: decayed human silhouette, worn clothing and cached shamble joints.
// Pure artwork construction; physics, health, sensing and gameplay live on ZombieController.
using Godot;

public sealed class ZombieMeshBuilder
{
    private readonly Node3D _root;
    private Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Node3D _leftKnee = null!, _rightKnee = null!;
    public Node3D LeftArm => _leftArm;
    public Node3D RightArm => _rightArm;
    public Node3D LeftLeg => _leftLeg;
    public Node3D RightLeg => _rightLeg;
    public Node3D LeftKnee => _leftKnee;
    public Node3D RightKnee => _rightKnee;

    // Shared original geometry and separate wound/face feature materials. ZMAT0 palette textures
    // are generated only once by ZombieAppearance and reused across ordinary zombies and runners.
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1f, RadialSegments = 10, Rings = 5 };
    private static readonly Mesh Torso = ZombieHumanGeometry.Torso;
    private static readonly Material Bruise = Solid(new(0.24f, 0.26f, 0.23f));
    private static readonly Material Hair = Solid(new(0.17f, 0.18f, 0.15f));
    private static readonly Material Boots = Solid(new(0.15f, 0.16f, 0.15f));

    private static Node3D? _visualTemplate;

    // =========================================================
    // Only the first shambler constructs source primitives; all later bodies share the batched meshes.
    public ZombieMeshBuilder(Node3D root)
    {
        _root = root;
        if (_visualTemplate == null)
        { BuildArt(); ZombieUpperBodyRig.Attach(root); _visualTemplate = ZombieVisualBatcher.Capture(root); }
        else ZombieVisualBatcher.Populate(root, _visualTemplate);
        _leftArm = root.GetNode<Node3D>("UpperBody/LeftArm"); _rightArm = root.GetNode<Node3D>("UpperBody/RightArm");
        _leftLeg = root.GetNode<Node3D>("LeftLeg"); _rightLeg = root.GetNode<Node3D>("RightLeg");
        _leftKnee = _leftLeg.GetNode<Node3D>("Knee"); _rightKnee = _rightLeg.GetNode<Node3D>("Knee");
        ZombieAppearance.Apply(root, unchecked((uint)root.GetInstanceId()));
    }

    // =========================================================
    // Materials use an instance-based temporary preview until world placement supplies its stable seed.
    // All colours/texture objects are shared across zombies, not duplicated per body.
    private void BuildArt()
    {
        var appearance = ZombieAppearance.Choose(unchecked((uint)_root.GetInstanceId()));
        Material skin = appearance.Skin, face = appearance.Face;
        Material shirt = appearance.Shirt, pants = appearance.Pants;

        // Tapered, slightly hunched chest and narrow waist replace the Minecraft-like cuboid.
        Part(_root, "SlouchedTorso", new(0, 1.105f, -0.025f), new(0.47f, 0.59f, 0.31f), shirt, Torso, new(7, 0, 3));
        Part(_root, "ShoulderShape", new(0, 1.29f, -0.055f), new(0.50f, 0.28f, 0.30f), shirt, Rounded);
        Part(_root, "Hips", new(0, 0.83f, 0.015f), new(0.36f, 0.18f, 0.28f), pants, Rounded);
        Part(_root, "ExposedNeck", new(0.02f, 1.42f, -0.035f), new(0.16f, 0.16f, 0.18f), skin, Rounded);
        Part(_root, "TornCollar", new(-0.10f, 1.405f, -0.125f), new(0.16f, 0.075f, 0.075f), shirt, Rounded, new(0, 0, -18));
        Part(_root, "ShirtHem", new(0.09f, 0.824f, -0.112f), new(0.24f, 0.13f, 0.085f), shirt, Rounded, new(0, 0, -9));

        // Small head tilt, sunken sockets, discoloured jaw and imperfect hairline.
        var head = Joint(_root, "Head", new(0.022f, 1.55f, -0.047f));
        head.RotationDegrees = new Vector3(-8, 0, 8);
        Part(head, "Skull", Vector3.Zero, new(0.274f, 0.338f, 0.246f), face, Rounded);
        Part(head, "Jaw", new(0.0f, -0.126f, -0.042f), new(0.178f, 0.125f, 0.174f), face, Rounded);
        Part(head, "Nose", new(0.0f, -0.005f, -0.156f), new(0.055f, 0.085f, 0.058f), face, Rounded);
        Part(head, "LeftSocket", new(-0.082f, 0.041f, -0.129f), new(0.071f, 0.037f, 0.019f), Bruise, Rounded);
        Part(head, "RightSocket", new(0.085f, 0.025f, -0.129f), new(0.071f, 0.038f, 0.019f), Bruise, Rounded);
        Part(head, "OpenMouth", new(-0.002f, -0.119f, -0.149f), new(0.083f, 0.018f, 0.014f), Hair, Rounded);

        // Existing animation pivots are kept exactly at shoulder/hip positions and names.
        _leftArm = Joint(_root, "LeftArm", new(-0.32f, 1.30f, 0));
        _rightArm = Joint(_root, "RightArm", new(0.32f, 1.30f, 0));
        BuildArm(_leftArm, true, shirt, skin);
        BuildArm(_rightArm, false, shirt, skin);

        _leftLeg = Joint(_root, "LeftLeg", new(-0.13f, 0.82f, 0));
        _rightLeg = Joint(_root, "RightLeg", new(0.13f, 0.82f, 0));
        _leftKnee = BuildLeg(_leftLeg, true, pants);
        _rightKnee = BuildLeg(_rightLeg, false, pants);
    }

    // =========================================================
    // Asymmetric sleeves and articulated forearms remain; tiny finger/bruise draws are removed.
    private static void BuildArm(Node3D arm, bool left, Material shirt, Material skin)
    {
        Part(arm, "Shoulder", new(0, -0.065f, 0), new(0.224f, 0.214f, 0.218f), shirt, Rounded);
        Part(arm, "RaggedSleeve", new(0, left ? -0.18f : -0.135f, 0),
            new(0.181f, left ? 0.29f : 0.22f, 0.195f), shirt, Rounded);
        Part(arm, "ExposedForearm", new(0, left ? -0.375f : -0.34f, 0),
            new(0.142f, left ? 0.29f : 0.34f, 0.165f), skin, Rounded);
        // A slightly longer shared hand contour replaces two tiny finger draw calls.
        Part(arm, "Hand", new(0, -0.545f, -0.018f), new(0.118f, 0.167f, 0.128f), skin, Rounded, new(left ? 8 : -5, 0, left ? 7 : -7));
    }

    // =========================================================
    // Cached knee pivots preserve the original hip position and support a modest, one-clock shamble.
    private static Node3D BuildLeg(Node3D leg, bool left, Material pants)
    {
        Part(leg, "Thigh", new(0, -0.18f, 0), new(0.223f, 0.375f, 0.251f), pants, Rounded);
        if (left)
            Part(leg, "TornTrouserPatch", new(-0.108f, -0.205f, -0.056f),
                new(0.04f, 0.16f, 0.124f), Bruise, Rounded);
        var knee = Joint(leg, "Knee", new(0, -0.36f, 0));
        Part(knee, "KneeJoint", Vector3.Zero, new(0.151f, 0.130f, 0.172f), pants, Rounded);
        var lower = Joint(knee, "LowerLegShape", Vector3.Zero);
        lower.Rotation = new Vector3(left ? -0.10f : 0.03f, 0, left ? -0.03f : 0);
        Part(lower, "Shin", new(0, -0.17f, 0), new(0.178f, 0.35f, 0.195f), pants, Rounded);
        var ankle = Joint(lower, "Ankle", new(0, -0.34f, 0));
        // Keep a shoe toe silhouette with one longer mesh rather than a separate toe instance.
        Part(ankle, "ShoeUpper", new(0, 0.013f, -0.042f), new(0.192f, 0.145f, 0.310f), Boots, Rounded);
        return knee;
    }

    // =========================================================
    private static Node3D Joint(Node3D parent, string name, Vector3 position)
    { var joint = new Node3D { Name = name, Position = position }; parent.AddChild(joint); return joint; }

    // =========================================================
    private static Material Solid(Color color)
    { return new StandardMaterial3D { AlbedoColor = color, Roughness = 0.96f }; }

    // =========================================================
    // Visual meshes do not collide, think or process. Shared geometry/materials remain cached.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size,
        Material material, Mesh? shape = null, Vector3? rotation = null)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = ZombieHumanGeometry.Shape(name, shape ?? Box), Position = position, Scale = size,
            RotationDegrees = rotation ?? Vector3.Zero, MaterialOverride = material
        });
    }
}
