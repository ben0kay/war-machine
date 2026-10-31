// Building002: two-storey walk-in ranger lodge with usable interior stairs and floor-tagged geometry.
// C# creates the provisional low-poly art and static collision once; imported replacements belong in Artwork/.
using Godot;

[Tool]
public partial class Building002Mesh : Node3D
{
    // Upper deck owns the roof of the ground-floor rooms. The wall-top inset
    // is shared with the actual upper slab so partitions do not stop short.
    private const float UpperDeckThickness = 0.24f;
    private const float ReceptionX = -4.5f;

    [Export] public Building002Settings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;
    private Node3D? _level0Visuals, _level1Visuals;
    private float _level0Opacity = 1, _level1Opacity = 1;

    // =========================================================
    // Procedural art is built only once at scene load; idle scene has no ticking callbacks.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build(); DeclareVisibility();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Useful for future isometric roof-hiding logic. No collider is silently changed by fading visuals.
    public void SetRoofVisible(bool visible)
    {
        ShowRoof = visible;
        if (GodotObject.IsInstanceValid(_roof)) _roof!.Visible = visible;
    }

    // =========================================================
    // Visual-only fade entrypoint for future building-level camera controller. Physics remains collidable.
    // Other camera occlusion fades currently act on the entire prefab; Codex must compose those effects.
    public void SetFloorOpacity(int floor, float opacity)
    {
        opacity = Mathf.Clamp(opacity, 0f, 1f);
        Node3D? visual = floor == 0 ? _level0Visuals : floor == 1 ? _level1Visuals : null;
        if (visual == null) return;
        if (floor == 0) { if (Mathf.IsEqualApprox(opacity, _level0Opacity)) return; _level0Opacity = opacity; }
        else { if (Mathf.IsEqualApprox(opacity, _level1Opacity)) return; _level1Opacity = opacity; }
        ApplyOpacity(visual, opacity);
    }

    // =========================================================
    // Only called on floor opacity changes, never per frame. Group contains visuals, not colliders.
    private static void ApplyOpacity(Node root, float opacity)
    {
        if (root is MeshInstance3D mesh) mesh.Transparency = 1f - opacity;
        foreach (Node child in root.GetChildren()) ApplyOpacity(child, opacity);
    }

