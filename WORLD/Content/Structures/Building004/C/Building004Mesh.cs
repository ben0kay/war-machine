// Building004: procedural two-storey field office with a flat roof, usable stairwell and balcony.
// One-time art/collision generation; generic StructureVisibility owns top-down floor presentation.
using Godot;

[Tool]
public partial class Building004Mesh : Node3D
{
    [Export] public Building004Settings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;
    private const float ShaftMinX = -7.1f, ShaftMaxX = 1.9f;
    private const float ShaftMinZ = -4.8f, ShaftMaxZ = -2.5f;

    // =========================================================
    // Author stable geometry and layout once per prefab; no runtime scene regeneration or idle callbacks.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Manual inspection helper; only roof graphics disappear, never roof physics.
    public void SetRoofVisible(bool visible)
    { ShowRoof = visible; if (GodotObject.IsInstanceValid(_roof)) _roof!.Visible = visible; }

    // =========================================================
    // Front is +Z. Every accessible doorway and window is a real gap in solid colliding walls.
    private void Build()
    {
        var s = Settings ?? new Building004Settings();
        float w = Mathf.Clamp(s.Width, 17f, 23f), d = Mathf.Clamp(s.Depth, 13f, 17f);
        float groundHeight = Mathf.Clamp(s.GroundFloorHeight, 3.1f, 3.8f);
        float upperHeight = Mathf.Clamp(s.UpperFloorHeight, 2.8f, 3.5f);
        float hx = w * 0.5f, hz = d * 0.5f, top = groundHeight + upperHeight;

        var brick = StructureGeometry.Paint(s.Brick);
        var brickLight = StructureGeometry.Paint(s.Brick.Lightened(0.13f));
        var concrete = StructureGeometry.Paint(s.Concrete);
        var concreteDark = StructureGeometry.Paint(s.Concrete.Darkened(0.25f));
        var roofMat = StructureGeometry.Paint(s.Roof, 0.12f);
        var metal = StructureGeometry.Paint(s.Metal, 0.22f);
        var glass = StructureGeometry.Paint(s.Glass, 0.08f);
        var accent = StructureGeometry.Paint(s.Accent);
        var paper = StructureGeometry.Paint(new Color(0.70f, 0.69f, 0.61f));

        var generated = StructureGeometry.Group(this, "Generated");
        // Local-space occupancy; separate balcony zone enables correct upstairs reveal while outside the main footprint.
        StructureLayout.Occupied(generated,
            new Aabb(new Vector3(-hx, -0.5f, -hz), new Vector3(w, top + 1.0f, d)));
        var balconyZone = StructureGeometry.Group(generated, "BalconyOccupancy");
        StructureLayout.Occupied(balconyZone,
            new Aabb(new Vector3(-3.35f, groundHeight - 0.35f, hz), new Vector3(6.7f, upperHeight + 0.8f, 2.4f)));

        var lower = StructureGeometry.Group(generated, "Level_0");
        var upper = StructureGeometry.Group(generated, "Level_1");
        StructureLayout.Level(lower, 0, 0f);
        StructureLayout.Level(upper, 1, groundHeight);
        var lowerWall = Tagged(lower, "ExteriorWalls", "walls");
        var lowerContents = Tagged(lower, "GroundContents", "contents");
        var lowerPartitions = Tagged(lower, "Partitions", "walls");
        var upperDeck = Tagged(upper, "FloorAndBalcony", "deck");
        var upperWall = Tagged(upper, "ExteriorWalls", "walls");
        var upperContents = Tagged(upper, "Offices", "contents");
        var upperPartitions = Tagged(upper, "Partitions", "walls");
        _roof = Tagged(upper, "Roof", "roof"); _roof.Visible = ShowRoof;

        // Collision is separate from every visual-role subtree. No opacity change can affect solidity.
        var lowerBody = StructureGeometry.Body(lower);
        var upperBody = StructureGeometry.Body(upper);

        // Thin ground overlays are non-solid; Terrain3D continues to determine the entry floor height.
        StructureGeometry.Box(lowerContents, null, "ConcreteGroundFinish", new(0, 0.05f, 0),
            new(w - 0.25f, 0.09f, d - 0.25f), concreteDark);
        StructureGeometry.Box(lowerContents, null, "FrontFootpath", new(0, 0.06f, hz + 1.15f),
            new(4.1f, 0.10f, 2.3f), concrete);

        // Main street-facing entrance (central, 2.4m clear) and two street-level window bays.
        float front = hz, back = -hz;
        StructureGeometry.WallX(lowerWall, lowerBody, "FrontFarLeft", -hx, -6.6f, front, 0, groundHeight, brick);
        WindowX(lowerWall, lowerBody, "FrontOfficeWest", -6.6f, -4.2f, front, 0f, groundHeight,
            1.0f, 2.38f, brick, glass, metal);
        StructureGeometry.WallX(lowerWall, lowerBody, "FrontLeftPier", -4.2f, -1.2f, front, 0, groundHeight, brick);
        StructureGeometry.WallX(lowerWall, lowerBody, "FrontEntryHeader", -1.2f, 1.2f, front, 2.75f, groundHeight, brick);
        StructureGeometry.WallX(lowerWall, lowerBody, "FrontRightPier", 1.2f, 4.2f, front, 0, groundHeight, brick);
        WindowX(lowerWall, lowerBody, "FrontOfficeEast", 4.2f, 6.6f, front, 0f, groundHeight,
            1.0f, 2.38f, brick, glass, metal);
        StructureGeometry.WallX(lowerWall, lowerBody, "FrontFarRight", 6.6f, hx, front, 0, groundHeight, brick);
        FrameDoor(lowerContents, "PublicEntry", 0f, front + 0.15f, 0f, 2.75f, concrete);

        // Rear staff exit to the right, with a clear walk-through gap instead of a decorative painted door.
        StructureGeometry.WallX(lowerWall, lowerBody, "RearWest", -hx, 5f, back, 0, groundHeight, brick);
        StructureGeometry.WallX(lowerWall, lowerBody, "RearExitHeader", 5f, 7.3f, back, 2.62f, groundHeight, brick);
        StructureGeometry.WallX(lowerWall, lowerBody, "RearEast", 7.3f, hx, back, 0, groundHeight, brick);
        FrameDoor(lowerContents, "ServiceExit", 6.15f, back - 0.17f, 0f, 2.62f, metal);

        foreach (float x in new[] { -hx, hx })
        {
            WindowZ(lowerWall, lowerBody, (x > 0 ? "East" : "West") + "Window",
                x, -hz, hz, -1.3f, 1.7f, 0, groundHeight, 1.10f, 2.50f, brick, glass, metal);
            StructureGeometry.Box(lowerContents, null, "ConcreteCornerPillar",
                new(x, groundHeight * 0.5f, front), new(0.31f, groundHeight, 0.31f), concrete);
            StructureGeometry.Box(lowerContents, null, "RearPillar",
                new(x, groundHeight * 0.5f, back), new(0.31f, groundHeight, 0.31f), concrete);
        }
        StructureGeometry.Box(lowerContents, null, "StoreyBelt",
            new(0, groundHeight - 0.13f, front + 0.17f), new(w + 0.24f, 0.22f, 0.14f), concrete);
        StructureGeometry.Box(lowerContents, null, "OfficePlaque",
            new(-2.66f, 2.94f, front + 0.23f), new(2.05f, 0.39f, 0.14f), metal);
        for (int i = 0; i < 4; i++)
            StructureGeometry.Box(lowerContents, null, "PlaqueLetterBars",
                new(-3.30f + i * 0.38f, 2.95f, front + 0.32f), new(0.22f, 0.10f, 0.03f), accent);

        // Ground dispatch counter and evidence racks; keep the front door, stairs and exits accessible.
        StructureGeometry.Box(lowerContents, lowerBody, "DispatchCounter",
            new(-3.8f, 0.57f, 3.50f), new(2.9f, 1.10f, 0.78f), metal);
        StructureGeometry.Box(lowerContents, null, "CounterTop",
            new(-3.8f, 1.16f, 3.50f), new(3.05f, 0.11f, 0.92f), concrete);
        StructureGeometry.Box(lowerContents, null, "Terminal",
            new(-3.8f, 1.39f, 3.4f), new(0.60f, 0.35f, 0.18f), glass);
        StructureGeometry.Box(lowerContents, lowerBody, "RecordsCabinet",
            new(-hx + 0.96f, 0.91f, -0.2f), new(1.18f, 1.75f, 1.7f), metal);
        for (int i = 0; i < 3; i++)
            StructureGeometry.Box(lowerContents, null, "DrawerPull",
                new(-hx + 1.58f, 0.44f + 0.44f * i, -0.2f), new(0.05f, 0.09f, 0.76f), concrete);

        // A secured office on the ground level, connected through a 2.2m wide actual partition opening.
        StructureGeometry.WallZ(lowerPartitions, lowerBody, "SecureWallRear",
            4.2f, -hz + 0.30f, -1.15f, 0, 2.90f, brickLight, 0.16f);
        StructureGeometry.WallZ(lowerPartitions, lowerBody, "SecureWallFront",
            4.2f, 1.05f, hz - 0.30f, 0, 2.90f, brickLight, 0.16f);
        StructureGeometry.WallZ(lowerPartitions, lowerBody, "SecureDoorHeader",
            4.2f, -1.15f, 1.05f, 2.45f, 2.90f, brickLight, 0.16f);
        StructureGeometry.Box(lowerContents, lowerBody, "SecurityDesk",
            new(6.3f, 0.59f, 3.10f), new(1.45f, 1.15f, 0.78f), metal);

        // Upper deck is four *solid* spans around a genuinely empty rectangular stairwell.
        // It uses no invisible whole-floor collider across the stair hole.
        const float slabThickness = 0.24f, inset = 0.24f;
        float slabY = groundHeight - slabThickness * 0.5f;
        DeckSpan(upperDeck, upperBody, "WestDeck", -hx + inset, ShaftMinX,
            -hz + inset, hz - inset, slabY, slabThickness, concreteDark);
        DeckSpan(upperDeck, upperBody, "EastDeck", ShaftMaxX, hx - inset,
            -hz + inset, hz - inset, slabY, slabThickness, concreteDark);
        DeckSpan(upperDeck, upperBody, "NorthDeck", ShaftMinX, ShaftMaxX,
            -hz + inset, ShaftMinZ, slabY, slabThickness, concreteDark);
        DeckSpan(upperDeck, upperBody, "SouthDeck", ShaftMinX, ShaftMaxX,
            ShaftMaxZ, hz - inset, slabY, slabThickness, concreteDark);

        // Upper facade: open centre balcony door and separate window bays on both sides.
        StructureGeometry.WallX(upperWall, upperBody, "FrontWest", -hx, -6.5f, front, groundHeight, top, brick);
        WindowX(upperWall, upperBody, "UpperFrontWestWindow", -6.5f, -3.9f,
            front, groundHeight, top, 0.88f, 2.23f, brick, glass, metal);
        StructureGeometry.WallX(upperWall, upperBody, "UpperFrontLeftPier",
            -3.9f, -1.2f, front, groundHeight, top, brick);
        StructureGeometry.WallX(upperWall, upperBody, "BalconyDoorHeader",
            -1.2f, 1.2f, front, groundHeight + 2.48f, top, brick);
        StructureGeometry.WallX(upperWall, upperBody, "UpperFrontRightPier",
            1.2f, 3.9f, front, groundHeight, top, brick);
        WindowX(upperWall, upperBody, "UpperFrontEastWindow", 3.9f, 6.5f,
            front, groundHeight, top, 0.88f, 2.23f, brick, glass, metal);
        StructureGeometry.WallX(upperWall, upperBody, "UpperFrontEast",
            6.5f, hx, front, groundHeight, top, brick);
        FrameDoor(upperContents, "BalconyEntry", 0f, front + 0.17f, groundHeight, 2.48f, metal);

        foreach (float z in new[] { back, front })
        {
            if (z > 0) continue; // Upper front was authored above with a real balcony entry.
            StructureGeometry.WallX(upperWall, upperBody, "UpperRearWest", -hx, -6f, z, groundHeight, top, brick);
            WindowX(upperWall, upperBody, "UpperRearWindowWest", -6f, -3.3f,
                z, groundHeight, top, 0.9f, 2.25f, brick, glass, metal);
            StructureGeometry.WallX(upperWall, upperBody, "UpperRearMid", -3.3f, 3.3f, z, groundHeight, top, brick);
            WindowX(upperWall, upperBody, "UpperRearWindowEast", 3.3f, 6f,
                z, groundHeight, top, 0.9f, 2.25f, brick, glass, metal);
            StructureGeometry.WallX(upperWall, upperBody, "UpperRearEast", 6f, hx, z, groundHeight, top, brick);
        }
        foreach (float x in new[] { -hx, hx })
            WindowZ(upperWall, upperBody, (x > 0 ? "UpperEast" : "UpperWest") + "Window",
                x, -hz, hz, -1.4f, 1.5f, groundHeight, top, 0.86f, 2.17f, brick, glass, metal);

        // A small cantilever balcony gives this office a silhouette unlike the ranger lodge.
        // Both upper deck and balcony are truly walkable/solid, at exactly the same elevation.
        StructureGeometry.Box(upperDeck, upperBody, "FrontBalcony",
            new(0, slabY, front + 1.15f), new(6.4f, slabThickness, 2.30f), concreteDark);
        StructureGeometry.Box(upperDeck, upperBody, "FrontBalconyRail",
            new(0, groundHeight + 0.58f, front + 2.26f), new(6.4f, 1.10f, 0.11f), metal);
        foreach (float x in new[] { -3.14f, 3.14f })
        {
            StructureGeometry.Box(upperDeck, upperBody, "BalconySideRail",
                new(x, groundHeight + 0.58f, front + 1.17f), new(0.11f, 1.10f, 2.15f), metal);
            StructureGeometry.Box(lowerContents, lowerBody, "BalconySupportColumn",
                new(x, groundHeight * 0.5f, front + 1.93f),
                new(0.30f, groundHeight, 0.30f), concrete);
        }
        StructureGeometry.Box(upperContents, null, "BalconyLintelTrim",
            new(0, groundHeight + 2.56f, front + 0.23f), new(2.7f, 0.13f, 0.17f), accent);

        // Upper partition stays EAST of the complete stairwell, so a climber has full headroom.
        // The genuine 2.2m passage at z=-1.1..1.1 links the landing and the eastern office.
        const float officeDividerX = 3.65f;
        StructureGeometry.WallZ(upperPartitions, upperBody, "OfficeDividerRear",
            officeDividerX, -hz + 0.25f, -1.10f, groundHeight, top - 0.18f, concrete, 0.16f);
        StructureGeometry.WallZ(upperPartitions, upperBody, "OfficeDividerFront",
            officeDividerX, 1.10f, hz - 0.25f, groundHeight, top - 0.18f, concrete, 0.16f);
        StructureGeometry.WallZ(upperPartitions, upperBody, "OfficeDividerLintel",
            officeDividerX, -1.10f, 1.10f, groundHeight + 2.35f, top - 0.18f, concrete, 0.16f);

        // Furniture remains clear of the stairwell, both landing positions and balcony passage.
        StructureGeometry.Box(upperContents, upperBody, "ArchiveShelf",
            new(-hx + 1.35f, groundHeight + 0.94f, 2.5f), new(0.85f, 1.87f, 2.25f), metal);
        for (int i = 0; i < 3; i++)
            StructureGeometry.Box(upperContents, null, "FileBox",
                new(-hx + 1.75f, groundHeight + 0.41f + i * 0.53f, 2.5f),
                new(0.25f, 0.33f, 0.45f), paper);
        StructureGeometry.Box(upperContents, upperBody, "MeetingTable",
            new(5.6f, groundHeight + 0.50f, 2.90f), new(2.25f, 0.94f, 1.30f), concreteDark);
        StructureGeometry.Box(upperContents, null, "MeetingTableTop",
            new(5.6f, groundHeight + 1.0f, 2.9f), new(2.36f, 0.10f, 1.41f), brickLight);
        StructureGeometry.Box(upperContents, upperBody, "UpperTerminal",
            new(7f, groundHeight + 0.65f, -0.65f), new(1.22f, 1.25f, 0.75f), metal);
        StructureGeometry.Box(upperContents, null, "TerminalScreen",
            new(7f, groundHeight + 1.5f, -0.23f), new(0.77f, 0.61f, 0.05f), glass);
        // Stair railings occupy only the perimeter of the upper-floor opening, not its landing mouth.
        foreach (float z in new[] { ShaftMinZ, ShaftMaxZ })
            StructureGeometry.Box(upperContents, upperBody, "StairwellSafetyRail",
                new((ShaftMinX + ShaftMaxX) * 0.5f, groundHeight + 0.57f, z),
                new(ShaftMaxX - ShaftMinX, 1.03f, 0.10f), metal);

        BuildStairs(generated, groundHeight, concrete, concreteDark, metal);

        // The flat roof is a collidable, separate visual role; hiding it never opens a physics hole.
        StructureGeometry.Box(_roof, upperBody, "ConcreteFlatRoof",
            new(0, top + 0.12f, 0), new(w + 0.55f, 0.24f, d + 0.55f), roofMat);
        foreach (float z in new[] { -hz, hz })
            StructureGeometry.Box(_roof, null, "RoofFrontBackParapet",
                new(0, top + 0.52f, z), new(w + 0.48f, 0.75f, 0.30f), metal);
        foreach (float x in new[] { -hx, hx })
            StructureGeometry.Box(_roof, null, "RoofSideParapet",
                new(x, top + 0.52f, 0), new(0.30f, 0.75f, d + 0.3f), metal);
        StructureGeometry.Box(_roof, null, "RoofVentHousing",
            new(hx - 2.3f, top + 0.72f, -2.6f), new(1.78f, 1.14f, 1.55f), metal);
        for (int i = 0; i < 4; i++)
            StructureGeometry.Box(_roof, null, "VentGrill",
                new(hx - 2.94f + i * 0.42f, top + 1.35f, -2.6f),
                new(0.21f, 0.08f, 1.1f), concrete);
    }

