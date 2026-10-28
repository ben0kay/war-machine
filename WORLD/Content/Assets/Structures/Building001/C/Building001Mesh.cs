// Building001: one-shot, editor-previewable Godot C# geometry for an abandoned maintenance depot.
// C/ owns procedural art and static collision. Artwork/ is reserved for an eventual imported model.
using Godot;

[Tool]
public partial class Building001Mesh : Node3D
{
    [Export] public Building001Settings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;

    // =========================================================
    // One build per scene instance; no frame, physics, editor-poll or noise callbacks.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build(); DeclareVisibility();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // A roof toggle allows later player-indoors camera logic without rebuilding or removing roof physics.
    public void SetRoofVisible(bool visible)
    {
        ShowRoof = visible;
        if (GodotObject.IsInstanceValid(_roof)) _roof!.Visible = visible;
    }

    // =========================================================
    // All parts are children of a replaceable visual set; collision remains a single static body.
    private void Build()
    {
        var s = Settings ?? new Building001Settings();
        float w = Mathf.Clamp(s.Width, 16f, 32f), d = Mathf.Clamp(s.Depth, 10f, 24f);
        float h = Mathf.Clamp(s.WallHeight, 4f, 8f), rise = Mathf.Clamp(s.RoofRise, 1f, 5f);
        float hx = w * 0.5f, hz = d * 0.5f, wall = 0.25f;
        var generated = new Node3D { Name = "Generated" }; AddChild(generated);
        var shell = Group(generated, "WallsAndTrim");
        var details = Group(generated, "Details");
        _roof = Group(generated, "RoofVisuals"); _roof.Visible = ShowRoof;
        var body = new StaticBody3D { Name = "Collision", CollisionLayer = 1, CollisionMask = 1 };
        generated.AddChild(body);
        var siding = MakeMaterial(s.Siding);
        var sidingLight = MakeMaterial(s.Siding.Lightened(0.12f));
        var sidingDark = MakeMaterial(s.Siding.Darkened(0.19f));
        var concrete = MakeMaterial(s.Concrete);
        var concreteDark = MakeMaterial(s.Concrete.Darkened(0.22f));
        var trim = MakeMaterial(s.Trim, 0.32f);
        var metal = MakeMaterial(s.Roof, 0.22f);
        var roofRib = MakeMaterial(s.Roof.Lightened(0.18f), 0.36f);
        var glass = MakeMaterial(s.Window, 0.23f);
        var accent = MakeMaterial(s.Accent);
        var dark = MakeMaterial(new Color(0.105f, 0.13f, 0.145f));
        var grime = MakeMaterial(s.Roof.Darkened(0.2f));

        // Building is designed on Y=0 so WORLD can position the entire prefab on sampled terrain.
        // The decorative slab is non-solid: Terrain3D remains responsible for walking ground.
        Box(details, null, "InteriorFloor", new(0, 0.055f, 0), new(w - 0.22f, 0.10f, d - 0.22f), concreteDark);
        Box(details, null, "LoadingApron", new(0.5f, 0.055f, hz + 1.6f), new(w + 1.4f, 0.10f, 3.4f), concrete);
        // Keep the walk-in opening clear at ground level; only solid wall bays get a footing.
        Box(shell, body, "ConcreteFrontLeft", new(-hx + 1.4f, 0.32f, hz), new(2.8f, 0.64f, wall), concrete);
        Box(shell, body, "ConcreteFrontCenter", new((-hx + 4.8f) * 0.5f, 0.32f, hz),
            new(hx - 4.8f, 0.64f, wall), concrete);
        Box(shell, body, "ConcreteFrontRight", new((hx + 6.0f) * 0.5f, 0.32f, hz),
            new(hx - 6f, 0.64f, wall), concrete);
        Box(shell, body, "ConcreteBaseRear", new(0, 0.32f, -hz), new(w, 0.64f, wall), concrete);
        Box(shell, body, "ConcreteBaseLeft", new(-hx, 0.32f, 0), new(wall, 0.64f, d), concrete);
        Box(shell, body, "ConcreteBaseRight", new(hx, 0.32f, 0), new(wall, 0.64f, d), concrete);

        // Front (+Z): a genuinely open two-metre pedestrian doorway on the left.
        // The six-metre rolling garage shutter on the right is closed/collidable.
        float doorL = -hx + 2.8f, doorR = doorL + 2.0f;
        float gateL = 0.0f, gateR = 6.0f;
        FrontWall(shell, body, "FrontLeft", -hx, doorL, hz, h, siding);
        FrontWall(shell, body, "FrontBetween", doorR, gateL, hz, h, siding);
        FrontWall(shell, body, "FrontRight", gateR, hx, hz, h, siding);
        Header(shell, body, "DoorLintel", doorL, doorR, hz, 3.05f, h, siding);
        Header(shell, body, "GarageLintel", gateL, gateR, hz, 4.05f, h, siding);
        Box(shell, body, "Shutter", new(3.0f, 2.02f, hz + 0.022f), new(6.0f, 4.04f, 0.15f), sidingDark);
        for (int row = 0; row < 13; row++)
            Box(details, null, "ShutterRib", new(3, 0.22f + row * 0.285f, hz + 0.12f), new(5.85f, 0.055f, 0.065f), trim);
        for (int row = 0; row < 4; row++)
            Box(details, null, "ShutterWindows", new(0.93f + row * 1.36f, 3.33f, hz + 0.13f), new(0.98f, 0.30f, 0.045f), glass);
        Box(details, null, "DoorLeftJamb", new(doorL, 1.53f, hz + 0.14f), new(0.17f, 3.06f, 0.28f), trim);
        Box(details, null, "DoorRightJamb", new(doorR, 1.53f, hz + 0.14f), new(0.17f, 3.06f, 0.28f), trim);
        Box(details, null, "DoorHead", new((doorL + doorR) * 0.5f, 3.05f, hz + 0.14f), new(2.20f, 0.20f, 0.28f), trim);
        Box(details, null, "DoorThreshold", new((doorL + doorR) * 0.5f, 0.08f, hz + 0.14f), new(2.15f, 0.09f, 0.56f), accent);
        Box(details, null, "OpenDoorLeaf", new(doorL + 0.07f, 1.48f, hz - 0.94f), new(0.11f, 2.88f, 1.8f), sidingDark);
        Box(details, null, "DoorWindow", new(doorL + 0.145f, 2.24f, hz - 1.07f), new(0.035f, 0.47f, 0.50f), glass);
        Box(details, null, "GarageRailL", new(gateL, 2.0f, hz + 0.20f), new(0.17f, 4.15f, 0.30f), trim);
        Box(details, null, "GarageRailR", new(gateR, 2.0f, hz + 0.20f), new(0.17f, 4.15f, 0.30f), trim);
        Box(details, null, "GarageLintelTrim", new(3, 4.09f, hz + 0.20f), new(6.25f, 0.18f, 0.32f), trim);

        // Side window openings are actually cut out of the walls, not simply painted on.
        SideWall(shell, details, body, -hx, -1, hz, h, wall, siding, sidingLight, trim, glass);
        SideWall(shell, details, body,  hx,  1, hz, h, wall, siding, sidingLight, trim, glass);
        Box(shell, body, "RearWall", new(0, h * 0.5f, -hz), new(w, h, wall), siding);
        for (int i = 0; i < 3; i++)
        {
            float wx = -hx * 0.65f + i * hx * 0.65f;
            Box(details, null, "RearWindowFrame", new(wx, 2.9f, -hz - 0.15f), new(2.85f, 1.7f, 0.12f), trim);
            Box(details, null, "RearWindowGlass", new(wx, 2.9f, -hz - 0.23f), new(2.59f, 1.45f, 0.12f), glass);
            Box(details, null, "RearWindowCross", new(wx, 2.9f, -hz - 0.31f), new(0.10f, 1.56f, 0.11f), sidingLight);
        }

        // Wall posts, horizontal belt trim and weathered siding seams give a readable scale.
        foreach (float x in new[] { -hx, hx })
            foreach (float z in new[] { -hz, hz })
                Box(details, null, "CornerPost", new(x, h * 0.5f, z), new(0.39f, h, 0.39f), trim);
        foreach (float z in new[] { -hz - 0.15f, hz + 0.15f })
        {
            Box(details, null, "EaveBelt", new(0, h - 0.20f, z), new(w, 0.26f, 0.22f), trim);
            for (int i = 1; i < 15; i++)
            {
                float x = -hx + i * w / 15f;
                if (z > 0 && x > doorL && x < doorR) continue;
                if (z > 0 && x > gateL && x < gateR) continue;
                Box(details, null, "SidingVertical", new(x, 2.7f, z + Mathf.Sign(z) * 0.035f),
                    new(0.045f, 4.55f, 0.055f), sidingLight);
            }
        }

        // A complete pitched roof with sloped static collision, gable infill and standing seams.
        float eave = 0.70f, halfSlope = hz + eave;
        float length = Mathf.Sqrt(halfSlope * halfSlope + rise * rise);
        float angle = Mathf.Atan2(rise, halfSlope);
        for (int side = -1; side <= 1; side += 2)
        {
            float xAngle = side * angle;
            Vector3 center = new(0, h + rise * 0.5f, side * halfSlope * 0.5f);
            Box(_roof, body, "SlopedRoof", center, new(w + 1.2f, 0.22f, length + 0.17f), metal, xAngle);
            Vector3 normal = new(0, Mathf.Cos(xAngle), Mathf.Sin(xAngle));
            for (int stripe = 0; stripe <= 11; stripe++)
            {
                float x = -(w + 0.8f) * 0.5f + stripe * (w + 0.8f) / 11f;
                Box(_roof, null, "RoofStandingSeam", center + normal * 0.145f + Vector3.Right * x,
                    new(0.07f, 0.065f, length + 0.09f), roofRib, xAngle);
            }
            Box(_roof, null, "Gutter", new(0, h - 0.08f, side * (halfSlope + 0.14f)),
                new(w + 1.28f, 0.19f, 0.22f), trim);
        }
        Box(_roof, null, "RidgeCap", new(0, h + rise + 0.12f, 0), new(w + 1.30f, 0.24f, 0.38f), roofRib);
        Gable(shell, "LeftGable", -hx, hz, h, rise, siding);
        Gable(shell, "RightGable", hx, hz, h, rise, siding);

        // Roof fixtures and low-key military/industrial details: no neon or expensive effects.
        RoofFixture(_roof, -w * 0.26f, -2.0f, h, rise, halfSlope, concreteDark, trim);
        RoofFixture(_roof, w * 0.28f, 1.15f, h, rise, halfSlope, concreteDark, trim);
        Box(_roof, null, "AntennaMount", new(w * 0.35f, h + rise + 0.23f, 0), new(0.66f, 0.44f, 0.66f), trim);
        Cylinder(_roof, "AntennaPole", new(w * 0.35f, h + rise + 1.36f, 0), 0.07f, 2.1f, roofRib);
        Cylinder(_roof, "AntennaCap", new(w * 0.35f, h + rise + 2.5f, 0), 0.28f, 0.10f, accent);

        // Entry canopy, warning panel and small grounded exterior props give human-scale detail.
        Box(details, null, "EntryAwning", new((doorL + doorR) * 0.5f, 3.42f, hz + 0.79f),
            new(2.98f, 0.17f, 1.72f), metal);
        for (float x = doorL - 0.25f; x <= doorR + 0.30f; x += doorR - doorL + 0.5f)
            Box(details, null, "AwningBracket", new(x, 3.1f, hz + 0.45f), new(0.10f, 0.52f, 0.13f), trim);
        Box(details, null, "DepotSign", new(3.0f, 4.68f, hz + 0.20f),
            new(5.2f, 0.40f, 0.08f), dark);
        for (int i = 0; i < 6; i++)
            Box(details, null, "SignStripe", new(0.78f + i * 0.89f, 4.68f, hz + 0.26f),
                new(0.32f, 0.24f, 0.036f), accent);
        for (int i = 0; i < 3; i++)
        {
            float x = gateL + 0.5f + i * 2.50f;
            Box(details, body, "Bollard", new(x, 0.64f, hz + 2.17f),
                new(0.23f, 1.27f, 0.23f), accent);
            Box(details, null, "BollardCap", new(x, 1.29f, hz + 2.17f),
                new(0.29f, 0.13f, 0.29f), trim);
        }
        Box(details, body, "SideUtility", new(hx + 0.48f, 1.25f, 2.6f), new(0.82f, 2.1f, 1.60f), concreteDark);
        Box(details, null, "UtilityPanel", new(hx + 0.94f, 1.52f, 2.6f), new(0.08f, 0.67f, 1.0f), sidingLight);
        Cylinder(details, "DrumA", new(hx + 1.2f, 0.55f, -3.6f), 0.42f, 1.1f, grime);
        Cylinder(details, "DrumB", new(hx + 2.1f, 0.50f, -3.8f), 0.38f, 1.0f, roofRib);
    }

