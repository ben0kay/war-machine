// Single-floor roadside shop: real OPEN windows, till, retail shelving, partitioned stockroom and back exit.
// Uses common structure geometry, one-time layout metadata and independent colliders. No runtime ticks.
using Godot;

[Tool]
public partial class ConvenienceStoreMesh : Node3D
{
    [Export] public ConvenienceStoreSettings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;

    // =========================================================
    // Construct once; don't reprocess scenery or poll geometry settings.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") == null) Build();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Hide only roof graphics for manual editor preview. Its collision remains solid.
    public void SetRoofVisible(bool visible)
    { ShowRoof = visible; if (GodotObject.IsInstanceValid(_roof)) _roof!.Visible = visible; }

    // =========================================================
    // +Z faces the parking lot; -Z is the rear service entrance.
    private void Build()
    {
        var s = Settings ?? new ConvenienceStoreSettings();
        float w = Mathf.Clamp(s.Width, 14f, 20f), d = Mathf.Clamp(s.Depth, 11f, 16f);
        float h = Mathf.Clamp(s.WallHeight, 3.8f, 5.2f);
        float stockDepth = Mathf.Clamp(s.StockroomDepth, 3.3f, 5.2f);
        float hx = w * 0.5f, hz = d * 0.5f, rear = -hz, front = hz, divider = rear + stockDepth;

        var generated = StructureGeometry.Group(this, "Generated");
        StructureLayout.Occupied(generated, new Aabb(new Vector3(-hx, -0.40f, rear), new Vector3(w, h + 0.95f, d)));
        var ground = StructureGeometry.Group(generated, "Level_0");
        StructureLayout.Level(ground, 0, 0f);
        var walls = Role(ground, "OutsideWalls", "walls");
        var partitions = Role(ground, "StockroomPartition", "walls");
        var furniture = Role(ground, "InteriorFixtures", "contents");
        _roof = Role(ground, "RoofAndAwning", "roof"); _roof.Visible = ShowRoof;
        var body = StructureGeometry.Body(ground);

        var stucco = StructureGeometry.Paint(s.Walls);
        var insideWall = StructureGeometry.Paint(s.Walls.Lightened(0.12f));
        var metal = StructureGeometry.Paint(s.Trim, 0.18f);
        var accent = StructureGeometry.Paint(s.Accent);
        var concrete = StructureGeometry.Paint(s.Floor);
        var roofMaterial = StructureGeometry.Paint(s.Roofing, 0.13f);
        var shelfMetal = StructureGeometry.Paint(s.Shelves, 0.12f);
        var shelfDark = StructureGeometry.Paint(s.Shelves.Darkened(0.23f));
        var cardboard = StructureGeometry.Paint(new Color(0.53f, 0.44f, 0.32f));
        var red = StructureGeometry.Paint(new Color(0.62f, 0.27f, 0.22f));
        var green = StructureGeometry.Paint(new Color(0.27f, 0.46f, 0.35f));
        var yellow = StructureGeometry.Paint(new Color(0.63f, 0.54f, 0.27f));

        // Entire store has a thin, collidable floor at 0.12m, matching the future parking apron.
        StructureGeometry.Box(furniture, body, "WalkableSlab", new Vector3(0, 0.06f, 0),
            new Vector3(w - 0.18f, 0.12f, d - 0.18f), concrete);
        StructureGeometry.Box(furniture, null, "StockroomFloorTone",
            new Vector3(0, 0.127f, (rear + divider) * 0.5f),
            new Vector3(w - 0.42f, 0.010f, stockDepth - 0.22f), insideWall);

        // Huge *unglazed* front windows: real holes, not tinted panes or visual-only gaps.
        const float entryHalf = 1.25f, sill = 0.76f, windowHead = 3.46f, entryHead = 3.05f;
        float west = -hx + 0.82f, east = hx - 0.82f;
        StructureGeometry.WallX(walls, body, "FrontWestPier", -hx, west, front, 0, h, stucco);
        WindowX(walls, body, "BigWestShopfront", west, -entryHalf, front, sill, windowHead, h, stucco, metal);
        StructureGeometry.WallX(walls, body, "FrontDoorHead", -entryHalf, entryHalf, front, entryHead, h, stucco);
        WindowX(walls, body, "BigEastShopfront", entryHalf, east, front, sill, windowHead, h, stucco, metal);
        StructureGeometry.WallX(walls, body, "FrontEastPier", east, hx, front, 0, h, stucco);
        FrameDoor(furniture, "FrontEntry", 0, front + 0.13f, entryHalf * 2, entryHead, metal);

        // Rear exit opens from the separate stockroom to outside. No door mesh, no collider across gap.
        float serviceLeft = hx - 3.5f, serviceRight = hx - 1.1f;
        const float serviceHead = 2.85f;
        StructureGeometry.WallX(walls, body, "RearWest", -hx, serviceLeft, rear, 0, h, stucco);
        StructureGeometry.WallX(walls, body, "RearExitHead", serviceLeft, serviceRight, rear,
            serviceHead, h, stucco);
        StructureGeometry.WallX(walls, body, "RearEast", serviceRight, hx, rear, 0, h, stucco);
        FrameDoor(furniture, "RearServiceExit", (serviceLeft + serviceRight) * 0.5f,
            rear - 0.13f, serviceRight - serviceLeft, serviceHead, metal);

        // Side customer windows also have open space, framed by solid wall spans.
        float winStart = divider + 0.78f, winEnd = front - 1.3f;
        foreach (float x in new[] { -hx, hx })
        {
            WindowZ(walls, body, x < 0 ? "LeftSideWindow" : "RightSideWindow", x, rear, front,
                winStart, winEnd, 1.13f, 3.03f, h, stucco, metal);
            StructureGeometry.Box(furniture, null, "CornerPostFront", new Vector3(x, h * 0.5f, front),
                new Vector3(0.28f, h, 0.28f), metal);
            StructureGeometry.Box(furniture, null, "CornerPostRear", new Vector3(x, h * 0.5f, rear),
                new Vector3(0.28f, h, 0.28f), metal);
        }

        // One internal door connects retail floor to rear stockroom, in line with the main aisle.
        const float partitionDoorHalf = 1.16f, partitionHead = 2.68f;
        StructureGeometry.WallX(partitions, body, "DividerWest", -hx + 0.2f, -partitionDoorHalf,
            divider, 0, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "DividerDoorHead", -partitionDoorHalf,
            partitionDoorHalf, divider, partitionHead, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "DividerEast", partitionDoorHalf, hx - 0.2f,
            divider, 0, h - 0.16f, insideWall, 0.17f);
        FrameDoor(furniture, "StockroomDoor", 0, divider + 0.12f,
            partitionDoorHalf * 2, partitionHead, metal);

        // Simple weather-worn fascia and a shallow entrance awning; no signage text/texture.
        StructureGeometry.Box(walls, null, "StoreSignBoard",
            new Vector3(0, h - 0.48f, front + 0.20f), new Vector3(w - 1.8f, 0.70f, 0.13f), metal);
        StructureGeometry.Box(walls, null, "SignAccent",
            new Vector3(0, h - 0.46f, front + 0.29f), new Vector3(w - 3.6f, 0.32f, 0.06f), accent);
        StructureGeometry.Box(_roof, null, "EntryAwning",
            new Vector3(0, h - 0.72f, front + 0.85f), new Vector3(6.8f, 0.17f, 1.5f), metal);
        StructureGeometry.Box(_roof, null, "AwningRedEdge",
            new Vector3(0, h - 0.76f, front + 1.61f), new Vector3(6.85f, 0.15f, 0.09f), accent);

        StructureGeometry.Box(_roof, body, "FlatRoof", new Vector3(0, h + 0.12f, 0),
            new Vector3(w + 0.35f, 0.24f, d + 0.35f), roofMaterial);
        foreach (float z in new[] { rear, front })
            StructureGeometry.Box(_roof, null, "ParapetX", new Vector3(0, h + 0.47f, z),
                new Vector3(w + 0.35f, 0.5f, 0.25f), metal);
        foreach (float x in new[] { -hx, hx })
            StructureGeometry.Box(_roof, null, "ParapetZ", new Vector3(x, h + 0.47f, 0),
                new Vector3(0.25f, 0.5f, d + 0.35f), metal);
        StructureGeometry.Box(_roof, null, "RoofVent",
            new Vector3(hx - 2.1f, h + 0.70f, rear + 2f),
            new Vector3(1.65f, 0.9f, 1.48f), metal);

        if (s.ShowFurnishings)
            Furnish(furniture, body, hx, front, rear, divider,
                Mathf.Clamp(s.ShelfHeight, 0.8f, 1.8f), shelfMetal, shelfDark, metal,
                concrete, cardboard, red, green, yellow);

        // Markers are passive; the future site may use them without coupling the store to any car/lot.
        var markers = StructureGeometry.Group(generated, "Markers");
        Marker(markers, "FrontEntrance", new Vector3(0, 0.12f, front + 0.35f));
        Marker(markers, "StockroomEntrance", new Vector3(0, 0.12f, divider));
        Marker(markers, "RearExit", new Vector3((serviceLeft + serviceRight) * 0.5f, 0.12f, rear - 0.4f));
    }