    // =========================================================
    // Mark role-bearing subtrees once. Shared visibility walks only these disjoint groups.
    private static Node3D Tagged(Node3D parent, string name, string role)
    {
        var group = StructureGeometry.Group(parent, name);
        StructureLayout.Visual(group, role); return group;
    }

    // =========================================================
    private static void DeckSpan(Node3D visual, StaticBody3D body, string name,
        float minX, float maxX, float minZ, float maxZ, float y, float thickness, Material mat)
    {
        StructureGeometry.Box(visual, body, name,
            new((minX + maxX) * 0.5f, y, (minZ + maxZ) * 0.5f),
            new(maxX - minX, thickness, maxZ - minZ), mat);
    }

    // =========================================================
    // Real window apertures: two wall spans and a separate physical pane; trim is only decorative.
    private static void WindowX(Node3D visual, StaticBody3D body, string name,
        float left, float right, float z, float y, float top, float sill, float header,
        Material wall, Material glass, Material trim)
    {
        StructureGeometry.WallX(visual, body, name + "Sill", left, right, z, y, y + sill, wall);
        StructureGeometry.WallX(visual, body, name + "Header", left, right, z, y + header, top, wall);
        float mid = (left + right) * 0.5f;
        float side = z >= 0 ? 1 : -1;
        StructureGeometry.Box(visual, body, name + "Pane",
            new(mid, y + (sill + header) * 0.5f, z + side * 0.14f),
            new(right - left, header - sill, 0.06f), glass);
        StructureGeometry.Box(visual, null, name + "HorizontalFrame",
            new(mid, y + sill, z + side * 0.19f),
            new(right - left + 0.11f, 0.11f, 0.13f), trim);
        StructureGeometry.Box(visual, null, name + "VerticalFrame",
            new(mid, y + (sill + header) * 0.5f, z + side * 0.19f),
            new(0.09f, header - sill, 0.10f), trim);
    }

