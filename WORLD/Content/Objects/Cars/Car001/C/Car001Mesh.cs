// Car001: one-shot low-poly abandoned compact hatchback mesh, with driver's door swung open.
// No vehicle physics/AI; C/ owns temporary Godot C# geometry while Artwork/ holds later GLB assets.
using Godot;

[Tool]
public partial class Car001Mesh : Node3D
{
    [Export] public Car001Settings Settings { get; set; } = null!;
    private Node3D? _door;

    // =========================================================
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // An editor/runtime pose control: rotate existing door and its rigid static collision together.
    // The first prefab is deliberately already open, not an interactive animated door.
    public void SetDoorAngle(float degrees)
    {
        if (Settings != null) Settings.DoorAngle = Mathf.Clamp(degrees, 0, 82);
        if (GodotObject.IsInstanceValid(_door)) _door!.RotationDegrees = new Vector3(0, -Mathf.Clamp(degrees, 0, 82), 0);
    }

    // =========================================================
    private void Build()
    {
        var s = Settings ?? new Car001Settings();
        float length = Mathf.Clamp(s.Length, 3.6f, 5.5f), width = Mathf.Clamp(s.Width, 1.55f, 2.3f);
        float halfL = length * 0.5f, halfW = width * 0.5f;
        var generated = new Node3D { Name = "Generated" }; AddChild(generated);
        var shell = Group(generated, "ChassisAndBody");
        var cabin = Group(generated, "Cabin");
        var detail = Group(generated, "ExteriorDetails");
        _door = Group(generated, "DriverDoorOpen");
        // All collision is static; no RigidBody3D, CharacterBody3D or wheels simulation.
        var body = new StaticBody3D { Name = "StaticCollision", CollisionLayer = 1, CollisionMask = 1 };
        generated.AddChild(body);
        var paint = Mat(s.Paint), paintLight = Mat(s.Paint.Lightened(0.13f));
        var paintDark = Mat(s.Paint.Darkened(0.22f)), rubber = Mat(new Color(0.085f, 0.092f, 0.10f));
        var glass = Mat(s.Glass), black = Mat(new Color(0.13f, 0.15f, 0.16f));
        var seats = Mat(new Color(0.33f, 0.34f, 0.32f)), metal = Mat(new Color(0.56f, 0.58f, 0.56f));
        var lights = Mat(new Color(0.83f, 0.81f, 0.65f)), red = Mat(new Color(0.63f, 0.15f, 0.12f));

        // Local forward = -Z; driver is on the left (-X). Base at Y=0.
        Box(shell, body, "LowerMonocoque", new(0, 0.72f, 0), new(width, 0.64f, length - 0.14f), paint);
        Box(shell, body, "Underbody", new(0, 0.37f, 0), new(width - 0.12f, 0.19f, length - 0.36f), black);
        Box(shell, null, "FrontHood", new(0, 1.06f, -halfL + 0.64f), new(width - 0.09f, 0.16f, 1.22f), paintLight);
        Box(shell, null, "RearDeck", new(0, 1.11f, halfL - 0.47f), new(width - 0.08f, 0.20f, 0.84f), paintDark);
        Box(shell, null, "FrontBumper", new(0, 0.56f, -halfL - 0.06f), new(width + 0.13f, 0.24f, 0.18f), black);
        Box(shell, null, "RearBumper", new(0, 0.56f, halfL + 0.06f), new(width + 0.12f, 0.25f, 0.17f), black);

        // Black interior cavity remains visible through the genuinely open driver-side door gap.
        Box(cabin, null, "InteriorFloor", new(0, 0.75f, 0.05f), new(width - 0.29f, 0.07f, 2.12f), black);
        Box(cabin, null, "Dashboard", new(0, 1.06f, -0.79f), new(width - 0.27f, 0.26f, 0.37f), black);
        for (int i = 0; i < 2; i++)
        {
            float x = (i == 0 ? -0.35f : 0.35f) * width / 1.85f;
            Box(cabin, null, "SeatCushion", new(x, 0.92f, -0.12f), new(0.55f, 0.13f, 0.70f), seats);
            Box(cabin, null, "SeatBack", new(x, 1.25f, 0.23f), new(0.57f, 0.73f, 0.15f), seats);
            Box(cabin, null, "HeadRest", new(x, 1.72f, 0.24f), new(0.35f, 0.19f, 0.16f), black);
        }
        Box(cabin, null, "RearSeat", new(0, 1.01f, 0.89f), new(width - 0.37f, 0.27f, 0.53f), seats);

        // Short roof, front windscreen, rear glass, and three upright pillars.
        float roofY = Mathf.Max(1.65f, s.RoofHeight);
        Box(cabin, body, "Roof", new(0, roofY, 0.16f), new(width - 0.30f, 0.15f, 1.89f), paint);
        Box(cabin, null, "Windscreen", new(0, (roofY + 1.15f) * 0.5f, -0.89f),
            new(width - 0.39f, roofY - 1.10f, 0.07f), glass, 0.39f);
        Box(cabin, null, "RearGlass", new(0, (roofY + 1.16f) * 0.5f, 1.19f),
            new(width - 0.40f, roofY - 1.11f, 0.07f), glass, -0.23f);
        foreach (float x in new[] { -halfW + 0.10f, halfW - 0.10f })
        {
            Box(cabin, null, "FrontPillar", new(x, 1.49f, -0.80f),
                new(0.12f, roofY - 1.02f, 0.12f), paintDark, 0.36f);
            Box(cabin, null, "RearPillar", new(x, 1.49f, 1.03f),
                new(0.15f, roofY - 1.03f, 0.15f), paintDark);
            Box(cabin, null, "RoofRail", new(x, roofY + 0.08f, 0.14f),
                new(0.07f, 0.06f, 1.72f), paintLight);
        }
        // Passenger side is closed. Driver side front door is NOT covered by another solid side panel.
        Box(cabin, body, "PassengerDoorClosed", new(halfW + 0.01f, 1.17f, -0.20f),
            new(0.13f, 0.76f, 1.19f), paintDark);
        Box(cabin, null, "PassengerWindow", new(halfW + 0.083f, 1.57f, -0.18f),
            new(0.04f, roofY - 1.30f, 1.04f), glass);
        Box(cabin, body, "RearRightQuarter", new(halfW, 1.15f, 0.89f),
            new(0.17f, 0.76f, 0.92f), paint);
        Box(cabin, body, "RearLeftQuarter", new(-halfW, 1.15f, 0.89f),
            new(0.17f, 0.76f, 0.92f), paint);
        Box(cabin, null, "RearQuarterGlassL", new(-halfW - 0.10f, 1.59f, 0.90f),
            new(0.06f, roofY - 1.30f, 0.75f), glass);
        Box(cabin, null, "RearQuarterGlassR", new(halfW + 0.10f, 1.59f, 0.90f),
            new(0.06f, roofY - 1.30f, 0.75f), glass);
        Box(cabin, null, "DoorSillOpenLeft", new(-halfW, 0.83f, -0.20f),
            new(0.13f, 0.19f, 1.27f), black);

        // The visible door is a separate hinge pivot on the DRIVER FRONT edge, already swung out ~58 degrees.
        // Door panel starts behind the hinge; the hinge rotation (negative yaw) swings it toward -X.
        _door.Position = new Vector3(-halfW - 0.05f, 0, -0.83f);
        _door.RotationDegrees = new Vector3(0, -Mathf.Clamp(s.DoorAngle, 0, 82), 0);
        var doorBody = new StaticBody3D { Name = "OpenDoorCollision", CollisionLayer = 1, CollisionMask = 1 };
        _door.AddChild(doorBody);
        Box(_door, doorBody, "OuterDoorPanel", new(0, 1.21f, 0.60f),
            new(0.12f, 0.79f, 1.16f), paint);
        Box(_door, null, "InnerDoorPanel", new(0.075f, 1.19f, 0.60f),
            new(0.03f, 0.54f, 1.06f), black);
        Box(_door, null, "DoorUpperWindow", new(0.012f, 1.68f, 0.60f),
            new(0.065f, roofY - 1.34f, 1.02f), glass);
        Box(_door, null, "UpperWindowFrame", new(0, roofY - 0.10f, 0.60f),
            new(0.10f, 0.11f, 1.14f), paintDark);
        Box(_door, null, "OuterDoorHandle", new(-0.105f, 1.35f, 0.96f),
            new(0.08f, 0.06f, 0.18f), metal);
        Box(_door, null, "InnerDoorPull", new(0.11f, 1.25f, 0.75f),
            new(0.08f, 0.08f, 0.28f), rubber);

        // Low-cost tyres (each is only one cylinder with end caps); do not physically simulate wheels.
        foreach (float x in new[] { -halfW + 0.02f, halfW - 0.02f })
            foreach (float z in new[] { -halfL + 0.86f, halfL - 0.75f })
            {
                var wheel = new MeshInstance3D { Name = "Wheel", Mesh = new CylinderMesh {
                    TopRadius = 0.39f, BottomRadius = 0.39f, Height = 0.24f, RadialSegments = 12 },
                    MaterialOverride = rubber, Position = new(x, 0.41f, z),
                    Rotation = new Vector3(0, 0, Mathf.Pi * 0.5f) };
                detail.AddChild(wheel);
                var hub = new MeshInstance3D { Name = "Hub", Mesh = new CylinderMesh {
                    TopRadius = 0.20f, BottomRadius = 0.20f, Height = 0.255f, RadialSegments = 8 },
                    MaterialOverride = metal, Position = new(x + (x < 0 ? -0.012f : 0.012f), 0.41f, z),
                    Rotation = new Vector3(0, 0, Mathf.Pi * 0.5f) };
                detail.AddChild(hub);
            }

        foreach (float x in new[] { -halfW + 0.33f, halfW - 0.33f })
        {
            Box(detail, null, "Headlight", new(x, 0.91f, -halfL - 0.045f),
                new(0.49f, 0.26f, 0.08f), lights);
            Box(detail, null, "Taillight", new(x, 0.90f, halfL + 0.048f),
                new(0.45f, 0.29f, 0.08f), red);
        }
        Box(detail, null, "FrontGrille", new(0, 0.76f, -halfL - 0.08f),
            new(width * 0.45f, 0.22f, 0.07f), black);
        Box(detail, null, "RearPlate", new(0, 0.64f, halfL + 0.10f),
            new(0.58f, 0.18f, 0.03f), metal);
        Box(detail, null, "SideMirror", new(halfW + 0.16f, 1.41f, -0.76f),
            new(0.25f, 0.17f, 0.26f), paintDark);
        Box(detail, null, "DriverMirror", new(-halfW - 0.16f, 1.41f, -0.76f),
            new(0.25f, 0.17f, 0.26f), paintDark);
    }

    // =========================================================
    private static readonly BoxMesh UnitBox = new() { Size = Vector3.One };
    private static StandardMaterial3D Mat(Color color) => new() { AlbedoColor = color, Roughness = 0.83f };

    // =========================================================
    private static Node3D Group(Node3D parent, string name)
    { var n = new Node3D { Name = name }; parent.AddChild(n); return n; }

    // =========================================================
    private static void Box(Node3D root, StaticBody3D? collider, string name,
        Vector3 pos, Vector3 size, Material material, float pitch = 0f)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0) return;
        root.AddChild(new MeshInstance3D { Name = name, Mesh = UnitBox,
            Position = pos, Scale = size, Rotation = new Vector3(pitch, 0, 0),
            MaterialOverride = material });
        if (collider != null)
            collider.AddChild(new CollisionShape3D { Name = name + "Shape", Position = pos,
                Rotation = new Vector3(pitch, 0, 0), Shape = new BoxShape3D { Size = size } });
    }
}