    // =========================================================
    private static Node3D Group(Node3D parent, string name)
    {
        var group = new Node3D { Name = name }; parent.AddChild(group); return group;
    }

    // =========================================================
    private static StandardMaterial3D MakeMaterial(Color color, float metallic = 0f) =>
        new() { AlbedoColor = color, Roughness = 0.83f, Metallic = metallic };

    private static readonly BoxMesh UnitBox = new() { Size = Vector3.One };

    // =========================================================
    // Shared primitive geometry; one matching static box collider only when explicitly requested.
    private static void Box(Node3D visuals, StaticBody3D? body, string name,
        Vector3 position, Vector3 size, Material material, float pitch = 0f)
    {
        var mesh = new MeshInstance3D { Name = name, Mesh = UnitBox, MaterialOverride = material,
            Position = position, Rotation = new Vector3(pitch, 0, 0), Scale = size };
        visuals.AddChild(mesh);
        if (body == null) return;
        var shape = new CollisionShape3D { Name = name + "Shape",
            Shape = new BoxShape3D { Size = size }, Position = position,
            Rotation = new Vector3(pitch, 0, 0) };
        FadeAwareAim.Link(shape, mesh); // Matches the shared structure authoring helper.
        body.AddChild(shape);
    }

    // =========================================================
    private static void Cylinder(Node3D visuals, string name, Vector3 position,
        float radius, float height, Material material)
    {
        visuals.AddChild(new MeshInstance3D { Name = name,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 10 },
            MaterialOverride = material, Position = position });
    }

    // =========================================================
    private static void FrontWall(Node3D shell, StaticBody3D body, string name,
        float left, float right, float z, float height, Material siding)
    {
        if (right <= left) return;
        Box(shell, body, name, new((left + right) * 0.5f, height * 0.5f, z),
            new(right - left, height, 0.25f), siding);
    }

    // =========================================================
    private static void Header(Node3D shell, StaticBody3D body, string name,
        float left, float right, float z, float openingTop, float wallTop, Material siding)
    {
        float space = wallTop - openingTop;
        Box(shell, body, name, new((left + right) * 0.5f, openingTop + space * 0.5f, z),
            new(right - left, space, 0.25f), siding);
    }

    // =========================================================
    private static void SideWall(Node3D shell, Node3D details, StaticBody3D body,
        float x, int side, float hz, float h, float thickness,
        Material siding, Material sidingLight, Material trim, Material glass)
    {
        const float z1 = -2.1f, z2 = 1.3f, low = 2.0f, high = 3.52f;
        float a = -hz, b = hz;
        if (z1 <= a || z2 >= b) return;
        Box(shell, body, "SideRear", new(x, h * 0.5f, (a + z1) * 0.5f),
            new(thickness, h, z1 - a), siding);
        Box(shell, body, "SideFront", new(x, h * 0.5f, (z2 + b) * 0.5f),
            new(thickness, h, b - z2), siding);
        Box(shell, body, "SideSill", new(x, low * 0.5f, (z1 + z2) * 0.5f),
            new(thickness, low, z2 - z1), siding);
        Box(shell, body, "SideHeader", new(x, (high + h) * 0.5f, (z1 + z2) * 0.5f),
            new(thickness, h - high, z2 - z1), siding);
        float xo = x + side * (thickness * 0.5f + 0.035f);
        Box(details, body, "SideGlass", new(xo, (low + high) * 0.5f, (z1 + z2) * 0.5f),
            new(0.07f, high - low, z2 - z1), glass);
        Box(details, null, "WindowTop", new(xo + side * 0.05f, high, (z1 + z2) * 0.5f),
            new(0.15f, 0.13f, z2 - z1 + 0.25f), trim);
        Box(details, null, "WindowBottom", new(xo + side * 0.05f, low, (z1 + z2) * 0.5f),
            new(0.15f, 0.15f, z2 - z1 + 0.25f), sidingLight);
        for (int i = 0; i <= 2; i++)
            Box(details, null, "WindowBar", new(xo + side * 0.10f, (low + high) * 0.5f,
                z1 + i * (z2 - z1) * 0.5f), new(0.13f, high - low + 0.1f, 0.10f), trim);
        for (float z = a + 0.7f; z < b; z += 1.15f)
        {
            if (z > z1 - 0.2f && z < z2 + 0.2f) continue;
            Box(details, null, "SidingFlute", new(xo + side * 0.045f, h * 0.54f, z),
                new(0.055f, h * 0.78f, 0.04f), sidingLight);
        }
    }

    // =========================================================
    // One thin triangle on each roof end (double-sided); metal roof panels cover its diagonal sides.
    private static void Gable(Node3D shell, string name, float x, float halfDepth,
        float wallTop, float rise, Material material)
    {
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        surface.SetMaterial(material);
        Vector3 a = new(x, wallTop, -halfDepth), b = new(x, wallTop, halfDepth);
        Vector3 c = new(x, wallTop + rise, 0);
        Face(surface, a, b, c); Face(surface, c, b, a);
        shell.AddChild(new MeshInstance3D { Name = name, Mesh = surface.Commit(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On });
    }

    // =========================================================
    private static void Face(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = (b - a).Cross(c - a).Normalized();
        surface.SetNormal(normal);
        surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
    }

    // =========================================================
    private static void RoofFixture(Node3D roof, float x, float z, float h, float rise,
        float slopeDepth, Material baseMaterial, Material trim)
    {
        float top = h + rise * (1f - Mathf.Abs(z) / slopeDepth);
        Box(roof, null, "VentBase", new(x, top + 0.13f, z), new(1.10f, 0.32f, 1.30f), baseMaterial);
        Box(roof, null, "VentHousing", new(x, top + 0.52f, z), new(0.85f, 0.68f, 1.00f), trim);
        Box(roof, null, "VentCap", new(x, top + 0.93f, z), new(1.12f, 0.15f, 1.27f), baseMaterial);
    }
    // =========================================================
    // Art declares reusable visibility roles; runtime camera/player logic stays outside the mesh generator.
    private void DeclareVisibility()
    {
        var s = Settings ?? new Building001Settings();
        float w = Mathf.Clamp(s.Width, 16, 32), d = Mathf.Clamp(s.Depth, 10, 24);
        StructureLayout.Occupied(GetNode<Node3D>("Generated"), new Aabb(new Vector3(-w / 2, -0.5f, -d / 2), new Vector3(w, 9, d)));
        StructureLayout.Visual(GetNode<Node3D>("Generated/WallsAndTrim"), "walls");
        StructureLayout.Visual(GetNode<Node3D>("Generated/RoofVisuals"), "roof");
        StructureLayout.Visual(GetNode<Node3D>("Generated/Details"), "contents");
    }

}
