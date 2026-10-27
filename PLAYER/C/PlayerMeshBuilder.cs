// Lightweight procedural soldier: rounded human proportions, tactical details and deterministic fabric/camo.
// Art-only replacement for the original box character; all existing rig pivots and weapon sockets stay unchanged.
using Godot;

public sealed class PlayerMeshBuilder
{
    private readonly Node3D _owner;
    private readonly PlayerSettings _settings;

    // =========================================================
    // Shared geometry/materials avoid duplicate GPU resources if another player preview is instantiated.
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1f, RadialSegments = 12, Rings = 6 };
    private static readonly CylinderMesh TaperedTorso = new() { TopRadius = 0.5f, BottomRadius = 0.39f, Height = 1f, RadialSegments = 12, Rings = 1 };
    private static readonly Material Jacket = Camouflage(
        new Color(0.27f, 0.32f, 0.25f), new Color(0.35f, 0.39f, 0.28f),
        new Color(0.18f, 0.25f, 0.20f), new Color(0.38f, 0.34f, 0.25f), 47);
    private static readonly Material Trousers = Camouflage(
        new Color(0.32f, 0.33f, 0.26f), new Color(0.42f, 0.40f, 0.30f),
        new Color(0.22f, 0.28f, 0.22f), new Color(0.29f, 0.26f, 0.22f), 131);
    private static readonly Material Skin = Solid(new Color(0.67f, 0.48f, 0.34f));
    private static readonly Material Helmet = Solid(new Color(0.24f, 0.29f, 0.22f));
    private static readonly Material Vest = Solid(new Color(0.19f, 0.25f, 0.20f));
    private static readonly Material Webbing = Solid(new Color(0.43f, 0.40f, 0.29f));
    private static readonly Material Boots = Solid(new Color(0.13f, 0.14f, 0.13f));
    private static readonly Material Gloves = Solid(new Color(0.22f, 0.23f, 0.19f));
    private static readonly Material FaceDetail = Solid(new Color(0.16f, 0.13f, 0.11f));

    private Node3D _art = null!, _upperBody = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Node3D _leftKnee = null!, _rightKnee = null!, _leftFoot = null!, _rightFoot = null!;
    public Node3D Art => _art;
    public Node3D UpperBody => _upperBody;
    public Node3D LeftArm => _leftArm;
    public Node3D RightArm => _rightArm;
    public Node3D LeftLeg => _leftLeg;
    public Node3D RightLeg => _rightLeg;
    public Node3D LeftKnee => _leftKnee;
    public Node3D RightKnee => _rightKnee;
    public Node3D LeftFoot => _leftFoot;
    public Node3D RightFoot => _rightFoot;
    public Marker3D WeaponMount { get; private set; } = null!;

    // =========================================================
    public PlayerMeshBuilder(Node3D owner, PlayerSettings settings)
    { _owner = owner; _settings = settings; BuildHuman(); }

    // =========================================================
    // Same 1.8m animation skeleton as before: only visible geometry, materials and attachments change.
    private void BuildHuman()
    {
        _art = new Node3D { Name = "VisualRoot" }; _owner.AddChild(_art);
        _upperBody = new Node3D { Name = "UpperBody", Position = Vector3.Up * 0.85f }; _art.AddChild(_upperBody);
        WeaponMount = new Marker3D { Name = "WeaponMount", Position = _settings.WeaponMountOffset }; _upperBody.AddChild(WeaponMount);

        // Shoulder-width jacket and tapered waist, rather than a rectangular torso.
        Part(_upperBody, "TaperedJacket", new(0, 0.29f, 0), new(0.54f, 0.55f, 0.34f), Jacket, TaperedTorso);
        Part(_upperBody, "RoundedChest", new(0, 0.39f, -0.006f), new(0.53f, 0.31f, 0.32f), Jacket, Rounded);
        Part(_upperBody, "Waist", new(0, 0.06f, 0), new(0.37f, 0.18f, 0.29f), Trousers, Rounded);
        Part(_upperBody, "Collar", new(0, 0.60f, 0), new(0.26f, 0.15f, 0.27f), Vest, Rounded);

        // Simplified face is deliberately visible from the angled view, under a low-profile helmet.
        Part(_upperBody, "Head", new(0, 0.76f, 0), new(0.29f, 0.33f, 0.28f), Skin, Rounded);
        Part(_upperBody, "Nose", new(0, 0.735f, -0.148f), new(0.085f, 0.10f, 0.10f), Skin, Rounded);
        Part(_upperBody, "LeftEar", new(-0.15f, 0.745f, 0), new(0.055f, 0.09f, 0.08f), Skin, Rounded);
        Part(_upperBody, "RightEar", new(0.15f, 0.745f, 0), new(0.055f, 0.09f, 0.08f), Skin, Rounded);
        Part(_upperBody, "LeftEye", new(-0.084f, 0.782f, -0.137f), new(0.027f, 0.022f, 0.013f), FaceDetail, Rounded);
        Part(_upperBody, "RightEye", new(0.084f, 0.782f, -0.137f), new(0.027f, 0.022f, 0.013f), FaceDetail, Rounded);
        Part(_upperBody, "HelmetShell", new(0, 0.866f, 0.008f), new(0.355f, 0.145f, 0.335f), Helmet, Rounded);
        Part(_upperBody, "HelmetRim", new(0, 0.832f, -0.003f), new(0.343f, 0.045f, 0.321f), Vest, Rounded);
        Part(_upperBody, "HelmetBrim", new(0, 0.828f, -0.189f), new(0.29f, 0.032f, 0.16f), Helmet, Box);

        // Chest webbing, pack and small muted-colour pouches are legible at top-down zoom.
        Part(_upperBody, "ProtectiveVest", new(0, 0.325f, -0.154f), new(0.435f, 0.385f, 0.125f), Vest, Rounded);
        Part(_upperBody, "LeftStrap", new(-0.165f, 0.39f, -0.213f), new(0.056f, 0.36f, 0.039f), Webbing, Box, new(0, 0, -14));
        Part(_upperBody, "RightStrap", new(0.165f, 0.39f, -0.213f), new(0.056f, 0.36f, 0.039f), Webbing, Box, new(0, 0, 14));
        Part(_upperBody, "Belt", new(0, 0.085f, -0.005f), new(0.42f, 0.078f, 0.33f), Boots, Box);
        Part(_upperBody, "BeltBuckle", new(0, 0.086f, -0.184f), new(0.091f, 0.065f, 0.026f), Webbing, Box);
        Part(_upperBody, "LeftChestPouch", new(-0.125f, 0.21f, -0.251f), new(0.15f, 0.13f, 0.075f), Webbing, Box);
        Part(_upperBody, "RightChestPouch", new(0.125f, 0.21f, -0.251f), new(0.15f, 0.13f, 0.075f), Webbing, Box);
        Part(_upperBody, "UtilityPack", new(0, 0.34f, 0.224f), new(0.35f, 0.37f, 0.20f), Vest, Rounded);
        Part(_upperBody, "PackPocket", new(0, 0.21f, 0.343f), new(0.23f, 0.16f, 0.063f), Webbing, Rounded);
        Part(_upperBody, "LeftHipPouch", new(-0.235f, 0.06f, 0.025f), new(0.115f, 0.16f, 0.16f), Webbing, Rounded);
        Part(_upperBody, "RightHipPouch", new(0.235f, 0.06f, 0.025f), new(0.115f, 0.16f, 0.16f), Webbing, Rounded);

        // Shoulders retain the same effective positions used by walk, aim and crouch poses.
        _leftArm = Limb(_upperBody, "LeftArm", new(-0.34f, 0.50f, 0));
        _rightArm = Limb(_upperBody, "RightArm", new(0.34f, 0.50f, 0));
        BuildArm(_leftArm); BuildArm(_rightArm);

        _leftLeg = Limb(_art, "LeftLeg", new(-0.14f, 0.85f, 0));
        _rightLeg = Limb(_art, "RightLeg", new(0.14f, 0.85f, 0));
        _leftKnee = BuildLeg(_leftLeg, out _leftFoot);
        _rightKnee = BuildLeg(_rightLeg, out _rightFoot);
    }

    // =========================================================
    // Overlapping rounded shapes suggest upper arm, forearm and gloved hand without an elbow driver.
    private static void BuildArm(Node3D arm)
    {
        Part(arm, "Shoulder", new(0, -0.07f, 0), new(0.235f, 0.22f, 0.24f), Jacket, Rounded);
        Part(arm, "Sleeve", new(0, -0.215f, 0), new(0.19f, 0.32f, 0.205f), Jacket, Rounded);
        Part(arm, "ElbowPatch", new(0, -0.322f, -0.075f), new(0.13f, 0.115f, 0.055f), Vest, Rounded);
        Part(arm, "Forearm", new(0, -0.385f, 0), new(0.158f, 0.22f, 0.176f), Jacket, Rounded);
        Part(arm, "Glove", new(0, -0.485f, -0.018f), new(0.16f, 0.155f, 0.172f), Gloves, Rounded);
    }

    // =========================================================
    // Existing knee and ankle pivot positions are unchanged; shin and boots now have rounded contours.
    private static Node3D BuildLeg(Node3D leg, out Node3D foot)
    {
        Part(leg, "Thigh", new(0, -0.17f, 0), new(0.235f, 0.39f, 0.268f), Trousers, Rounded);
        Part(leg, "CargoPocket", new(0.14f * (leg.Name == "LeftLeg" ? -1 : 1), -0.21f, -0.006f),
            new(0.075f, 0.18f, 0.17f), Webbing, Rounded);
        var knee = Limb(leg, "Knee", new(0, -0.36f, 0));
        // Rounded joint masks the seam between thigh and shin during knee flexion.
        Part(knee, "KneeJoint", Vector3.Zero, new(0.19f, 0.18f, 0.22f), Trousers, Rounded);
        Part(knee, "Shin", new(0, -0.18f, 0), new(0.20f, 0.37f, 0.226f), Trousers, Rounded);
        Part(knee, "KneePad", new(0, -0.06f, -0.13f), new(0.18f, 0.17f, 0.065f), Vest, Rounded);
        foot = Limb(knee, "Ankle", new(0, -0.36f, 0));
        Part(foot, "BootUpper", new(0, 0.055f, 0), new(0.205f, 0.24f, 0.235f), Boots, Rounded);
        Part(foot, "BootToe", new(0, -0.001f, -0.095f), new(0.234f, 0.15f, 0.34f), Boots, Rounded);
        Part(foot, "BootSole", new(0, -0.069f, -0.085f), new(0.243f, 0.038f, 0.34f), Gloves, Box);
        return knee;
    }

    // =========================================================
    private static Node3D Limb(Node3D parent, string name, Vector3 position)
    { var pivot = new Node3D { Name = name, Position = position }; parent.AddChild(pivot); return pivot; }

    // =========================================================
    // One static material per style; no individual procedural textures/meshes are generated each frame.
    private static StandardMaterial3D Solid(Color color)
    { return new StandardMaterial3D { AlbedoColor = color, Roughness = 0.95f }; }

    // =========================================================
    // A small deterministic 2D colour pattern adds broken-up soldier fabric without external assets/shaders.
    private static StandardMaterial3D Camouflage(Color a, Color b, Color c, Color d, int seed)
    {
        const int size = 96;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Color[] swatches = { a, b, c, d };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float value = Noise(x / 15f, y / 15f, seed) * 0.70f + Noise(x / 6.5f, y / 6.5f, seed + 17) * 0.30f;
            Color colour = swatches[value < 0.32f ? 0 : value < 0.48f ? 1 : value < 0.65f ? 2 : 3];
            float grain = (Hash(x, y, seed + 83) - 0.5f) * 0.055f;
            image.SetPixel(x, y, new Color(
                Mathf.Clamp(colour.R + grain, 0, 1), Mathf.Clamp(colour.G + grain, 0, 1),
                Mathf.Clamp(colour.B + grain, 0, 1), 1));
        }
        image.GenerateMipmaps();
        return new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(image), Roughness = 0.96f };
    }

    // =========================================================
    // Bilinearly interpolated value noise creates irregular patches; hashing is allocation-free.
    private static float Noise(float x, float y, int seed)
    {
        int ix = (int)Mathf.Floor(x), iy = (int)Mathf.Floor(y);
        float dx = x - ix, dy = y - iy;
        dx = dx * dx * (3 - 2 * dx); dy = dy * dy * (3 - 2 * dy);
        return Mathf.Lerp(
            Mathf.Lerp(Hash(ix, iy, seed), Hash(ix + 1, iy, seed), dx),
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
    // Art meshes have no collision, AI or update loops. The original player capsule remains authoritative.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size, Material material, Mesh? shape = null, Vector3? rotation = null)
    {
        parent.AddChild(new MeshInstance3D {
            Name = name, Mesh = shape ?? Box, Position = position, Scale = size,
            RotationDegrees = rotation ?? Vector3.Zero, MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        });
    }
}
