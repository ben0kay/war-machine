// Single-floor roadside shop: real OPEN windows, till, retail shelving, partitioned stockroom and back exit.
// Uses common structure geometry, one-time layout metadata and independent colliders. No runtime ticks.
using Godot;

[Tool]
public partial class ConvenienceStoreMesh : Node3D
{
    [Export] public ConvenienceStoreSettings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    // Only opt in when the whole interior has been surveyed/graded before actors activate.
    [Export] public bool UseTerrainSupport { get; set; }
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
        StructureLayout.FadeGroup(partitions); // Both sides, header and doorway frame fade as one wall.
        var frontWall = WallGroup(walls, "FrontWall");
        var rearWall = WallGroup(walls, "RearWall");
        var westWall = WallGroup(walls, "WestWall");
        var eastWall = WallGroup(walls, "EastWall");
        var furniture = Role(ground, "InteriorFixtures", "contents");
        _roof = Role(ground, "RoofAndAwning", "roof"); _roof.Visible = ShowRoof;
        var body = StructureGeometry.Body(ground);

        // Textured opaque standard materials preserve the shared geometry-based wall fade.
        var stucco = WallFinish(s, false);
        var insideWall = WallFinish(s, true);
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
        Vector3 floorSize = new(w - 0.18f, 0.12f, d - 0.18f);
        // Separate visual clearance from physical height: no z-fighting or raised collision lip.
        StructureGeometry.Box(furniture, null, "WalkableSlab", new Vector3(0, 0.07f, 0), floorSize, concrete);
        if (!UseTerrainSupport)
            body.AddChild(new CollisionShape3D { Name = "WalkableSlabShape", Position = new Vector3(0, 0.06f, 0),
                Shape = new BoxShape3D { Size = floorSize } });
        StructureGeometry.Box(furniture, null, "StockroomFloorTone",
            new Vector3(0, 0.127f, (rear + divider) * 0.5f),
            new Vector3(w - 0.42f, 0.010f, stockDepth - 0.22f), insideWall);

        // Huge *unglazed* front windows: real holes, not tinted panes or visual-only gaps.
        const float entryHalf = 1.25f, sill = 0.76f, windowHead = 3.46f, entryHead = 3.05f;
        float west = -hx + 0.82f, east = hx - 0.82f;
        StructureGeometry.WallX(frontWall, body, "FrontWestPier", -hx, west, front, 0, h, stucco);
        WindowX(frontWall, body, "BigWestShopfront", west, -entryHalf, front, sill, windowHead, h, stucco, metal);
        StructureGeometry.WallX(frontWall, body, "FrontDoorHead", -entryHalf, entryHalf, front, entryHead, h, stucco);
        WindowX(frontWall, body, "BigEastShopfront", entryHalf, east, front, sill, windowHead, h, stucco, metal);
        StructureGeometry.WallX(frontWall, body, "FrontEastPier", east, hx, front, 0, h, stucco);
        FrameDoor(frontWall, "FrontEntry", 0, front + 0.13f, entryHalf * 2, entryHead, metal);

        // Rear exit opens from the separate stockroom to outside. No door mesh, no collider across gap.
        float serviceLeft = hx - 3.5f, serviceRight = hx - 1.1f;
        const float serviceHead = 2.85f;
        StructureGeometry.WallX(rearWall, body, "RearWest", -hx, serviceLeft, rear, 0, h, stucco);
        StructureGeometry.WallX(rearWall, body, "RearExitHead", serviceLeft, serviceRight, rear,
            serviceHead, h, stucco);
        StructureGeometry.WallX(rearWall, body, "RearEast", serviceRight, hx, rear, 0, h, stucco);
        FrameDoor(rearWall, "RearServiceExit", (serviceLeft + serviceRight) * 0.5f,
            rear - 0.13f, serviceRight - serviceLeft, serviceHead, metal);

