// Double-area 24x16m shop: wide retail glazing, six aisles, two rear rooms and clear front-to-rear passage.
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
        float w = Mathf.Clamp(s.Width, 16f, 32f), d = Mathf.Clamp(s.Depth, 12f, 24f);
        float h = Mathf.Clamp(s.WallHeight, 3.8f, 5.2f);
        float stockDepth = Mathf.Clamp(s.StockroomDepth, 4.5f, 8f);
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
        var servicePartitions = Role(ground, "RearRoomSideWalls", "walls");
        var westCorridorWall = WallGroup(servicePartitions, "WestRoomCorridorWall");
        var eastCorridorWall = WallGroup(servicePartitions, "EastRoomCorridorWall");
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

        // Both exterior doors remain real, clear apertures: main entrance at +Z, rear exit at -Z.
        // The rear rooms sit on either side of a direct 2.7m central service corridor.
        const float serviceHalf = 1.30f, serviceHead = 2.90f;
        // DOOR1: leave the generous 2.6m corridor, but use a real 1.52m service-door opening.
        // One full-width single hinged leaf is much more believable than a 2.6m swinging slab.
        const float rearDoorHalf = 0.76f;
        float serviceLeft = -rearDoorHalf, serviceRight = rearDoorHalf;
        StructureGeometry.WallX(rearWall, body, "RearWest", -hx, serviceLeft, rear, 0, h, stucco);
        StructureGeometry.WallX(rearWall, body, "RearExitHead", serviceLeft, serviceRight, rear, serviceHead, h, stucco);
        StructureGeometry.WallX(rearWall, body, "RearEast", serviceRight, hx, rear, 0, h, stucco);
        FrameDoor(rearWall, "RearServiceExit", 0, rear - 0.13f, 2 * rearDoorHalf, serviceHead, metal);
        // The reusable Door001 owns a real HingeJoint3D + RigidBody3D leaf. Hinge rests beside
        // the left jamb; the doorway/camera fade remains owned by the rear wall group.
        if (!Engine.IsEditorHint())
        {
            const string doorPath = "res://WORLD/Content/Assets/Objects/Static/Doors/Door001/Door001.tscn";
            var doorScene = GD.Load<PackedScene>(doorPath);
            if (doorScene == null) GD.PushError($"ConvenienceStore001: door prefab missing: {doorPath}");
            else
            {
                var rearDoor = doorScene.Instantiate<Node3D>();
                rearDoor.Name = "RearServiceDoor001";
                rearDoor.Position = new Vector3(-0.69f, 0, rear);
                rearWall.AddChild(rearDoor);
            }
        }

        // Long, physically empty shop windows occupy most of BOTH retail side walls.
        // Back-room side walls remain solid for storage and privacy.
        float winStart = divider + 1.05f, winEnd = front - 1.35f;
        const float sideSill = 0.93f, sideHead = 3.42f;
        foreach (float x in new[] { -hx, hx })
        {
            var sideWall = x < 0 ? westWall : eastWall;
            WindowZ(sideWall, body, x < 0 ? "WestRetailWindow" : "EastRetailWindow",
                x, rear, front, winStart, winEnd, sideSill, sideHead, h, stucco, metal);
            StructureGeometry.Box(sideWall, null, "CornerPostFront", new(x, h * 0.5f, front),
                new(0.28f, h, 0.28f), metal);
            StructureGeometry.Box(sideWall, null, "CornerPostRear", new(x, h * 0.5f, rear),
                new(0.28f, h, 0.28f), metal);
        }

        // Two genuinely separate back rooms with their OWN front-facing 2.5m interior openings.
        // The central door-sized opening continues all the way to the exterior rear exit.
        const float roomDoorHalf = 1.25f, roomDoorX = 6.40f;
        const float partitionHead = 2.75f;
        float westLeft = -roomDoorX - roomDoorHalf, westRight = -roomDoorX + roomDoorHalf;
        float eastLeft = roomDoorX - roomDoorHalf, eastRight = roomDoorX + roomDoorHalf;
        StructureGeometry.WallX(partitions, body, "WestOuterSpan", -hx + 0.2f, westLeft, divider, 0, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "WestRoomHeader", westLeft, westRight, divider, partitionHead, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "WestInnerSpan", westRight, -serviceHalf, divider, 0, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "CorridorHeader", -serviceHalf, serviceHalf, divider, partitionHead, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "EastInnerSpan", serviceHalf, eastLeft, divider, 0, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "EastRoomHeader", eastLeft, eastRight, divider, partitionHead, h - 0.16f, insideWall, 0.17f);
        StructureGeometry.WallX(partitions, body, "EastOuterSpan", eastRight, hx - 0.2f, divider, 0, h - 0.16f, insideWall, 0.17f);
        FrameDoor(partitions, "WestStorageEntry", -roomDoorX, divider + 0.12f, roomDoorHalf * 2, partitionHead, metal);
        FrameDoor(partitions, "CentralServicePassage", 0, divider + 0.12f, serviceHalf * 2, partitionHead, metal);
        FrameDoor(partitions, "EastUtilityEntry", roomDoorX, divider + 0.12f, roomDoorHalf * 2, partitionHead, metal);

        // Permanent partitions fully separate the two rooms from the central exit corridor.
        // No wall ever crosses the open central passage; all collision shapes follow their visuals.
        float corridorEdge = serviceHalf + 0.12f;
        StructureGeometry.WallZ(westCorridorWall, body, "WestRoomBoundary", -corridorEdge,
            rear + 0.14f, divider, 0, h - 0.16f, insideWall, 0.18f);
        StructureGeometry.WallZ(eastCorridorWall, body, "EastRoomBoundary", corridorEdge,
            rear + 0.14f, divider, 0, h - 0.16f, insideWall, 0.18f);

        // Fascia/awning live ABOVE the glazing; no wall ornament covers an opening.
        StructureGeometry.Box(frontWall, null, "StoreSignBoard",
            new(0, h - 0.30f, front + 0.20f), new(w - 1.8f, 0.46f, 0.13f), metal);
        StructureGeometry.Box(frontWall, null, "SignAccent",
            new(0, h - 0.27f, front + 0.29f), new(w - 3.6f, 0.15f, 0.06f), accent);
        StructureGeometry.Box(_roof, null, "EntryAwning",
            new(0, h - 0.43f, front + 0.85f), new(4.1f, 0.17f, 1.5f), metal);
        StructureGeometry.Box(_roof, null, "AwningRedEdge",
            new(0, h - 0.47f, front + 1.61f), new(4.12f, 0.15f, 0.09f), accent);

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

        // Reserve continuous circulation and all openings BEFORE adding ANY shelves or decoration.
        // These are nonphysical, cached authoring volumes that future GLB passes can also query.
        var clearance = new StructureClearance(generated);
        clearance.Reserve("FrontEntrance", new(0, 1.18f, front - 0.20f), new(2.30f, 2.36f, 2.35f));
        clearance.Reserve("RearServiceExit", new(0, 1.18f, rear + 0.18f), new(1.32f, 2.36f, 2.2f));
        clearance.Reserve("CentralServiceEntry", new(0, 1.18f, divider), new(2.30f, 2.36f, 2.5f));
        clearance.Reserve("WestStorageEntry", new(-roomDoorX, 1.18f, divider), new(2.30f, 2.36f, 2.4f));
        clearance.Reserve("EastUtilityEntry", new(roomDoorX, 1.18f, divider), new(2.30f, 2.36f, 2.4f));
        clearance.Reserve("RetailMainAisle", new(0, 1.05f, (divider + front) * 0.5f), new(2.35f, 2.10f, front - divider));
        // Cross-aisle feeds BOTH back rooms while the straight middle passage leads outside.
        clearance.Reserve("RetailRearCrossAisle", new(0, 1.05f, divider + 1.35f),
            new(2 * roomDoorX + 2.65f, 2.10f, 1.45f));
        clearance.Reserve("RearExitPassage", new(0, 1.05f, (rear + divider) * 0.5f),
            new(2.28f, 2.10f, divider - rear));
        clearance.Reserve("WestRoomAccess", new(-roomDoorX, 1.05f, divider - 1.20f),
            new(2.20f, 2.10f, 2.35f));
        clearance.Reserve("EastRoomAccess", new(roomDoorX, 1.05f, divider - 1.20f),
            new(2.20f, 2.10f, 2.35f));
        // Window reservations cover their actual apertures, including the sightline through them.
        clearance.Reserve("FrontWindowWest", new((west - entryHalf) * 0.5f, (sill + windowHead) * 0.5f, front),
            new(-entryHalf - west - 0.16f, windowHead - sill - 0.16f, 0.72f));
        clearance.Reserve("FrontWindowEast", new((entryHalf + east) * 0.5f, (sill + windowHead) * 0.5f, front),
            new(east - entryHalf - 0.16f, windowHead - sill - 0.16f, 0.72f));
        foreach (float x in new[] { -hx, hx })
            clearance.Reserve(x < 0 ? "WestRetailWindow" : "EastRetailWindow",
                new(x, (sideSill + sideHead) * 0.5f, (winStart + winEnd) * 0.5f),
                new(0.72f, sideHead - sideSill - 0.16f, winEnd - winStart - 0.16f));

        if (s.ShowFurnishings)
            Furnish(furniture, body, clearance, hx, front, rear, divider,
                Mathf.Clamp(s.ShelfHeight, 0.8f, 1.8f), shelfMetal, shelfDark, metal,
                concrete, cardboard, red, green, yellow);

        // Markers are passive; the future site may use them without coupling the store to any car/lot.
        var markers = StructureGeometry.Group(generated, "Markers");
        Marker(markers, "FrontEntrance", new Vector3(0, 0.12f, front + 0.35f));
        Marker(markers, "CentralServiceEntry", new(0, 0.12f, divider));
        Marker(markers, "StockroomEntrance", new(0, 0.12f, divider)); // Legacy anchor alias for existing tools.
        Marker(markers, "WestStorageEntry", new(-roomDoorX, 0.12f, divider));
        Marker(markers, "EastUtilityEntry", new(roomDoorX, 0.12f, divider));
        Marker(markers, "RearExit", new(0, 0.12f, rear - 0.4f));
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
    // =========================================================
    // Layout second: six readable retail aisles, front till, storage room and utility/service room.
    // Every solid fixture is tested against the previously reserved door and walkway volumes.
    private static void Furnish(Node3D group, StaticBody3D body, StructureClearance clearance, float hx, float front,
        float rear, float divider, float shelfHeight, Material shelf, Material shade,
        Material trim, Material floor, Material boxes, Material red, Material green, Material yellow)
    {
        float retailDepth = front - divider;
        float centre = divider + retailDepth * 0.40f;
        float shelfDepth = Mathf.Clamp(retailDepth * 0.40f, 3.0f, 4.1f);
        // Three lanes on each side; the 2.35m centre aisle remains entirely unobstructed.
        foreach (float x in new[] { -hx + 3.2f, -hx + 6.2f, -3.0f, 3.0f, hx - 6.2f, hx - 3.2f })
            DisplayShelf(group, body, clearance, "RetailShelf", x, centre, 1.08f, shelfHeight,
                shelfDepth, shelf, shade, boxes, red, green, yellow);

        // Checkout visible from the front and left glass, without projecting into the windows.
        float tillX = -hx + 2.65f, tillZ = front - 2.80f;
        if (clearance.Allows("CheckoutCounter", new(tillX, 0.68f, tillZ), new(2.69f, 1.20f, 1.02f)))
        {
            StructureGeometry.Box(group, body, "CheckoutCounter", new(tillX, 0.68f, tillZ),
                new(2.55f, 1.16f, 0.88f), shelf);
            StructureGeometry.Box(group, null, "CounterTop", new(tillX, 1.30f, tillZ),
                new(2.69f, 0.10f, 1.02f), floor);
            StructureGeometry.Box(group, null, "CashTill", new(tillX - 0.65f, 1.44f, tillZ - 0.08f),
                new(0.50f, 0.23f, 0.39f), trim);
            StructureGeometry.Box(group, null, "TillDisplay", new(tillX - 0.65f, 1.65f, tillZ - 0.23f),
                new(0.47f, 0.34f, 0.07f), shade);
            StructureGeometry.Box(group, null, "CardReader", new(tillX + 0.53f, 1.42f, tillZ + 0.19f),
                new(0.25f, 0.16f, 0.30f), trim);
        }

        // West rear room: dry storage. The middle of the room stays clear from its own doorway.
        float backShelfZ = rear + 1.60f;
        DisplayShelf(group, body, clearance, "WestRoomRacking", -hx + 1.7f, backShelfZ,
            1.08f, 1.85f, 1.55f, shelf, shade, boxes, red, green, yellow);
        DisplayShelf(group, body, clearance, "WestRoomRacking", -3.25f, backShelfZ,
            1.08f, 1.85f, 1.55f, shelf, shade, boxes, red, green, yellow);
        Vector3 cratePos = new(-hx + 1.5f, 0.43f, divider - 1.25f);
        if (clearance.Allows("StorageCrate", cratePos, new(0.82f, 0.70f, 0.83f)))
            StructureGeometry.Box(group, body, "StorageCrate", cratePos, new(0.82f, 0.70f, 0.83f), boxes);

        // East rear room: workbench and supplies, accessible through a separate interior opening.
        Vector3 benchPos = new(hx - 2.5f, 0.74f, rear + 1.65f);
        if (clearance.Allows("UtilityWorkbench", benchPos, new(2.75f, 1.23f, 0.94f)))
        {
            StructureGeometry.Box(group, body, "UtilityWorkbench", benchPos,
                new(2.65f, 1.15f, 0.84f), shelf);
            StructureGeometry.Box(group, null, "WorkbenchTop", benchPos + Vector3.Up * 0.63f,
                new(2.75f, 0.10f, 0.96f), trim);
        }
        DisplayShelf(group, body, clearance, "EastRoomSupplies", 3.30f, backShelfZ,
            1.08f, 1.85f, 1.55f, shelf, shade, boxes, red, green, yellow);
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