    // =========================================================
    // Shelves stay on each side of the centre corridor, with the till near the front windows.
    private static void Furnish(Node3D group, StaticBody3D body, float hx, float front,
        float rear, float divider, float shelfHeight, Material shelf, Material shade,
        Material trim, Material floor, Material boxes, Material red, Material green, Material yellow)
    {
        float depth = Mathf.Clamp(front - divider - 3.85f, 2.45f, 3.55f);
        float centre = divider + 1.2f + depth * 0.5f;
        foreach (float x in new[] { -hx + 2.2f, -2.75f, 2.75f, hx - 2.2f })
            DisplayShelf(group, body, "ShopShelf", x, centre, 1.02f, shelfHeight,
                depth, shelf, shade, boxes, red, green, yellow);

        float tillX = -hx + 2.15f, tillZ = front - 1.45f;
        StructureGeometry.Box(group, body, "CheckoutCounter", new Vector3(tillX, 0.68f, tillZ),
            new Vector3(2.55f, 1.16f, 0.88f), shelf);
        StructureGeometry.Box(group, null, "CounterTop", new Vector3(tillX, 1.30f, tillZ),
            new Vector3(2.69f, 0.10f, 1.02f), floor);
        StructureGeometry.Box(group, null, "CashTill", new Vector3(tillX - 0.65f, 1.44f, tillZ - 0.08f),
            new Vector3(0.50f, 0.23f, 0.39f), trim);
        StructureGeometry.Box(group, null, "TillDisplay", new Vector3(tillX - 0.65f, 1.65f, tillZ - 0.23f),
            new Vector3(0.47f, 0.34f, 0.07f), shade);
        StructureGeometry.Box(group, null, "CardReader", new Vector3(tillX + 0.53f, 1.42f, tillZ + 0.19f),
            new Vector3(0.25f, 0.16f, 0.30f), trim);

        // Stockroom shelving occupies the west/rear side; path from centre door to east exit stays open.
        float stockZ = rear + 1.2f;
        DisplayShelf(group, body, "StockShelfWest", -hx + 1.6f, stockZ,
            0.92f, 1.79f, 1.35f, shelf, shade, boxes, red, green, yellow);
        DisplayShelf(group, body, "StockShelfMiddle", -hx + 4.0f, stockZ,
            0.92f, 1.79f, 1.35f, shelf, shade, boxes, red, green, yellow);
        StructureGeometry.Box(group, body, "StockCrate", new Vector3(-hx + 1.15f, 0.43f, divider - 0.82f),
            new Vector3(0.82f, 0.70f, 0.83f), boxes);
    }