    // =========================================================
    // Solid outside shell with a real 2m walk-in entrance, windows and two passable interior door gaps.
    private void Build()
    {
        var s = Settings ?? new Building002Settings();
        float w = Mathf.Clamp(s.Width, 14f, 24f), d = Mathf.Clamp(s.Depth, 10f, 18f);
        float h = Mathf.Clamp(s.WallHeight, 3.4f, 5f), rise = Mathf.Clamp(s.RoofRise, 1.4f, 3.5f);
        float hx = w * 0.5f, hz = d * 0.5f;
        float upperHeight = Mathf.Clamp(s.UpperWallHeight, 2.5f, 3.4f);
        float stairFront = hz - 3.45f, stairRear = stairFront - 5.50f;
        float rearOfficeZ = -hz + 3.35f, partitionTop = h - UpperDeckThickness - 0.02f;
        // Place freestanding storage against SOLID wall corners; avoid the
        // front and side glazing, and keep all decoration at the same anchor.
        float receptionZ = hz - 3.65f;
        float stoveX = -hx + 0.115f + 1.05f * 0.5f + 0.08f;
        float stoveZ = hz - 0.115f - 1.06f * 0.5f - 0.08f;
        float cabinetX = -hx + 0.115f + 1.45f * 0.5f + 0.05f;
        float cabinetZ = rearOfficeZ - 0.70f;
        float lockerX = hx - 0.115f - 0.95f * 0.5f - 0.05f;
        float lockerZ = hz - 0.115f - 1.06f * 0.5f - 0.05f;
        float bunkX = hx - 0.115f - 1.65f * 0.5f - 0.07f, bunkZ = -hz + 2.10f;
        if (stairRear <= rearOfficeZ + 0.50f || Mathf.RadToDeg(Mathf.Atan2(h, stairFront - stairRear)) > 44f)
            throw new System.InvalidOperationException(
                "Building002: these footprint/floor-height settings cannot fit the 2m+ stair approach " +
                "and a safely pitched straight flight ahead of the rear-office wall.");
        var generated = StructureGeometry.Group(this, "Generated");
        var clearance = new StructureClearance(generated);
        var lower = StructureGeometry.Group(generated, "Level_0");
        var upper = StructureGeometry.Group(generated, "Level_1");
        lower.SetMeta("floor_level", 0); lower.SetMeta("floor_elevation", 0f);
        upper.SetMeta("floor_level", 1); upper.SetMeta("floor_elevation", h);
        lower.AddToGroup("building_floor_0"); upper.AddToGroup("building_floor_1");
        _level0Visuals = StructureGeometry.Group(lower, "Visuals");
        _level1Visuals = StructureGeometry.Group(upper, "Visuals");
        var shell = StructureGeometry.Group(_level0Visuals, "Exterior");
        var detail = StructureGeometry.Group(_level0Visuals, "TimberAndProps");
        var rooms = StructureGeometry.Group(_level0Visuals, "Rooms");
        var partitions = StructureGeometry.Group(_level0Visuals, "Partitions");
        _roof = StructureGeometry.Group(_level1Visuals, "RoofVisuals"); _roof.Visible = ShowRoof;
        var body = StructureGeometry.Body(lower);
        var upperBody = StructureGeometry.Body(upper);
        var wood = StructureGeometry.Paint(s.Timber);
        var woodLight = StructureGeometry.Paint(s.Timber.Lightened(0.16f));
        var woodDark = StructureGeometry.Paint(s.Timber.Darkened(0.27f));
        var stone = StructureGeometry.Paint(s.Stone);
        var stoneDark = StructureGeometry.Paint(s.Stone.Darkened(0.22f));
        var slate = StructureGeometry.Paint(s.Roof, 0.08f);
        var slateLight = StructureGeometry.Paint(s.Roof.Lightened(0.20f));
        var trim = StructureGeometry.Paint(s.Trim);
        var glass = StructureGeometry.Paint(s.Glass, 0.12f);
        var accent = StructureGeometry.Paint(s.Accent);

        // Decorative floor remains non-colliding so Terrain3D is the walking surface.
        StructureGeometry.Box(detail, null, "PlankInteriorFloor", new(0, 0.07f, 0),
            new(w - 0.20f, 0.10f, d - 0.20f), woodDark);
        for (float x = -hx + 0.55f; x < hx; x += 1.05f)
            StructureGeometry.Box(detail, null, "InteriorFloorboardSeam", new(x, 0.131f, 0),
                new(0.025f, 0.006f, d - 0.36f), woodLight);

        // The FRONT faces +Z. Window spans are real wall holes with separate glass collision.
        float front = hz, winB = 1.25f, winT = 2.75f, doorT = 2.9f;
        StructureGeometry.WallX(shell, body, "FrontFarLeft", -hx, -5.5f, front, 0, h, wood);
        FrontWindow(shell, detail, body, "FrontWindowLeft", -5.5f, -3f, front, winB, winT, h, wood, glass, trim);
        StructureGeometry.WallX(shell, body, "FrontCenterLeft", -3f, -1f, front, 0, h, wood);
        StructureGeometry.WallX(shell, body, "FrontDoorHeader", -1f, 1f, front, doorT, h, wood);
        StructureGeometry.WallX(shell, body, "FrontCenterRight", 1f, 3.5f, front, 0, h, wood);
        FrontWindow(shell, detail, body, "FrontWindowRight", 3.5f, 6f, front, winB, winT, h, wood, glass, trim);
        StructureGeometry.WallX(shell, body, "FrontFarRight", 6f, hx, front, 0, h, wood);
        foreach (float x in new[] { -1.0f, 1.0f })
            StructureGeometry.Box(detail, null, "DoorJamb", new(x, doorT * 0.5f, front + 0.13f),
                new(0.13f, doorT, 0.20f), trim);
        StructureGeometry.Box(detail, null, "DoorLintel", new(0, doorT + 0.04f, front + 0.14f),
            new(2.2f, 0.14f, 0.23f), trim);
        // One slightly raised visual threshold spans the deliberate floor-to-porch
        // transition. It no longer shares an opaque face with either Y=0.12 deck.
        StructureGeometry.Box(detail, null, "DoorThreshold", new(0, 0.143f, front - 0.005f),
            new(2.06f, 0.04f, 0.50f), stone);
        // The door is propped inward and to the side, not blocking entry.
        StructureGeometry.Box(detail, null, "OpenDoorLeaf", new(-1.08f, 1.43f, front - 0.92f),
            new(0.10f, 2.76f, 1.75f), woodDark);

        // Rear wall windows and real side window apertures.
        StructureGeometry.WallX(shell, body, "Rear", -hx, hx, -hz, 0, h, wood);
        foreach (float x in new[] { -4.5f, 4.5f })
        {
            StructureGeometry.Box(detail, null, "RearWindowFrame", new(x, 2.48f, -hz - 0.14f),
                new(2.3f, 1.48f, 0.16f), trim);
            StructureGeometry.Box(detail, null, "RearWindowGlass", new(x, 2.48f, -hz - 0.23f),
                new(2.04f, 1.22f, 0.07f), glass);
            StructureGeometry.Box(detail, null, "RearWindowMullion", new(x, 2.48f, -hz - 0.30f),
                new(0.09f, 1.34f, 0.10f), woodLight);
        }
        SideWindow(shell, detail, body, -hx, -1, hz, h, wood, woodLight, trim, glass);
        SideWindow(shell, detail, body,  hx,  1, hz, h, wood, woodLight, trim, glass);

        // Ground-storey partitions continue to the underside of the actual
        // Y=h upper deck with a 2cm reveal (not the old fixed Y=2.95m).
        // The old right-hand door was centered UNDER the stair's climbing
        // flight (Z=-0.8..1.35). Move it ahead of the first tread, leaving
        // solid wall behind the flight. Its headroom now stays unobstructed.
        const float dividingX = 2.7f;
        float gapA = hz - 2.85f, gapB = hz - 0.65f;
        if (gapA < stairFront + 0.55f || gapB > hz - 0.115f)
            throw new System.InvalidOperationException(
                "Building002: equipment room door must remain clear of the staircase and front shell.");
        // Solid exterior walls are 0.23m thick. Meet their INNER faces
        // (+/-0.115m), not the old 0.25m inset that left 13.5cm daylight.
        const float exteriorInnerFace = 0.115f, jointOverlap = 0.005f;
        StructureGeometry.WallZ(partitions, body, "PartitionRear", dividingX,
            -hz + exteriorInnerFace - jointOverlap, gapA, 0, partitionTop, woodLight, 0.15f);
        StructureGeometry.WallZ(partitions, body, "PartitionFront", dividingX,
            gapB, hz - exteriorInnerFace + jointOverlap, 0, partitionTop, woodLight, 0.15f);
        StructureGeometry.WallZ(partitions, body, "PartitionHeader", dividingX, gapA, gapB, 2.65f, partitionTop, woodLight, 0.15f);
        // Stop the rear partition at the receiving wall's near face so it does
        // not leave a narrow floating seam or project through the intersection.
        StructureGeometry.WallX(partitions, body, "RearOfficeLeft",
            -hx + exteriorInnerFace - jointOverlap, -4.8f,
            rearOfficeZ, 0, partitionTop, woodLight, 0.15f);
        StructureGeometry.WallX(partitions, body, "RearOfficeRight", -2.9f, dividingX - 0.075f,
            rearOfficeZ, 0, partitionTop, woodLight, 0.15f);
        StructureGeometry.WallX(partitions, body, "RearOfficeLintel", -4.8f, -2.9f,
            rearOfficeZ, 2.6f, partitionTop, woodLight, 0.15f);

        // Reserve both the entrance-to-stair walkway and full-height glazing
        // volumes BEFORE furnishing. Use complete prop bounds in every check.
        clearance.Reserve("FrontEntranceVestibule", new(0, 1.45f, (front + stairFront) * 0.5f),
            new(2.4f, 2.9f, front - stairFront));
        clearance.Reserve("GroundStairApproach", new(0.85f, 1.45f, stairFront + 1.15f),
            new(2.6f, 2.9f, 2.3f));
        clearance.Reserve("EquipmentRoomDoor", new(dividingX, 1.45f, (gapA + gapB) * 0.5f),
            new(2.4f, 2.9f, gapB - gapA));
        clearance.Reserve("FrontLeftWindow", new(-4.25f, 2f, front - 0.80f),
            new(2.7f, 1.8f, 1.60f));
        clearance.Reserve("FrontRightWindow", new(4.75f, 2f, front - 0.80f),
            new(2.7f, 1.8f, 1.60f));
        clearance.Reserve("WestSideWindow", new(-hx + 0.65f, 2f, -0.4f),
            new(1.65f, 1.8f, 4.0f));
        clearance.Reserve("EastSideWindow", new(hx - 0.65f, 2f, -0.4f),
            new(1.65f, 1.8f, 4.0f));
        foreach (float windowX in new[] { -4.5f, 4.5f })
            clearance.Reserve("RearWindowSightline", new(windowX, 2f, -hz + 0.80f),
                new(2.6f, 1.8f, 1.60f));

        // Furniture is checked against the reserved circulation and window
        // space, not simply placed by eye. Invalid authoring fails visibly.
        RequireFurnishingClearance(clearance, "ReceptionDesk",
            new(ReceptionX, 0.70f, receptionZ), new(2.90f, 1.40f, 0.95f));
        RequireFurnishingClearance(clearance, "OfficeCabinet",
            new(cabinetX, 1.04f, cabinetZ), new(1.45f, 2.05f, 0.75f));
        RequireFurnishingClearance(clearance, "EquipmentLocker",
            new(lockerX, 1.02f, lockerZ), new(1.08f, 2.04f, 1.06f));
        RequireFurnishingClearance(clearance, "WoodStove",
            new(stoveX, 0.92f, stoveZ), new(1.05f, 1.72f, 1.06f));
        RequireFurnishingClearance(clearance, "GroundBunkBed",
            new(bunkX, 0.54f, bunkZ), new(1.65f, 0.96f, 3.20f));

        // Shift desk, locker, stove and cabinet away from entrance and windows.
        StructureGeometry.Box(rooms, body, "ReceptionDesk", new(ReceptionX, 0.66f, receptionZ),
            new(2.7f, 1.3f, 0.7f), woodDark);
        StructureGeometry.Box(rooms, null, "DeskTop", new(ReceptionX, 1.34f, receptionZ),
            new(2.9f, 0.10f, 0.95f), woodLight);
        StructureGeometry.Box(rooms, body, "OfficeCabinet", new(cabinetX, 1.04f, cabinetZ),
            new(1.45f, 2.05f, 0.75f), woodDark);
        StructureGeometry.Box(rooms, body, "BunkBedBase", new(bunkX, 0.54f, bunkZ),
            new(1.65f, 0.96f, 3.2f), trim);
        StructureGeometry.Box(rooms, null, "Mattress", new(bunkX, 1.08f, bunkZ),
            new(1.53f, 0.17f, 3.04f), slateLight);
        StructureGeometry.Box(rooms, null, "Pillow", new(bunkX, 1.20f, bunkZ - 1.30f),
            new(1.20f, 0.17f, 0.53f), stone);
        StructureGeometry.Box(rooms, body, "EquipmentLocker", new(lockerX, 1.02f, lockerZ),
            new(0.95f, 2.04f, 1.06f), trim);
        StructureGeometry.Box(rooms, null, "LockerDoor", new(lockerX + 0.55f, 1.06f, lockerZ),
            new(0.06f, 1.82f, 0.93f), slate);
        StructureGeometry.Box(rooms, body, "Stove", new(stoveX, 0.92f, stoveZ),
            new(1.05f, 1.72f, 1.06f), trim);
        StructureGeometry.Box(rooms, null, "StoveFirebox", new(stoveX, 0.98f, stoveZ + 0.57f),
            new(0.64f, 0.56f, 0.06f), accent);

        // Thick timber framework and a porch that visually distinguishes this from Building001.
        foreach (float x in new[] { -hx, hx })
        {
            StructureGeometry.Box(detail, null, "CornerPostFront", new(x, h * 0.5f, front),
                new(0.29f, h, 0.30f), trim);
            StructureGeometry.Box(detail, null, "CornerPostBack", new(x, h * 0.5f, -hz),
                new(0.29f, h, 0.30f), trim);
        }
        // The interior plank floor ends at front-0.10m and stays on Terrain3D.
        // Begin the veranda OUTSIDE the facade, not 24.5cm inside it at the
        // same Y as the interior floor (which caused first-person Z-fighting).
        // The doorway's separate raised threshold covers this 16cm transition.
        float porchNear = front + 0.06f, porchFar = front + 3.005f;
        float porchCenter = (porchNear + porchFar) * 0.5f, porchDepth = porchFar - porchNear;
        StructureGeometry.Box(detail, null, "FrontVeranda", new(0, 0.07f, porchCenter),
            new(w - 1.8f, 0.10f, porchDepth), woodLight);
        for (float x = -hx + 0.75f; x < hx - 0.25f; x += 0.85f)
            StructureGeometry.Box(detail, null, "VerandaBoard", new(x, 0.128f, porchCenter),
                new(0.022f, 0.006f, porchDepth - 0.12f), woodDark);
        foreach (float x in new[] { -hx + 1.05f, hx - 1.05f })
        {
            StructureGeometry.Box(detail, body, "PorchPost", new(x, 1.64f, front + 2.68f),
                new(0.26f, 3.28f, 0.26f), trim);
            StructureGeometry.Box(detail, null, "PostFoot", new(x, 0.25f, front + 2.68f),
                new(0.45f, 0.5f, 0.45f), stone);
        }
        StructureGeometry.Box(detail, null, "PorchCrossBeam", new(0, 3.28f, front + 2.68f),
            new(w - 1.78f, 0.32f, 0.32f), trim);
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (hx - 0.90f);
            // These read as physical guards: give the rails and uprights their
            // matching static colliders, using the existing lower-floor body.
            StructureGeometry.Box(detail, body, "PorchHandRail", new(x, 1.00f, front + 1.43f),
                new(0.14f, 0.12f, 2.50f), trim);
            for (int j = 0; j < 3; j++)
                StructureGeometry.Box(detail, body, "PorchBaluster", new(x, 0.57f, front + 0.43f + j * 0.83f),
                    new(0.10f, 1.05f, 0.10f), woodDark);
        }