    // =========================================================
    private static void WindowZ(Node3D visual, StaticBody3D body, string name,
        float x, float back, float front, float start, float end,
        float y, float top, float sill, float header, Material wall, Material glass, Material trim)
    {
        StructureGeometry.WallZ(visual, body, name + "Rear", x, back, start, y, top, wall);
        StructureGeometry.WallZ(visual, body, name + "Front", x, end, front, y, top, wall);
        StructureGeometry.WallZ(visual, body, name + "Sill", x, start, end, y, y + sill, wall);
        StructureGeometry.WallZ(visual, body, name + "Header", x, start, end, y + header, top, wall);
        float side = x >= 0 ? 1f : -1f, middle = (start + end) * 0.5f;
        StructureGeometry.Box(visual, body, name + "Pane",
            new(x + side * 0.14f, y + (sill + header) * 0.5f, middle),
            new(0.06f, header - sill, end - start), glass);
        StructureGeometry.Box(visual, null, name + "Mullion",
            new(x + side * 0.21f, y + (sill + header) * 0.5f, middle),
            new(0.10f, header - sill, 0.09f), trim);
    }

    // =========================================================
    // Door surrounds stop at jambs: no new solid barrier across the centre of a real entrance.
    private static void FrameDoor(Node3D props, string name, float x, float z, float y, float height, Material trim)
    {
        foreach (float dx in new[] { -1.2f, 1.2f })
            StructureGeometry.Box(props, null, name + "Jamb",
                new(x + dx, y + height * 0.5f, z), new(0.13f, height, 0.19f), trim);
        StructureGeometry.Box(props, null, name + "Lintel",
            new(x, y + height + 0.06f, z), new(2.5f, 0.12f, 0.17f), trim);
    }