        // Side customer windows also have open space, framed by solid wall spans.
        float winStart = divider + 0.78f, winEnd = front - 1.3f;
        foreach (float x in new[] { -hx, hx })
        {
            var sideWall = x < 0 ? westWall : eastWall;
            WindowZ(sideWall, body, x < 0 ? "LeftSideWindow" : "RightSideWindow", x, rear, front,
                winStart, winEnd, 1.13f, 3.03f, h, stucco, metal);
            StructureGeometry.Box(sideWall, null, "CornerPostFront", new Vector3(x, h * 0.5f, front),
                new Vector3(0.28f, h, 0.28f), metal);
            StructureGeometry.Box(sideWall, null, "CornerPostRear", new Vector3(x, h * 0.5f, rear),
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
        FrameDoor(partitions, "StockroomDoor", 0, divider + 0.12f,
            partitionDoorHalf * 2, partitionHead, metal);

        // Simple weather-worn fascia and a shallow entrance awning; no signage text/texture.
        StructureGeometry.Box(frontWall, null, "StoreSignBoard",
            new Vector3(0, h - 0.48f, front + 0.20f), new Vector3(w - 1.8f, 0.70f, 0.13f), metal);
        StructureGeometry.Box(frontWall, null, "SignAccent",
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

        // Reserve headroom and horizontal passage BEFORE placing any interior fixtures or future GLBs.
        // Reserved volumes are authored in building-local coordinates and remain queryable as metadata.
        var clearance = new StructureClearance(generated);
        clearance.Reserve("FrontEntrance", new(0, 1.18f, front - 0.20f), new(2.16f, 2.36f, 2.35f));
        clearance.Reserve("StockroomDoor", new(0, 1.18f, divider), new(2.04f, 2.36f, 2.40f));
        float rearDoorX = (serviceLeft + serviceRight) * 0.5f;
        clearance.Reserve("RearServiceExit", new(rearDoorX, 1.18f, rear + 0.15f), new(2.12f, 2.36f, 2.0f));
        // Main public aisle, turn across the stockroom, then travel back to the rear service exit.
        clearance.Reserve("RetailMainAisle", new(0, 1.05f, (divider + front) * 0.5f), new(2.0f, 2.1f, front - divider));
        clearance.Reserve("StockroomCrossAisle", new(rearDoorX * 0.5f, 1.05f, divider - 0.95f),
            new(rearDoorX + 2.0f, 2.1f, 1.50f));
        clearance.Reserve("StockroomServiceAisle", new(rearDoorX, 1.05f, (rear + divider) * 0.5f),
            new(2.0f, 2.1f, divider - rear));
        // Keep the actual unglazed apertures clear of signs, picture frames, clocks and future GLB props.
        clearance.Reserve("FrontWindowWest", new((west - entryHalf) * 0.5f, (sill + windowHead) * 0.5f, front),
            new(-entryHalf - west - 0.16f, windowHead - sill - 0.16f, 0.65f));
        clearance.Reserve("FrontWindowEast", new((entryHalf + east) * 0.5f, (sill + windowHead) * 0.5f, front),
            new(east - entryHalf - 0.16f, windowHead - sill - 0.16f, 0.65f));
        foreach (float x in new[] { -hx, hx })
            clearance.Reserve(x < 0 ? "WestSideWindow" : "EastSideWindow",
                new(x, (1.13f + 3.03f) * 0.5f, (winStart + winEnd) * 0.5f),
                new(0.65f, 3.03f - 1.13f - 0.16f, winEnd - winStart - 0.16f));

        if (s.ShowFurnishings)
            Furnish(furniture, body, clearance, hx, front, rear, divider,
                Mathf.Clamp(s.ShelfHeight, 0.8f, 1.8f), shelfMetal, shelfDark, metal,
                concrete, cardboard, red, green, yellow);

        // Markers are passive; the future site may use them without coupling the store to any car/lot.
        var markers = StructureGeometry.Group(generated, "Markers");
        Marker(markers, "FrontEntrance", new Vector3(0, 0.12f, front + 0.35f));
        Marker(markers, "StockroomEntrance", new Vector3(0, 0.12f, divider));
        Marker(markers, "RearExit", new Vector3((serviceLeft + serviceRight) * 0.5f, 0.12f, rear - 0.4f));
    }

    // =========================================================
    // Build only two opaque wall materials (exterior / brighter partition) from shared
    // NoiseTexture2D resources. World triplanar avoids stretching across wide box walls.
    // StandardMaterial3D remains a valid opaque blocker for shared exterior camera occlusion.
    private static StandardMaterial3D WallFinish(ConvenienceStoreSettings s, bool interior)
    {
        Color color = interior ? s.Walls.Lightened(0.12f) : s.Walls;
        if (!s.UseTexturedWalls) return StructureGeometry.Paint(color);
        const string assets = "res://WORLD/Content/Assets/Structures/ConvenienceStore001/Artwork/Materials/";
        var albedo = ResourceLoader.Load<Texture2D>(assets + "StuccoColor.tres");
        if (albedo == null)
        {
            GD.PushWarning("ConvenienceStore001: stucco colour texture missing; using plain wall finish.");
            return StructureGeometry.Paint(color);
        }
        var normal = s.WallBumpStrength > 0f
            ? ResourceLoader.Load<Texture2D>(assets + "StuccoNormal.tres") : null;
        float repeats = Mathf.Clamp(s.WallTextureRepeatsPerMeter, 0.15f, 2f);
        return new StandardMaterial3D
        {
            AlbedoColor = color, AlbedoTexture = albedo,
            Roughness = Mathf.Clamp(s.WallRoughness, 0.6f, 1f), Metallic = 0f,
            Uv1Triplanar = true, Uv1WorldTriplanar = true, Uv1Scale = Vector3.One * repeats,
            NormalEnabled = normal != null, NormalTexture = normal,
            NormalScale = Mathf.Clamp(s.WallBumpStrength, 0f, 1f)
        };
    }

    // =========================================================
    // Shelves stay on each side of the centre corridor, with the till near the front windows.
    private static void Furnish(Node3D group, StaticBody3D body, StructureClearance clearance, float hx, float front,
        float rear, float divider, float shelfHeight, Material shelf, Material shade,
        Material trim, Material floor, Material boxes, Material red, Material green, Material yellow)
    {
        float depth = Mathf.Clamp(front - divider - 3.85f, 2.45f, 3.55f);
        float centre = divider + 1.2f + depth * 0.5f;
        foreach (float x in new[] { -hx + 2.2f, -2.75f, 2.75f, hx - 2.2f })
            DisplayShelf(group, body, clearance, "ShopShelf", x, centre, 1.02f, shelfHeight,
                depth, shelf, shade, boxes, red, green, yellow);

        float tillX = -hx + 2.15f, tillZ = front - 1.45f;
        if (clearance.Allows("CheckoutCounter", new(tillX, 0.68f, tillZ), new(2.69f, 1.20f, 1.02f)))
        {
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
        }

        // Stockroom shelving occupies the west/rear side; path from centre door to east exit stays open.
        float stockZ = rear + 1.2f;
        DisplayShelf(group, body, clearance, "StockShelfWest", -hx + 1.6f, stockZ,
            0.92f, 1.79f, 1.35f, shelf, shade, boxes, red, green, yellow);
        DisplayShelf(group, body, clearance, "StockShelfMiddle", -hx + 4.0f, stockZ,
            0.92f, 1.79f, 1.35f, shelf, shade, boxes, red, green, yellow);
        if (clearance.Allows("StockCrate", new(-hx + 1.15f, 0.43f, divider - 0.82f), new(0.82f, 0.70f, 0.83f)))
            StructureGeometry.Box(group, body, "StockCrate", new Vector3(-hx + 1.15f, 0.43f, divider - 0.82f),
                new Vector3(0.82f, 0.70f, 0.83f), boxes);
    }

    // =========================================================
    // One collision shape per shelf, with non-colliding coloured stock on three shelf levels.
    private static void DisplayShelf(Node3D group, StaticBody3D body, StructureClearance clearance, string name,
        float x, float z, float width, float height, float depth, Material metal, Material dark,
        Material box, Material red, Material green, Material yellow)
    {
        if (!clearance.Allows(name, new(x, height * 0.5f + 0.12f, z),
            new(width + 0.04f, height + 0.07f, depth))) return;
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
    // New wall assemblies fade as one without merging meshes or changing independent collision.
    private static Node3D WallGroup(Node3D parent, string name)
    {
        var group = StructureGeometry.Group(parent, name);
        StructureLayout.FadeGroup(group); return group;
    }

    // =========================================================
    // Disjoint visual roles allow shared wall groups, full roof and exterior whole-house fading.
    private static Node3D Role(Node3D parent, string name, string role)
    {
        var node = StructureGeometry.Group(parent, name);
        StructureLayout.Visual(node, role); return node;
    }

    // =========================================================
    private static void Marker(Node3D parent, string name, Vector3 position)
        => parent.AddChild(new Marker3D { Name = name, Position = position });
}