        StructureGeometry.GableRoof(_roof, upperBody, w, d, h + upperHeight, rise, slate, slateLight, 9);
        // The gable ends are part of the upstairs exterior WALLS (built in
        // BuildUpperFloor), not roof art. Duplicating them here Z-fights and
        // hiding the roof must not open the upstairs exterior facade.
        // Flat lean-to roof over veranda, hidden with the main roof for top-down interior testing.
        StructureGeometry.Box(_roof, null, "PorchRoof", new(0, 3.52f, front + 1.48f),
            new(w - 1.55f, 0.22f, 3.44f), slate);
        StructureGeometry.Box(_roof, null, "PorchRoofFascia", new(0, 3.42f, front + 3.15f),
            new(w - 1.38f, 0.33f, 0.18f), trim);
        // Park the chimney on the roof RIDGE cap rather than letting the old
        // stack descend 0.6m into the upper rooms through a sloping roof sheet.
        // Ridge top = wall-top + rise + 0.11 + 0.21/2; embed 3cm for no seam.
        float chimneyBase = h + upperHeight + rise + 0.185f, chimneyHeight = 1.55f;
        float chimneyX = -w * 0.25f;
        StructureGeometry.Box(_roof, null, "ChimneyStack",
            new(chimneyX, chimneyBase + chimneyHeight * 0.5f, 0f),
            new(1.12f, chimneyHeight, 1.12f), stoneDark);
        StructureGeometry.Box(_roof, null, "ChimneyCap",
            new(chimneyX, chimneyBase + chimneyHeight + 0.085f, 0f),
            new(1.40f, 0.17f, 1.40f), stone);
        StructureGeometry.Box(detail, null, "RangerStationSign", new(0, 3.45f, front + 0.24f),
            new(3.6f, 0.43f, 0.13f), trim);
        for (int i = 0; i < 5; i++)
            StructureGeometry.Box(detail, null, "SignInlay", new(-1.40f + i * 0.70f, 3.45f, front + 0.32f),
                new(0.35f, 0.17f, 0.045f), accent);
        AddShowcaseDetails(_level0Visuals, body, s, hx, hz, h,
            ReceptionX, receptionZ, cabinetX, cabinetZ, bunkX, bunkZ);
        BuildUpperFloor(generated, _level1Visuals, upperBody, clearance, w, d, h, upperHeight,
            stairFront, stairRear, rise, s.StairTreadCount,
            wood, woodLight, woodDark, slateLight, trim, glass, stone);
    }



    // =========================================================
    // Fail at authoring time if a fixed fixture crosses a reserved doorway,
    // approach lane or window volume. This guard runs once, not every frame.
    private static void RequireFurnishingClearance(StructureClearance clearance,
        string name, Vector3 centre, Vector3 size)
    {
        if (!clearance.Allows(name, centre, size))
            throw new System.InvalidOperationException(
                $"Building002: {name} intersects a reserved circulation or window volume.");
    }

    // =========================================================
    // The second storey is kept on a separate, tagged structural floor. All coordinates are local
    // to the building. Upper floors and walls use dedicated solid collision that never fades.
    private static void BuildUpperFloor(Node3D generated, Node3D upperVisuals, StaticBody3D upperBody,
        StructureClearance clearance, float width, float depth, float floorY, float wallHeight,
        float stairFront, float stairRear, float roofRise, int stairTreads,
        Material wood, Material woodLight, Material woodDark, Material bedding, Material trim,
        Material glass, Material stone)
    {
        float hx = width * 0.5f, hz = depth * 0.5f, top = floorY + wallHeight;
        var deck = StructureGeometry.Group(upperVisuals, "FloorDeck");
        var walls = StructureGeometry.Group(upperVisuals, "ExteriorWalls");
        var furnishings = StructureGeometry.Group(upperVisuals, "Furnishings");
        var partitions = StructureGeometry.Group(upperVisuals, "Partitions");

        // The roof extends 0.5m beyond both Z eaves. The former flat upper
        // end walls stopped a few centimetres short of the sloped roof underside.
        float roofHalfDepth = hz + 0.5f;
        float eaveRoofUnderside = top + roofRise * (0.5f / roofHalfDepth) - 0.07f;
        float bedroomHalfDepth = hz - 0.12f;
        float bedroomEave = top + roofRise *
            (1f - bedroomHalfDepth / roofHalfDepth) - 0.07f;
        float bedroomRidge = top + roofRise - 0.07f;

        // Reserve a genuine open stairwell. The upper deck has four independent solid spans,
        // not a complete floor with a hidden/uncollidable hole painted into it.
        const float shaftLeft = -0.20f, shaftRight = 1.90f, deckInset = 0.24f;
        float deckThickness = UpperDeckThickness;
        float deckCenter = floorY - deckThickness * 0.5f;
        FloorSpan(deck, upperBody, "WestUpperFloor", -hx + deckInset, shaftLeft,
            -hz + deckInset, hz - deckInset, deckCenter, deckThickness, woodDark);
        FloorSpan(deck, upperBody, "EastUpperFloor", shaftRight, hx - deckInset,
            -hz + deckInset, hz - deckInset, deckCenter, deckThickness, woodDark);
        FloorSpan(deck, upperBody, "RearUpperLanding", shaftLeft, shaftRight,
            -hz + deckInset, stairRear, deckCenter, deckThickness, woodDark);
        FloorSpan(deck, upperBody, "FrontUpperLanding", shaftLeft, shaftRight,
            stairFront, hz - deckInset, deckCenter, deckThickness, woodDark);

        // Upper exterior: true apertures in solid walls, two windows on front and back.
        // No long crossing facade strip; opening widths and collision match the visuals.
        foreach (float z in new[] { -hz, hz })
        {
            string face = z > 0 ? "UpperFront" : "UpperRear";
            StructureGeometry.WallX(walls, upperBody, face + "FarLeft", -hx, -5.2f,
                z, floorY, eaveRoofUnderside, wood);
            UpperWindow(walls, upperBody, face + "LeftWindow", -5.2f, -3.1f,
                z, floorY, eaveRoofUnderside, wood, glass, trim);
            StructureGeometry.WallX(walls, upperBody, face + "Middle", -3.1f, 3.1f,
                z, floorY, eaveRoofUnderside, wood);
            UpperWindow(walls, upperBody, face + "RightWindow", 3.1f, 5.2f,
                z, floorY, eaveRoofUnderside, wood, glass, trim);
            StructureGeometry.WallX(walls, upperBody, face + "FarRight", 5.2f, hx,
                z, floorY, eaveRoofUnderside, wood);
            StructureGeometry.Box(walls, null, face + "SillTrim", new(0, floorY + 0.15f, z + (z > 0 ? 0.15f : -0.15f)),
                new(width - 0.30f, 0.17f, 0.12f), trim);
        }
        // Side upper windows face the garden, with real wall gaps and glazing.
        foreach (float x in new[] { -hx, hx })
        {
            string side = x > 0 ? "UpperEast" : "UpperWest";
            StructureGeometry.WallZ(walls, upperBody, side + "Rear", x, -hz, -1.35f, floorY, top, wood);
            StructureGeometry.WallZ(walls, upperBody, side + "Front", x, 1.35f, hz, floorY, top, wood);
            StructureGeometry.WallZ(walls, upperBody, side + "Sill", x, -1.35f, 1.35f,
                floorY, floorY + 0.80f, wood);
            StructureGeometry.WallZ(walls, upperBody, side + "Header", x, -1.35f, 1.35f,
                floorY + 2.04f, top, wood);
            StructureGeometry.Box(walls, upperBody, side + "Glass",
                new(x + (x > 0 ? 0.14f : -0.14f), floorY + 1.42f, 0),
                new(0.06f, 1.24f, 2.70f), glass);
            StructureGeometry.Box(walls, null, side + "WindowMullion",
                new(x + (x > 0 ? 0.24f : -0.24f), floorY + 1.42f, 0),
                new(0.10f, 1.25f, 0.08f), trim);
        }

        // The bedroom divider used to stop at a constant top, leaving a large
        // triangular opening under the pitched roof. Close that opening at the
        // real roof slope; the real doorway below Y=floorY+2.24 stays unchanged.
        const float roomX = -2.65f, partitionThickness = 0.15f;
        const float facadeThicknessHalf = 0.115f, wallJoin = 0.005f;
        StructureGeometry.WallZ(partitions, upperBody, "BedroomWallRear", roomX,
            -hz + facadeThicknessHalf - wallJoin, -1.20f,
            floorY, bedroomEave, woodLight, partitionThickness);
        StructureGeometry.WallZ(partitions, upperBody, "BedroomWallFront", roomX,
            0.90f, hz - facadeThicknessHalf + wallJoin,
            floorY, bedroomEave, woodLight, partitionThickness);
        StructureGeometry.WallZ(partitions, upperBody, "BedroomDoorHeader", roomX,
            -1.20f, 0.90f, floorY + 2.24f, bedroomEave, woodLight, partitionThickness);
        // GableEnd is a single solid-looking triangular SURFACE (two-sided),
        // not a duplicate full-width wall, and does not create a phantom collider.
        StructureGeometry.GableEnd(partitions, "BedroomRoofInfill",
            roomX, bedroomHalfDepth, bedroomEave, bedroomRidge - bedroomEave, woodLight);

        // Exterior triangular end panels are WALL-owned. Remove the former
        // copies from the roof group so no overlapping faces exist.
        StructureGeometry.GableEnd(walls, "EastGable", hx, hz, top, roofRise, wood);
        StructureGeometry.GableEnd(walls, "WestGable", -hx, hz, top, roofRise, wood);

        // Fixed furniture is anchored to intact corner/solid wall bays, NOT
        // pushed into windows or the stair landing. Whole prop footprints are
        // checked once against the same clearance reservations as ground level.
        float bedX = (-hx + (-5.20f)) * 0.5f;
        float bedZ = -hz + 0.115f + 3.15f * 0.5f + 0.12f;
        float wardrobeX = -hx + 0.115f + 0.90f * 0.5f + 0.06f;
        float wardrobeZ = hz - 0.115f - 1.40f * 0.5f - 0.10f;
        float studyX = hx - 0.115f - 2.80f * 0.5f - 0.14f;
        float studyZ = -hz + 0.115f + 1.08f * 0.5f + 0.08f;
        float chestX = hx - 0.115f - 1.65f * 0.5f - 0.08f;
        float chestZ = hz - 0.115f - 1.25f * 0.5f - 0.08f;
        foreach (float windowZ in new[] { -hz, hz })
            foreach (float windowX in new[] { -4.15f, 4.15f })
                clearance.Reserve("UpperFacadeWindow", new(windowX, floorY + 1.45f,
                    windowZ + (windowZ > 0 ? -0.52f : 0.52f)),
                    new(2.35f, 1.75f, 1.25f));
        foreach (float windowX in new[] { -hx + 0.52f, hx - 0.52f })
            clearance.Reserve("UpperSideWindow", new(windowX, floorY + 1.45f, 0f),
                new(1.10f, 1.75f, 3.20f));
        clearance.Reserve("UpperStairOpening", new((shaftLeft + shaftRight) * 0.5f,
            floorY + 1.35f, (stairFront + stairRear) * 0.5f),
            new(shaftRight - shaftLeft + 0.35f, 2.7f, stairFront - stairRear + 0.4f));
        RequireFurnishingClearance(clearance, "GuestBed",
            new(bedX, floorY + 0.55f, bedZ), new(1.90f, 1.10f, 3.15f));
        RequireFurnishingClearance(clearance, "GuestWardrobe",
            new(wardrobeX, floorY + 1.0f, wardrobeZ), new(0.90f, 2.0f, 1.40f));
        RequireFurnishingClearance(clearance, "UpperStudyDesk",
            new(studyX, floorY + 0.60f, studyZ), new(2.80f, 1.20f, 1.08f));
        RequireFurnishingClearance(clearance, "SupplyChest",
            new(chestX, floorY + 0.47f, chestZ), new(1.65f, 0.94f, 1.25f));

        // Bed is centred on the solid rear-left wall bay (before its window).
        // All bedding derives its offset from the bed frame.
        StructureGeometry.Box(furnishings, upperBody, "GuestBedFrame",
            new(bedX, floorY + 0.42f, bedZ), new(1.90f, 0.74f, 3.15f), trim);
        StructureGeometry.Box(furnishings, null, "GuestMattress",
            new(bedX, floorY + 0.85f, bedZ), new(1.78f, 0.15f, 3.0f), bedding);
        StructureGeometry.Box(furnishings, null, "GuestBlanket",
            new(bedX, floorY + 0.96f, bedZ + 0.70f), new(1.76f, 0.08f, 1.40f), woodLight);
        StructureGeometry.Box(furnishings, null, "GuestPillow",
            new(bedX, floorY + 1.02f, bedZ - 1.10f), new(1.20f, 0.14f, 0.50f), stone);
        StructureGeometry.Box(furnishings, upperBody, "GuestWardrobe",
            new(wardrobeX, floorY + 1.0f, wardrobeZ), new(0.90f, 2.00f, 1.40f), woodDark);
        StructureGeometry.Box(furnishings, null, "GuestWardrobeDoors",
            new(wardrobeX + 0.47f, floorY + 1.08f, wardrobeZ),
            new(0.055f, 1.70f, 1.25f), trim);
        StructureGeometry.Box(furnishings, upperBody, "UpperStudyDesk",
            new(studyX, floorY + 0.57f, studyZ), new(2.65f, 1.08f, 0.85f), woodDark);
        StructureGeometry.Box(furnishings, null, "StudyDeskTop",
            new(studyX, floorY + 1.16f, studyZ), new(2.80f, 0.10f, 1.08f), woodLight);
        StructureGeometry.Box(furnishings, upperBody, "SupplyChest",
            new(chestX, floorY + 0.45f, chestZ), new(1.55f, 0.82f, 1.15f), woodDark);
        StructureGeometry.Box(furnishings, null, "SupplyChestLid",
            new(chestX, floorY + 0.88f, chestZ), new(1.65f, 0.09f, 1.25f), trim);
        for (int i = 0; i < 3; i++)
            StructureGeometry.Box(furnishings, null, "UpperBooks",
                new(studyX - 0.85f + i * 0.27f, floorY + 1.28f, studyZ),
                new(0.15f, 0.20f + i * 0.05f, 0.38f), i % 2 == 0 ? trim : stone);

        // The stairwell remains open above the flight, regardless of tread count.
        // Low waist railings guard the upstairs edges but leave the rear landing open to enter/exit.
        foreach (float x in new[] { shaftLeft, shaftRight })
        {
            StructureGeometry.Box(furnishings, upperBody, "UpperStairGuard",
                new(x, floorY + 0.51f, (stairFront + stairRear) * 0.5f),
                new(0.12f, 1.02f, stairFront - stairRear), trim);
            for (int j = 0; j <= 3; j++)
                StructureGeometry.Box(furnishings, null, "UpperRailPost",
                    new(x, floorY + 0.52f, stairRear + j * (stairFront - stairRear) / 3f),
                    new(0.20f, 1.04f, 0.20f), woodLight);
        }
        BuildStairs(generated, floorY, shaftLeft, shaftRight, stairFront, stairRear,
            stairTreads, woodLight, woodDark, trim);
    }

    // =========================================================
    // Collidable flat deck rectangles around the open stairwell; never put a floor collider over stairs.
    private static void FloorSpan(Node3D visuals, StaticBody3D collision, string name,
        float minX, float maxX, float minZ, float maxZ, float y, float thickness, Material material)
    {
        if (maxX <= minX || maxZ <= minZ) return;
        StructureGeometry.Box(visuals, collision, name,
            new((minX + maxX) * 0.5f, y, (minZ + maxZ) * 0.5f),
            new(maxX - minX, thickness, maxZ - minZ), material);
    }

    // =========================================================
    // Full upper window opening with separate sill/header, matching glass and non-colliding trim.
    private static void UpperWindow(Node3D visuals, StaticBody3D collision, string name,
        float minX, float maxX, float z, float floorY, float top,
        Material wall, Material glass, Material trim)
    {
        const float sill = 0.84f, windowTop = 2.05f;
        StructureGeometry.WallX(visuals, collision, name + "Sill", minX, maxX,
            z, floorY, floorY + sill, wall);
        StructureGeometry.WallX(visuals, collision, name + "Header", minX, maxX,
            z, floorY + windowTop, top, wall);
        float midX = (minX + maxX) * 0.5f, offset = z > 0 ? 0.14f : -0.14f;
        StructureGeometry.Box(visuals, collision, name + "Glass",
            new(midX, floorY + (sill + windowTop) * 0.5f, z + offset),
            new(maxX - minX, windowTop - sill, 0.06f), glass);
        StructureGeometry.Box(visuals, null, name + "WindowSillTrim",
            new(midX, floorY + sill, z + offset * 1.5f),
            new(maxX - minX + 0.12f, 0.10f, 0.13f), trim);
        StructureGeometry.Box(visuals, null, name + "WindowMullion",
            new(midX, floorY + (sill + windowTop) * 0.5f, z + offset * 1.5f),
            new(0.08f, windowTop - sill, 0.10f), trim);
    }

    // =========================================================
    // The existing one-box walking ramp remains continuous, independent of the
    // visual step count. Riser tops sit beneath the treads, never coplanar.
    private static void BuildStairs(Node3D generated, float rise, float left, float right,
        float front, float rear, int treadCount, Material tread, Material riser, Material rail)
    {
        var stairs = StructureGeometry.Group(generated, "Stairs_0_to_1");
        stairs.SetMeta("connects_floor_from", 0);
        stairs.SetMeta("connects_floor_to", 1);
        stairs.SetMeta("lower_elevation", 0f);
        stairs.SetMeta("upper_elevation", rise);
        stairs.AddToGroup("building_stairs");
        var visuals = StructureGeometry.Group(stairs, "Visuals");
        var collision = StructureGeometry.Body(stairs);
        int count = Mathf.Clamp(treadCount, 12, 22);
        float run = front - rear, stepDepth = run / count, stepRise = rise / count;
        float centerX = (left + right) * 0.5f, width = right - left - 0.22f;
        const float treadThickness = 0.14f, riserThickness = 0.09f;
        if (stepRise <= treadThickness + 0.035f)
            throw new System.InvalidOperationException(
                "Building002: tread thickness exceeds the available rise.");
        for (int i = 0; i < count; i++)
        {
            float z = front - (i + 0.5f) * stepDepth, top = (i + 1) * stepRise;
            // The thicker tread hides the top of the riser. Individual visual
            // faces do not share a coplanar top plane with the adjacent tread.
            StructureGeometry.Box(visuals, null, "StepTread",
                new(centerX, top - treadThickness * 0.5f, z),
                new(width, treadThickness, stepDepth - 0.008f), tread);
            float riserHeight = stepRise - treadThickness + 0.030f;
            float riserY = top - treadThickness - riserHeight * 0.5f + 0.015f;
            float riserZ = z + stepDepth * 0.5f - riserThickness * 0.5f;
            StructureGeometry.Box(visuals, null, "StepRiser",
                new(centerX, riserY, riserZ),
                new(width, riserHeight, riserThickness), riser);
        }
        float pitch = Mathf.Atan2(rise, run), slopedLength = Mathf.Sqrt(run * run + rise * rise);
        collision.AddChild(new CollisionShape3D { Name = "SmoothWalkableRamp",
            Position = new Vector3(centerX, rise * 0.5f - 0.025f, (front + rear) * 0.5f),
            Rotation = new Vector3(pitch, 0, 0),
            Shape = new BoxShape3D { Size = new Vector3(width, 0.15f, slopedLength) } });
        // Sloping handrails parallel the ramp and are outside the usable walking width.
        foreach (float x in new[] { left + 0.07f, right - 0.07f })
        {
            StructureGeometry.Box(visuals, collision, "StairHandrail",
                new(x, rise * 0.5f + 0.83f, (front + rear) * 0.5f),
                new(0.09f, 0.10f, slopedLength), rail, pitch);
            for (int i = 0; i <= 5; i++)
            {
                float u = i / 5f;
                StructureGeometry.Box(visuals, null, "StairNewel",
                    new(x, rise * u + 0.40f, front - u * run),
                    new(0.10f, 0.80f, 0.10f), rail);
            }
        }
    }

    // =========================================================
    // A separate dressing pass: all details remain optional visuals or selective furniture collision.
    // Keep exterior openings, walk-through interior partitions and overall footprint untouched.
    private static void AddShowcaseDetails(Node3D generated, StaticBody3D body,
        Building002Settings settings, float hx, float hz, float h,
        float receptionX, float receptionZ, float cabinetX, float cabinetZ,
        float bunkX, float bunkZ)
    {
        var exterior = StructureGeometry.Group(generated, "ShowcaseExterior");
        var interior = StructureGeometry.Group(generated, "ShowcaseInterior");
        var carved = StructureGeometry.Paint(settings.Timber.Darkened(0.18f));
        var pale = StructureGeometry.Paint(settings.Timber.Lightened(0.24f));
        var moss = StructureGeometry.Paint(new Color(0.29f, 0.38f, 0.24f));
        var leaves = StructureGeometry.Paint(new Color(0.39f, 0.52f, 0.31f));
        var clay = StructureGeometry.Paint(new Color(0.57f, 0.34f, 0.25f));
        var paper = StructureGeometry.Paint(new Color(0.72f, 0.68f, 0.53f));
        var ink = StructureGeometry.Paint(new Color(0.26f, 0.31f, 0.28f));
        var steel = StructureGeometry.Paint(new Color(0.32f, 0.37f, 0.38f), 0.18f);
        var cloth = StructureGeometry.Paint(new Color(0.32f, 0.43f, 0.39f));
        var yellow = StructureGeometry.Paint(settings.Accent);

        // EXTERIOR: thin weatherboard seams lie ONLY over solid wall spans.
        // The door is open from X=-1..1 and Y=0..2.9; the front windows are open at Y=1.25..2.75.
        // Never let a decorative strip bridge any portion of those openings (including at sill height).
        for (float y = 0.73f; y < h - 0.38f; y += 0.52f)
        {
            StructureGeometry.Box(exterior, null, "RearWeatherboard", new(0, y, -hz - 0.14f),
                new(hx * 2f - 0.32f, 0.065f, 0.045f), carved);
            foreach (var seg in new[] { (-hx + 0.15f, -5.55f), (-2.95f, -1.06f),
                                       (1.06f, 3.43f), (6.06f, hx - 0.15f) })
                StructureGeometry.Box(exterior, null, "FrontWallWeatherboard",
                    new((seg.Item1 + seg.Item2) * 0.5f, y, hz + 0.14f),
                    new(seg.Item2 - seg.Item1, 0.065f, 0.045f), carved);

            // Window sill and header levels only; keep the openings themselves unbridged.
            if (y < 1.20f || y > 2.80f)
                foreach (var seg in new[] { (-5.5f, -3.0f), (3.5f, 6.0f) })
                    StructureGeometry.Box(exterior, null, "FrontWindowBayWeatherboard",
                        new((seg.Item1 + seg.Item2) * 0.5f, y, hz + 0.14f),
                        new(seg.Item2 - seg.Item1, 0.065f, 0.045f), carved);

            // There is wall above the 2.9m doorway, but not below the lintel.
            if (y > 2.96f)
                StructureGeometry.Box(exterior, null, "AboveDoorWeatherboard",
                    new(0, y, hz + 0.14f), new(2.0f, 0.065f, 0.045f), carved);
        }
        foreach (float x in new[] { -hx - 0.15f, hx + 0.15f })
            for (float y = 0.80f; y < h - 0.38f; y += 0.58f)
            {
                if (y > 1.15f && y < 2.84f)
                {
                    StructureGeometry.Box(exterior, null, "SideBoardRear", new(x, y, -hz * 0.68f),
                        new(0.055f, 0.06f, hz * 0.52f), carved);
                    StructureGeometry.Box(exterior, null, "SideBoardFront", new(x, y, hz * 0.70f),
                        new(0.055f, 0.06f, hz * 0.47f), carved);
                }
                else StructureGeometry.Box(exterior, null, "SideWeatherboard", new(x, y, 0),
                    new(0.055f, 0.06f, hz * 2f - 0.35f), carved);
            }

        // Weathering as thin geometry patches rather than GPU decals or additional shader passes.
        foreach (float x in new[] { -hx + 1.2f, -hx + 2.9f, hx - 1.55f })
        {
            StructureGeometry.Box(exterior, null, "SidingStain", new(x, 0.72f, hz + 0.158f),
                new(0.18f, 0.91f, 0.018f), moss);
            StructureGeometry.Box(exterior, null, "BottomMoss", new(x + 0.20f, 0.26f, hz + 0.17f),
                new(0.75f, 0.24f, 0.023f), moss);
        }
        // Wall plaques/noticeboard and exterior electrical/conduit detailing are shallow and non-colliding.
        StructureGeometry.Box(exterior, null, "TrailNoticeBacking", new(-hx + 1.56f, 2.15f, hz + 0.21f),
            new(1.45f, 1.18f, 0.12f), carved);
        StructureGeometry.Box(exterior, null, "TrailNoticePaper", new(-hx + 1.56f, 2.17f, hz + 0.29f),
            new(1.19f, 0.92f, 0.015f), paper);
        for (int i = 0; i < 4; i++)
            StructureGeometry.Box(exterior, null, "NoticeLine", new(-hx + 1.5f, 2.38f - i * 0.16f, hz + 0.31f),
                new(0.74f, 0.028f, 0.017f), ink);
        StructureGeometry.Box(exterior, null, "UtilityConduit", new(hx + 0.19f, 2.25f, hz - 1.15f),
            new(0.08f, 2.65f, 0.08f), steel);
        StructureGeometry.Box(exterior, null, "MeterHousing", new(hx + 0.29f, 1.74f, hz - 1.15f),
            new(0.22f, 0.62f, 0.52f), steel);

        // Porch furniture stays away from the centre entrance and its approach.
        foreach (float x in new[] { -hx + 2.65f, hx - 2.65f })
        {
            StructureGeometry.Box(exterior, body, "PorchBenchLegA", new(x - 0.75f, 0.36f, hz + 1.98f),
                new(0.13f, 0.71f, 0.45f), carved);
            StructureGeometry.Box(exterior, body, "PorchBenchLegB", new(x + 0.75f, 0.36f, hz + 1.98f),
                new(0.13f, 0.71f, 0.45f), carved);
            // Seat faces OUT toward +Z / the road; back is on the lodge side.
            // Both seat and back now have the same static collision as the legs.
            StructureGeometry.Box(exterior, body, "PorchBenchSeat", new(x, 0.76f, hz + 1.98f),
                new(1.80f, 0.14f, 0.54f), pale);
            StructureGeometry.Box(exterior, body, "PorchBenchBack", new(x, 1.12f, hz + 1.74f),
                new(1.80f, 0.68f, 0.14f), carved);
        }
        foreach (float x in new[] { -hx + 0.65f, hx - 0.65f })
        {
            StructureGeometry.Cylinder(exterior, "TerracottaPot", new(x, 0.26f, hz + 1.20f),
                0.28f, 0.46f, clay, 8);
            StructureGeometry.Cylinder(exterior, "PotSoil", new(x, 0.51f, hz + 1.20f),
                0.245f, 0.05f, carved, 8);
            for (int i = 0; i < 5; i++)
                StructureGeometry.Box(exterior, null, "PlantLeaves",
                    new(x + Mathf.Sin(i * 1.25f) * 0.16f, 0.72f + i * 0.07f,
                        hz + 1.2f + Mathf.Cos(i * 1.25f) * 0.16f),
                    new(0.09f, 0.40f, 0.15f), i % 2 == 0 ? moss : leaves,
                    Mathf.Sin(i * 1.25f) * 0.45f);
        }
        StructureGeometry.Box(exterior, null, "Doormat", new(0, 0.137f, hz + 0.64f),
            new(1.38f, 0.018f, 0.65f), cloth);
        StructureGeometry.Box(exterior, null, "PorchCrate", new(-hx + 0.65f, 0.37f, hz + 2.10f),
            new(0.65f, 0.68f, 0.65f), carved);
        for (int i = 0; i < 2; i++)
            StructureGeometry.Box(exterior, null, "CrateBand", new(-hx + 0.65f, 0.27f + i * 0.28f, hz + 2.45f),
                new(0.65f, 0.08f, 0.035f), pale);

        // INTERIOR: posters, map, shelving details, reception papers, radio and a hanging wall clock.
        // Keep pieces slim near walls; no extra collision for paper, decor or small possessions.
        // Hang the map FLUSH against the solid front-wall bay X=-3..-1.
        // The previous 2.3m map overlapped the X=-1..1 entry and looked like a floating painting.
        const float mapX = -2.0f;
        StructureGeometry.Box(interior, null, "LargeRangerMap", new(mapX, 2.14f, hz - 0.15f),
            new(1.70f, 1.28f, 0.08f), carved);
        StructureGeometry.Box(interior, null, "MapSurface", new(mapX, 2.14f, hz - 0.208f),
            new(1.52f, 1.03f, 0.035f), paper);
        for (int i = 0; i < 4; i++)
            StructureGeometry.Box(interior, null, "MapContours",
                new(mapX - 0.56f + i * 0.36f, 2.18f, hz - 0.236f),
                new(0.065f, 0.70f - (i % 2) * 0.18f, 0.018f), moss);

        for (int i = 0; i < 3; i++)
        {
            StructureGeometry.Box(interior, null, "DeskPaper", new(receptionX - 0.45f + i * 0.42f, 1.41f,
                receptionZ), new(0.31f, 0.018f, 0.42f), paper);
            StructureGeometry.Box(interior, null, "Pencil", new(receptionX - 0.4f + i * 0.30f, 1.44f,
                receptionZ - 0.30f), new(0.035f, 0.022f, 0.29f), yellow);
        }
        StructureGeometry.Box(interior, null, "DeskRadio", new(receptionX + 0.87f, 1.54f, receptionZ),
            new(0.43f, 0.35f, 0.30f), steel);
        StructureGeometry.Box(interior, null, "RadioFace", new(receptionX + 0.87f, 1.55f, receptionZ + 0.165f),
            new(0.29f, 0.17f, 0.02f), ink);
        StructureGeometry.Box(interior, null, "RadioAntenna", new(receptionX + 0.75f, 1.91f, receptionZ - 0.02f),
            new(0.02f, 0.53f, 0.02f), steel);

        // Wall shelves inside the small rear office; contents remain cosmetic.
        for (int i = 0; i < 3; i++)
        {
            float y = 1.10f + i * 0.48f;
            StructureGeometry.Box(interior, null, "OfficeWallShelf", new(-hx + 2.18f, y, -hz + 0.24f),
                new(2.25f, 0.09f, 0.42f), pale);
            for (int j = 0; j < 5; j++)
                StructureGeometry.Box(interior, null, "FieldManuals", new(-hx + 1.34f + j * 0.33f, y + 0.19f, -hz + 0.24f),
                    new(0.18f, 0.33f, 0.26f), j % 2 == 0 ? cloth : carved);
        }
        // This was a NOTICEBOARD overlapping the rear bookshelves, not an
        // actual window. Relocate it onto the left solid wall above furniture.
        StructureGeometry.Box(interior, null, "OfficeNotice",
            new(-hx + 0.165f, 2.88f, -hz + 1.75f),
            new(0.055f, 0.70f, 1.10f), paper);
        StructureGeometry.Box(interior, null, "CabinetDoorDetail", new(cabinetX, 1.16f, cabinetZ + 0.45f),
            new(1.23f, 1.65f, 0.05f), carved);
        StructureGeometry.Box(interior, null, "BunkBlanket",
            new(bunkX, 1.20f, bunkZ + 0.55f), new(1.55f, 0.08f, 1.68f), cloth);
        StructureGeometry.Box(interior, null, "BunkFoldedBlanket",
            new(bunkX, 1.27f, bunkZ + 1.04f), new(1.27f, 0.09f, 0.50f), pale);
        StructureGeometry.Box(interior, null, "LanternBase",
            new(bunkX + 0.45f, 1.25f, bunkZ + 0.80f), new(0.30f, 0.10f, 0.30f), steel);
        StructureGeometry.Box(interior, null, "LanternGlass",
            new(bunkX + 0.45f, 1.44f, bunkZ + 0.80f), new(0.21f, 0.30f, 0.21f), yellow);
        StructureGeometry.Box(interior, null, "LanternCap",
            new(bunkX + 0.45f, 1.62f, bunkZ + 0.80f), new(0.33f, 0.10f, 0.33f), steel);
        StructureGeometry.Box(interior, null, "WallClockBacking", new(1.65f, 2.40f, hz - 0.17f),
            new(0.68f, 0.68f, 0.09f), carved);
        StructureGeometry.Box(interior, null, "WallClockFace", new(1.65f, 2.40f, hz - 0.23f),
            new(0.54f, 0.54f, 0.02f), paper);
        StructureGeometry.Box(interior, null, "ClockHandVertical", new(1.65f, 2.48f, hz - 0.255f),
            new(0.028f, 0.23f, 0.015f), ink);
        StructureGeometry.Box(interior, null, "ClockHandHorizontal", new(1.73f, 2.40f, hz - 0.255f),
            new(0.20f, 0.028f, 0.015f), ink);
    }

    // =========================================================
    // Separate solid side-wall spans and physical panes; glass isn't a deceptive opaque wall texture.
    private static void SideWindow(Node3D shell, Node3D detail, StaticBody3D body,
        float x, int side, float hz, float height, Material wood, Material light, Material trim, Material glass)
    {
        const float a = -1.8f, b = 1.0f, lo = 1.28f, hi = 2.73f;
        StructureGeometry.WallZ(shell, body, "SideRear", x, -hz, a, 0, height, wood);
        StructureGeometry.WallZ(shell, body, "SideFront", x, b, hz, 0, height, wood);
        StructureGeometry.WallZ(shell, body, "SideSill", x, a, b, 0, lo, wood);
        StructureGeometry.WallZ(shell, body, "SideHeader", x, a, b, hi, height, wood);
        float offset = x + side * 0.14f;
        StructureGeometry.Box(detail, body, "SidePane", new(offset, (lo + hi) * 0.5f, (a + b) * 0.5f),
            new(0.065f, hi - lo, b - a), glass);
        StructureGeometry.Box(detail, null, "WindowHorizontal", new(offset + side * 0.07f, (lo + hi) * 0.5f, (a + b) * 0.5f),
            new(0.12f, 0.11f, b - a), trim);
        StructureGeometry.Box(detail, null, "WindowVertical", new(offset + side * 0.07f, (lo + hi) * 0.5f, (a + b) * 0.5f),
            new(0.12f, hi - lo, 0.11f), light);
    }

    // =========================================================
    // A complete front aperture: no full-height opaque panel or blocker behind the visible pane.
    private static void FrontWindow(Node3D shell, Node3D detail, StaticBody3D body,
        string name, float left, float right, float z, float lo, float hi, float h,
        Material wall, Material glass, Material trim)
    {
        StructureGeometry.WallX(shell, body, name + "Sill", left, right, z, 0, lo, wall);
        StructureGeometry.WallX(shell, body, name + "Header", left, right, z, hi, h, wall);
        StructureGeometry.Box(detail, body, name + "Glass", new((left + right) * 0.5f, (lo + hi) * 0.5f, z + 0.13f),
            new(right - left, hi - lo, 0.06f), glass);
        StructureGeometry.Box(detail, null, name + "Frame", new((left + right) * 0.5f, lo, z + 0.19f),
            new(right - left + 0.18f, 0.13f, 0.16f), trim);
        StructureGeometry.Box(detail, null, name + "Cross", new((left + right) * 0.5f, (lo + hi) * 0.5f, z + 0.19f),
            new(0.10f, hi - lo, 0.12f), trim);
    }
    // =========================================================
    // Same metadata contract used by future authored/procedural buildings; no visibility code knows these paths.
    private void DeclareVisibility()
    {
        var s = Settings ?? new Building002Settings();
        float w = Mathf.Clamp(s.Width, 14, 24), d = Mathf.Clamp(s.Depth, 10, 18);
        float h = Mathf.Clamp(s.WallHeight, 3.4f, 5), upper = Mathf.Clamp(s.UpperWallHeight, 2.5f, 3.4f);
        StructureLayout.Occupied(GetNode<Node3D>("Generated"), new Aabb(new Vector3(-w / 2, -0.5f, -d / 2), new Vector3(w, h + upper + 0.5f, d)));
        foreach (string level in new[] { "Level_0", "Level_1" })
        {
            string path = "Generated/" + level + "/Visuals/";
            foreach (string name in new[] { "Partitions", level == "Level_0" ? "Exterior" : "ExteriorWalls" })
                StructureLayout.Visual(GetNode<Node3D>(path + name), "walls");
        }
        foreach (string name in new[] { "Rooms", "TimberAndProps", "ShowcaseExterior", "ShowcaseInterior" })
            StructureLayout.Visual(GetNode<Node3D>("Generated/Level_0/Visuals/" + name), "contents");
        StructureLayout.Visual(GetNode<Node3D>("Generated/Level_1/Visuals/FloorDeck"), "deck");
        StructureLayout.Visual(GetNode<Node3D>("Generated/Level_1/Visuals/Furnishings"), "contents");
        StructureLayout.Visual(GetNode<Node3D>("Generated/Level_1/Visuals/RoofVisuals"), "roof");
        StructureLayout.Visual(GetNode<Node3D>("Generated/Stairs_0_to_1/Visuals"), "stairs");
    }

}