    // =========================================================
    // One collision shape per shelf, with non-colliding coloured stock on three shelf levels.
    private static void DisplayShelf(Node3D group, StaticBody3D body, string name,
        float x, float z, float width, float height, float depth, Material metal, Material dark,
        Material box, Material red, Material green, Material yellow)
    {
        body.AddChild(new CollisionShape3D { Name = name + "Solid",
            Position = new Vector3(x, height * 0.5f + 0.12f, z),
            Shape = new BoxShape3D { Size = new Vector3(width, height, depth) } });
        foreach (float side in new[] { -1f, 1f })
            StructureGeometry.Box(group, null, name + "End",
                new Vector3(x + side * (width * 0.47f), height * 0.5f + 0.12f, z),
                new Vector3(0.065f, height, depth), dark);
        for (int level = 0; level < 3; level++)
        {
            float y = 0.22f + level * (height - 0.15f) / 3f;
            StructureGeometry.Box(group, null, name + "Board",
                new Vector3(x, y, z), new Vector3(width, 0.065f, depth), metal);
            for (int i = 0; i < 4; i++)
            {
                float itemZ = z - depth * 0.34f + i * depth * 0.22f;
                Material colour = (i + level) % 3 == 0 ? red : (i + level) % 3 == 1 ? green : yellow;
                StructureGeometry.Box(group, null, name + "Items",
                    new Vector3(x, y + 0.16f, itemZ),
                    new Vector3(width * 0.53f, 0.26f, depth * 0.15f), colour);
            }
        }
        StructureGeometry.Box(group, null, name + "Top",
            new Vector3(x, height + 0.12f, z), new Vector3(width, 0.065f, depth), metal);
        StructureGeometry.Box(group, null, name + "ShelfLabel", new Vector3(x + width * 0.51f, 0.84f, z),
            new Vector3(0.012f, 0.16f, depth * 0.8f), box);
    }