    // =========================================================
    // A crosswise flight makes this structure's internal layout fundamentally different from Building002.
    // Flat decorative treads and ONE continuous walkable sloped collision box avoid vertical riser trapping.
    private static void BuildStairs(Node3D generated, float rise, Material tread, Material riser, Material railing)
    {
        var stairs = StructureGeometry.Group(generated, "Stairs_0_to_1");
        var visuals = Tagged(stairs, "Visuals", "stairs");
        var collision = StructureGeometry.Body(stairs);
        float run = ShaftMaxX - ShaftMinX, width = ShaftMaxZ - ShaftMinZ - 0.26f;
        float cz = (ShaftMinZ + ShaftMaxZ) * 0.5f;
        const int steps = 21;
        for (int i = 0; i < steps; i++)
        {
            float x = ShaftMinX + (i + 0.5f) * run / steps;
            float y = (i + 1) * rise / steps;
            StructureGeometry.Box(visuals, null, "StairTread",
                new(x, y - 0.033f, cz), new(run / steps - 0.02f, 0.066f, width), tread);
            StructureGeometry.Box(visuals, null, "StairRiser",
                new(x - run / steps * 0.44f, y - rise / steps * 0.5f, cz),
                new(0.07f, rise / steps, width), riser);
        }
        float pitch = Mathf.Atan2(rise, run), length = Mathf.Sqrt(run * run + rise * rise);
        collision.AddChild(new CollisionShape3D { Name = "SmoothStairRamp",
            Position = new Vector3((ShaftMinX + ShaftMaxX) * 0.5f, rise * 0.5f - 0.08f, cz),
            Rotation = new Vector3(0, 0, pitch),
            Shape = new BoxShape3D { Size = new Vector3(length, 0.17f, width) } });
        // Sloping handrails are visual only, placed beyond the walking corridor and rotated with the ramp.
        foreach (float z in new[] { ShaftMinZ + 0.07f, ShaftMaxZ - 0.07f })
        {
            var rail = StructureGeometry.Group(visuals, "SlopingRail");
            rail.Position = new Vector3((ShaftMinX + ShaftMaxX) * 0.5f, rise * 0.5f + 0.82f, z);
            rail.Rotation = new Vector3(0, 0, pitch);
            StructureGeometry.Box(rail, null, "Handrail", Vector3.Zero,
                new(length, 0.10f, 0.10f), railing);
            for (int i = 0; i <= 4; i++)
            {
                float u = i / 4f, x = Mathf.Lerp(ShaftMinX, ShaftMaxX, u);
                StructureGeometry.Box(visuals, null, "Newel",
                    new(x, Mathf.Lerp(0.48f, rise + 0.48f, u), z),
                    new(0.10f, 0.96f, 0.10f), railing);
            }
        }
    }
}
