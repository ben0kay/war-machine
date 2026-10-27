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

    // Shared geometry, low-resolution fabric/skin patterns and three muted outfit variants.
    // Instances only add transforms; no unique textures, physics or animation processors.
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1f, RadialSegments = 10, Rings = 5 };
    private static readonly CylinderMesh Torso = new() { TopRadius = 0.5f, BottomRadius = 0.37f, Height = 1f, RadialSegments = 10, Rings = 1 };
    private static readonly Material[] Skin =
    {
        Mottled(new(0.42f, 0.47f, 0.36f), new(0.31f, 0.35f, 0.28f), 19),
        Mottled(new(0.54f, 0.51f, 0.41f), new(0.36f, 0.39f, 0.31f), 29),
        Mottled(new(0.37f, 0.43f, 0.39f), new(0.25f, 0.30f, 0.30f), 41)
    };
    private static readonly Material[] Shirts =
    {
        Mottled(new(0.33f, 0.30f, 0.25f), new(0.17f, 0.21f, 0.18f), 61),
        Mottled(new(0.27f, 0.32f, 0.30f), new(0.16f, 0.21f, 0.22f), 77),
        Mottled(new(0.35f, 0.29f, 0.27f), new(0.23f, 0.21f, 0.21f), 89)
    };
    private static readonly Material[] Pants =
    {
        Mottled(new(0.25f, 0.29f, 0.30f), new(0.15f, 0.19f, 0.20f), 101),
        Mottled(new(0.29f, 0.28f, 0.24f), new(0.19f, 0.21f, 0.19f), 113),
        Mottled(new(0.22f, 0.26f, 0.25f), new(0.16f, 0.18f, 0.18f), 127)
    };
    private static readonly Material Bruise = Solid(new(0.24f, 0.26f, 0.23f));
    private static readonly Material Wound = Solid(new(0.34f, 0.19f, 0.20f));
    private static readonly Material Eye = Solid(new(0.73f, 0.72f, 0.53f));
    private static readonly Material Hair = Solid(new(0.17f, 0.18f, 0.15f));
    private static readonly Material Boots = Solid(new(0.15f, 0.16f, 0.15f));

    // =========================================================
    public ZombieMeshBuilder(Node3D root) { _root = root; BuildArt(); }

    // =========================================================
    // Only material selection varies per actor; all generated geometry is shared and assembled once.
    private void BuildArt()
    {
        int variant = (int)(_root.GetInstanceId() % (ulong)Shirts.Length);
        Material skin = Skin[variant], shirt = Shirts[variant], pants = Pants[variant];

        // Tapered, slightly hunched chest and narrow waist replace the Minecraft-like cuboid.
        Part(_root, "SlouchedTorso", new(0, 1.105f, -0.025f), new(0.47f, 0.59f, 0.31f), shirt, Torso, new(7, 0, 3));
        Part(_root, "ShoulderShape", new(0, 1.29f, -0.055f), new(0.50f, 0.28f, 0.30f), shirt, Rounded);
        Part(_root, "Hips", new(0, 0.83f, 0.015f), new(0.36f, 0.18f, 0.28f), pants, Rounded);
        Part(_root, "ExposedNeck", new(0.02f, 1.42f, -0.035f), new(0.16f, 0.16f, 0.18f), skin, Rounded);
        Part(_root, "TornCollar", new(-0.10f, 1.405f, -0.125f), new(0.16f, 0.075f, 0.075f), shirt, Rounded, new(0, 0, -18));
        Part(_root, "ShirtHem", new(0.09f, 0.824f, -0.112f), new(0.24f, 0.13f, 0.085f), shirt, Rounded, new(0, 0, -9));
        Part(_root, "FabricTear", new(-0.11f, 1.02f, -0.174f), new(0.15f, 0.075f, 0.022f), Wound, Rounded, new(0, 0, 17));

        // Small head tilt, sunken sockets, discoloured jaw and imperfect hairline.
        var head = Joint(_root, "Head", new(0.022f, 1.55f, -0.047f));
        head.RotationDegrees = new Vector3(-8, 0, 8);
        Part(head, "Skull", Vector3.Zero, new(0.295f, 0.34f, 0.295f), skin, Rounded);
        Part(head, "Jaw", new(0.0f, -0.126f, -0.042f), new(0.235f, 0.125f, 0.223f), skin, Rounded);
        Part(head, "Nose", new(0.0f, -0.005f, -0.156f), new(0.083f, 0.105f, 0.08f), skin, Rounded);
        Part(head, "LeftSocket", new(-0.082f, 0.041f, -0.129f), new(0.105f, 0.088f, 0.034f), Bruise, Rounded);
        Part(head, "RightSocket", new(0.085f, 0.025f, -0.129f), new(0.102f, 0.097f, 0.035f), Bruise, Rounded);
        Part(head, "LeftEye", new(-0.083f, 0.037f, -0.153f), new(0.039f, 0.03f, 0.016f), Eye, Rounded);
        Part(head, "RightEye", new(0.089f, 0.022f, -0.154f), new(0.03f, 0.024f, 0.015f), Eye, Rounded);
        Part(head, "OpenMouth", new(-0.002f, -0.119f, -0.149f), new(0.142f, 0.046f, 0.023f), Hair, Rounded);
        Part(head, "TempleBruise", new(0.126f, -0.02f, -0.067f), new(0.041f, 0.09f, 0.09f), Wound, Rounded);
        Part(head, "HairPatch", new(-0.04f, 0.15f, 0.009f), new(0.23f, 0.085f, 0.235f), Hair, Rounded);

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
    // Uneven sleeves, mottled forearms and hanging hands read as decayed rather than identical mannequin limbs.
    private static void BuildArm(Node3D arm, bool left, Material shirt, Material skin)
    {
        Part(arm, "Shoulder", new(0, -0.065f, 0), new(0.224f, 0.214f, 0.218f), shirt, Rounded);
        Part(arm, "RaggedSleeve", new(0, left ? -0.18f : -0.135f, 0),
            new(0.181f, left ? 0.29f : 0.22f, 0.195f), shirt, Rounded);
        Part(arm, "ExposedForearm", new(0, left ? -0.375f : -0.34f, 0),
            new(0.142f, left ? 0.29f : 0.34f, 0.165f), skin, Rounded);
        Part(arm, "Hand", new(0, -0.535f, -0.018f), new(0.142f, 0.145f, 0.162f), skin, Rounded, new(left ? 8 : -5, 0, left ? 7 : -7));
        Part(arm, "ArmBruise", new(left ? -0.074f : 0.073f, -0.365f, -0.008f),
            new(0.024f, 0.096f, 0.099f), left ? Wound : Bruise, Rounded);
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
        Part(knee, "KneeJoint", Vector3.Zero, new(0.188f, 0.18f, 0.218f), pants, Rounded);
        Part(knee, "Shin", new(0, -0.17f, 0), new(0.187f, 0.35f, 0.215f), pants, Rounded);
        var ankle = Joint(knee, "Ankle", new(0, -0.34f, 0));
        Part(ankle, "ShoeUpper", new(0, 0.028f, 0.003f), new(0.20f, 0.175f, 0.216f), Boots, Rounded);
        Part(ankle, "ShoeToe", new(0, -0.035f, -0.097f), new(0.224f, 0.115f, 0.316f), Boots, Rounded);
        return knee;
    }

    // =========================================================
    private static Node3D Joint(Node3D parent, string name, Vector3 position)
    { var joint = new Node3D { Name = name, Position = position }; parent.AddChild(joint); return joint; }

    // =========================================================
    private static Material Solid(Color color)
    { return new StandardMaterial3D { AlbedoColor = color, Roughness = 0.96f }; }

    // =========================================================
    // Static 64px mottling softens clothing/skin flatness. Never generates textures during gameplay frames.
    private static Material Mottled(Color main, Color stained, int seed)
    {
        const int size = 64;
        using var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float patch = Noise(x / 11f, y / 11f, seed) * 0.7f
                + Noise(x / 4.5f, y / 4.5f, seed + 31) * 0.3f;
            float stain = Mathf.SmoothStep(0.30f, 0.74f, patch) * 0.7f;
            float fleck = (Hash(x, y, seed + 103) - 0.5f) * 0.055f;
            Color color = main.Lerp(stained, stain);
            image.SetPixel(x, y, new Color(
                Mathf.Clamp(color.R + fleck, 0, 1),
                Mathf.Clamp(color.G + fleck, 0, 1),
                Mathf.Clamp(color.B + fleck, 0, 1)));
        }
        image.GenerateMipmaps();
        return new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(image), Roughness = 0.96f };
    }

    // =========================================================
    private static float Noise(float x, float y, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float dx = x - ix, dy = y - iy;
        dx = dx * dx * (3 - 2 * dx); dy = dy * dy * (3 - 2 * dy);
        return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy, seed), Hash(ix + 1, iy, seed), dx),
            Mathf.Lerp(Hash(ix, iy + 1, seed), Hash(ix + 1, iy + 1, seed), dx), dy);
    }

    // =========================================================
    private static float Hash(int x, int y, int seed)
    {
        uint n = unchecked((uint)(x * 374761393 ^ y * 668265263 ^ seed * 1274126177));
        n ^= n >> 13; n *= 1274126177u; n ^= n >> 16;
        return (n & 0xffff) / 65535f;
    }

    // =========================================================
    // Visual meshes do not collide, think or process. Shared primitives/materials remain reusable across zombies.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size,
        Material material, Mesh? shape = null, Vector3? rotation = null)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = shape ?? Box, Position = position, Scale = size,
            RotationDegrees = rotation ?? Vector3.Zero, MaterialOverride = material
        });
    }
}
