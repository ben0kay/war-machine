// Zombie 002 visual art: a leaner, longer-legged infected runner with torn clothing.
// Reuses the six named movement pivots of Zombie 001; all meshes/materials are static/shared.
using Godot;

public sealed class ZombieRunnerMeshBuilder
{
    private readonly Node3D _root;
    public Node3D LeftArm { get; private set; } = null!;
    public Node3D RightArm { get; private set; } = null!;
    public Node3D LeftLeg { get; private set; } = null!;
    public Node3D RightLeg { get; private set; } = null!;
    public Node3D LeftKnee { get; private set; } = null!;
    public Node3D RightKnee { get; private set; } = null!;

    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1, RadialSegments = 10, Rings = 5 };
    private static readonly Mesh Torso = ZombieHumanGeometry.Torso;
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly Material Skin = Paint(new Color(0.48f, 0.49f, 0.39f));
    private static readonly Material SkinShadow = Paint(new Color(0.31f, 0.34f, 0.29f));
    private static readonly Material Shirt = Paint(new Color(0.28f, 0.33f, 0.28f));
    private static readonly Material ShirtDark = Paint(new Color(0.18f, 0.24f, 0.21f));
    private static readonly Material Pants = Paint(new Color(0.25f, 0.27f, 0.28f));
    private static readonly Material Boots = Paint(new Color(0.13f, 0.16f, 0.16f));
    private static readonly Material Bruise = Paint(new Color(0.31f, 0.20f, 0.20f));
    private static readonly Material Eyes = Paint(new Color(0.30f, 0.34f, 0.29f));

    // =========================================================
    public ZombieRunnerMeshBuilder(Node3D root) { _root = root; Build(); }

    // =========================================================
    // Compact, recognisably different silhouette: narrower torso, longer-looking legs and stripped-down clothes.
    private void Build()
    {
        Part(_root, "LeanTorso", new(0, 1.13f, -0.035f), new(0.397f, 0.55f, 0.275f), Shirt, Torso, new(8, 0, -3));
        Part(_root, "NarrowShoulders", new(0, 1.32f, -0.055f), new(0.435f, 0.235f, 0.273f), Shirt, Rounded);
        Part(_root, "Waist", new(0, 0.847f, 0.012f), new(0.285f, 0.173f, 0.256f), Pants, Rounded);
        Part(_root, "BentNeck", new(0.02f, 1.438f, -0.064f), new(0.13f, 0.20f, 0.149f), Skin, Rounded);
        Part(_root, "TornShirtHem", new(-0.09f, 0.870f, -0.12f), new(0.14f, 0.105f, 0.06f), ShirtDark, Rounded);
        Part(_root, "ShoulderTear", new(0.18f, 1.30f, -0.116f), new(0.12f, 0.09f, 0.062f), SkinShadow, Rounded);
        Part(_root, "ChestStain", new(-0.072f, 1.12f, -0.174f), new(0.18f, 0.17f, 0.018f), Bruise, Rounded, new(0, 0, 18));
        Part(_root, "ShirtRip", new(0.116f, 1.04f, -0.143f), new(0.09f, 0.052f, 0.026f), SkinShadow, Rounded);

        // Slightly smaller angular head, sunken eye sockets, prominent brow and discoloured cheek.
        var head = Pivot(_root, "Head", new(0.014f, 1.54f, -0.055f));
        head.RotationDegrees = new Vector3(-8, -7, -7);
        Part(head, "Skull", Vector3.Zero, new(0.236f, 0.315f, 0.225f), Skin, Rounded);
        Part(head, "Jaw", new(0, -0.127f, -0.035f), new(0.165f, 0.119f, 0.167f), SkinShadow, Rounded);
        Part(head, "Brow", new(0, 0.072f, -0.124f), new(0.190f, 0.036f, 0.043f), SkinShadow, Rounded);
        Part(head, "LeftEye", new(-0.068f, 0.024f, -0.141f), new(0.016f, 0.011f, 0.008f), Eyes, Rounded);
        Part(head, "RightEye", new(0.074f, 0.022f, -0.143f), new(0.016f, 0.011f, 0.008f), Eyes, Rounded);
        Part(head, "OpenMouth", new(0, -0.131f, -0.139f), new(0.072f, 0.016f, 0.014f), Boots, Rounded);
        Part(head, "BruisedCheek", new(-0.107f, -0.045f, -0.092f), new(0.057f, 0.10f, 0.036f), Bruise, Rounded);

        LeftArm = Pivot(_root, "LeftArm", new(-0.302f, 1.305f, 0));
        RightArm = Pivot(_root, "RightArm", new(0.302f, 1.305f, 0));
        Arm(LeftArm, true); Arm(RightArm, false);
        LeftLeg = Pivot(_root, "LeftLeg", new(-0.119f, 0.82f, 0));
        RightLeg = Pivot(_root, "RightLeg", new(0.119f, 0.82f, 0));
        LeftKnee = Leg(LeftLeg, true); RightKnee = Leg(RightLeg, false);
        // Give standalone runner previews the same shared appearance pool as Zombie_001.
        // InitializeTarget replaces this provisional selection with the stable world spawn seed.
        ZombieAppearance.Apply(_root, unchecked((uint)_root.GetInstanceId()));
    }

    // =========================================================
    // Asymmetric ripped shirt sleeves and exposed forearms; simple arm rotations provide the running swing.
    private static void Arm(Node3D arm, bool left)
    {
        Part(arm, "Shoulder", new(0, -0.055f, 0), new(0.192f, 0.19f, 0.190f), Shirt, Rounded);
        Part(arm, "Sleeve", new(0, left ? -0.145f : -0.19f, 0),
            new(0.162f, left ? 0.21f : 0.285f, 0.182f), left ? ShirtDark : Shirt, Rounded);
        Part(arm, "Forearm", new(0, -0.368f, 0), new(0.123f, 0.298f, 0.142f), Skin, Rounded);
        Part(arm, "Hand", new(0, -0.531f, -0.024f), new(0.104f, 0.123f, 0.116f), SkinShadow, Rounded);
        for (int i = 0; i < 2; i++)
            Part(arm, "Finger", new(i == 0 ? -0.027f : 0.024f, -0.612f + i * 0.01f, -0.030f),
                new(0.030f, 0.068f - i * 0.012f, 0.034f), Skin, ZombieHumanGeometry.LowerLimb);
        Part(arm, "ElbowBruise", new(left ? -0.06f : 0.065f, -0.324f, -0.018f),
            new(0.028f, 0.085f, 0.08f), Bruise, Rounded);
    }

    // =========================================================
    // Knees have the same rest pivots as the shambler, but slimmer trousers and trainers for a quick silhouette.
    private static Node3D Leg(Node3D leg, bool left)
    {
        Part(leg, "LongThigh", new(0, -0.173f, 0), new(0.187f, 0.374f, 0.211f), Pants, Rounded);
        if (left) Part(leg, "TornKnee", new(-0.052f, -0.303f, -0.104f),
            new(0.093f, 0.09f, 0.026f), SkinShadow, Rounded);
        var knee = Pivot(leg, "Knee", new(0, -0.36f, 0));
        Part(knee, "KneeJoint", Vector3.Zero, new(0.141f, 0.125f, 0.158f), Pants, Rounded);
        var lower = Pivot(knee, "LowerLegShape", Vector3.Zero);
        lower.Rotation = new Vector3(left ? -0.045f : 0.025f, 0, left ? 0.025f : -0.020f);
        Part(lower, "LongShin", new(0, -0.163f, 0), new(0.154f, 0.347f, 0.171f), Pants, Rounded);
        var ankle = Pivot(lower, "Ankle", new(0, -0.34f, 0));
        Part(ankle, "Shoe", new(0, 0.008f, -0.047f), new(0.177f, 0.135f, 0.255f), Boots, Rounded);
        Part(ankle, "Toe", new(0, -0.04f, -0.127f), new(0.188f, 0.067f, 0.178f), Boots, Rounded);
        return knee;
    }

    // =========================================================
    private static Node3D Pivot(Node3D parent, string name, Vector3 offset)
    { var node = new Node3D { Name = name, Position = offset }; parent.AddChild(node); return node; }

    // =========================================================
    private static StandardMaterial3D Paint(Color color) => new() { AlbedoColor = color, Roughness = 0.97f };

    // =========================================================
    private static void Part(Node3D parent, string name, Vector3 pos, Vector3 scale, Material material,
        Mesh? mesh = null, Vector3? rotation = null)
    {
        parent.AddChild(new MeshInstance3D { Name = name, Mesh = ZombieHumanGeometry.Shape(name, mesh ?? Box), Position = pos,
            Scale = scale, MaterialOverride = material, RotationDegrees = rotation ?? Vector3.Zero });
    }
}
