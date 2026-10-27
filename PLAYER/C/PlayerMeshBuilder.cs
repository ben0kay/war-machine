// Procedural placeholder player mesh and cached rig; replace with a model from PLAYER/Artwork later.
using Godot;

public sealed class PlayerMeshBuilder
{
    private readonly Node3D _owner;
    private readonly PlayerSettings _settings;
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
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
    public PlayerMeshBuilder(Node3D owner, PlayerSettings settings) { _owner = owner; _settings = settings; BuildHuman(); }

    // =========================================================
    // Shared box geometry and four simple materials keep the visible test human cheap and replaceable.
    private void BuildHuman()
    {
        _art = new Node3D { Name = "VisualRoot" }; _owner.AddChild(_art);
        var shirt = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.55f, 0.62f), Roughness = 0.9f };
        var skin = new StandardMaterial3D { AlbedoColor = new Color(0.76f, 0.58f, 0.4f), Roughness = 1 };
        var trousers = new StandardMaterial3D { AlbedoColor = new Color(0.34f, 0.3f, 0.23f), Roughness = 1 };
        var boots = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.12f, 0.14f), Roughness = 1 };
        _upperBody = new Node3D { Name = "UpperBody", Position = Vector3.Up * 0.85f }; _art.AddChild(_upperBody);
        WeaponMount = new Marker3D { Name = "WeaponMount", Position = _settings.WeaponMountOffset }; _upperBody.AddChild(WeaponMount);
        Part(_upperBody, "Torso", new(0, 0.27f, 0), new(0.5f, 0.6f, 0.3f), shirt);
        Part(_upperBody, "Head", new(0, 0.77f, 0), new(0.32f, 0.36f, 0.32f), skin);
        Part(_upperBody, "Face", new(0, 0.77f, -0.17f), new(0.23f, 0.12f, 0.04f), skin);
        _leftArm = Limb("LeftArm", new(-0.34f, 1.35f, 0)); _rightArm = Limb("RightArm", new(0.34f, 1.35f, 0));
        _leftArm.Reparent(_upperBody); _rightArm.Reparent(_upperBody);
        foreach (var arm in new[] { _leftArm, _rightArm })
        {
            Part(arm, "Sleeve", new(0, -0.18f, 0), new(0.18f, 0.36f, 0.22f), shirt);
            Part(arm, "Hand", new(0, -0.43f, 0), new(0.15f, 0.2f, 0.17f), skin);
        }
        _leftLeg = Limb("LeftLeg", new(-0.14f, 0.85f, 0)); _rightLeg = Limb("RightLeg", new(0.14f, 0.85f, 0));
        _leftKnee = BuildLeg(_leftLeg, trousers, boots, out _leftFoot); _rightKnee = BuildLeg(_rightLeg, trousers, boots, out _rightFoot);
    }

    // =========================================================
    // Knee pivots let crouching fold the legs rather than shrink the entire character.
    private Node3D BuildLeg(Node3D leg, Material trousers, Material boots, out Node3D foot)
    {
        Part(leg, "Thigh", new(0, -0.18f, 0), new(0.21f, 0.36f, 0.25f), trousers);
        var knee = new Node3D { Name = "Knee", Position = new Vector3(0, -0.36f, 0) }; leg.AddChild(knee);
        Part(knee, "Shin", new(0, -0.18f, 0), new(0.2f, 0.36f, 0.24f), trousers);
        foot = new Node3D { Name = "Ankle", Position = new Vector3(0, -0.36f, 0) }; knee.AddChild(foot);
        Part(foot, "Boot", new(0, 0.01f, -0.06f), new(0.24f, 0.25f, 0.4f), boots);
        return knee;
    }

    // =========================================================
    // Pivot placement is cached at creation, never searched in a frame callback.
    private Node3D Limb(string name, Vector3 position)
    { var pivot = new Node3D { Name = name, Position = position }; _art.AddChild(pivot); return pivot; }

    // =========================================================
    // Art meshes contain no collision or update loops; the root capsule handles all physical movement.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size, Material material)
    { parent.AddChild(new MeshInstance3D { Name = name, Mesh = Box, Position = position, Scale = size, MaterialOverride = material }); }
}