    // =========================================================
    // Window spans stop above/below the aperture; there is NO pane or invisible collider.
    private static void WindowX(Node3D visual, StaticBody3D body, string label,
        float left, float right, float z, float sill, float head, float height,
        Material wall, Material trim)
    {
        StructureGeometry.WallX(visual, body, label + "Sill", left, right, z, 0, sill, wall);
        StructureGeometry.WallX(visual, body, label + "Header", left, right, z, head, height, wall);
        float mid = (left + right) * 0.5f;
        StructureGeometry.Box(visual, null, label + "SillEdge", new Vector3(mid, sill, z + 0.16f),
            new Vector3(right - left, 0.075f, 0.12f), trim);
        foreach (float x in new[] { left, right })
            StructureGeometry.Box(visual, null, label + "SideFrame",
                new Vector3(x, (sill + head) * 0.5f, z + 0.13f),
                new Vector3(0.10f, head - sill, 0.10f), trim);
    }

    // =========================================================
    // Open side windows with real wall segments on each side, under and above.
    private static void WindowZ(Node3D visual, StaticBody3D body, string label, float x,
        float rear, float front, float start, float end, float sill, float head,
        float height, Material wall, Material trim)
    {
        StructureGeometry.WallZ(visual, body, label + "Back", x, rear, start, 0, height, wall);
        StructureGeometry.WallZ(visual, body, label + "Front", x, end, front, 0, height, wall);
        StructureGeometry.WallZ(visual, body, label + "Sill", x, start, end, 0, sill, wall);
        StructureGeometry.WallZ(visual, body, label + "Header", x, start, end, head, height, wall);
        float z = (start + end) * 0.5f, outward = x < 0 ? -1 : 1;
        StructureGeometry.Box(visual, null, label + "SillEdge",
            new Vector3(x + outward * 0.16f, sill, z),
            new Vector3(0.10f, 0.075f, end - start), trim);
        foreach (float v in new[] { start, end })
            StructureGeometry.Box(visual, null, label + "SideFrame",
                new Vector3(x + outward * 0.13f, (sill + head) * 0.5f, v),
                new Vector3(0.10f, head - sill, 0.10f), trim);
    }

    // =========================================================
    // Decorative door framing never closes a physically open entrance or stockroom route.
    private static void FrameDoor(Node3D visual, string label, float x, float z,
        float width, float head, Material trim)
    {
        foreach (float delta in new[] { -width * 0.5f, width * 0.5f })
            StructureGeometry.Box(visual, null, label + "Jamb",
                new Vector3(x + delta, head * 0.5f, z),
                new Vector3(0.11f, head, 0.18f), trim);
        StructureGeometry.Box(visual, null, label + "TopFrame",
            new Vector3(x, head + 0.045f, z),
            new Vector3(width + 0.13f, 0.095f, 0.19f), trim);
    }

    // =========================================================
    // Disjoint visual roles allow shared per-wall, full roof and external whole-house fading.
    private static Node3D Role(Node3D parent, string name, string role)
    {
        var node = StructureGeometry.Group(parent, name);
        StructureLayout.Visual(node, role); return node;
    }

    // =========================================================
    private static void Marker(Node3D parent, string name, Vector3 position)
        => parent.AddChild(new Marker3D { Name = name, Position = position });
}
