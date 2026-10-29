// PS1: two-storey police station with asymmetric ground-floor rooms and front U-shaped stairs.
// One-shot procedural geometry/collision via shared structure helpers; no player/world placement or idle ticks.
using Godot;

[Tool]
public partial class PoliceStationMesh : Node3D
{
    public enum PreviewFloor { GroundFloor, UpperFloor, Exterior }

    [Export] public PoliceStationSettings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get => _showRoof; set { _showRoof = value; ApplyPreview(); } }
    [ExportGroup("Editor-only preview")]
    [Export] public PreviewFloor EditorPreview { get => _editorPreview; set { _editorPreview = value; ApplyPreview(); } }

    // Explicit recovery after a C# hot reload: reopening the scene is no longer the only way to build.
    [ExportToolButton("Build / Refresh Preview")]
    public Callable RefreshPreviewButton => Callable.From(RefreshPreview);

    private const float Width = 48f, Depth = 64f, CorridorHalfWidth = 2.5f;
    // The doorway-to-first-riser landing and side approach are both at least 2m clear.
    private const float CoreWallX = 7f, StairEntryZ = 20.8f;
    private const float LowerFlightX = 11f, UpperFlightX = 15.3f;
    private const float StairSouthZ = 23f, StairNorthZ = 28f;
    private const float ShaftMinX = 8.7f, ShaftMaxX = 18.2f, ShaftMinZ = 18f, ShaftMaxZ = 30.4f;
    private const float MinimumStairApproach = 2f, SideWindowWidth = 3.6f;
    private static readonly float[] SideWindowZ = { -26f, -13f, 0f, 13f, 25f };
    private bool _showRoof = true;
    private PreviewFloor _editorPreview = PreviewFloor.GroundFloor;
    private Node3D? _upperLevel, _roof;

    // =========================================================
    // Godot [Tool] invokes this when the standalone scene opens in the editor.
    // Regenerate transient children even if the editor previously saved an empty Generated anchor.
    public override void _Ready()
    {
        RebuildPreviewGeometry();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Inspector recovery button for C# recompiles that did not rerun _Ready.
    private void RefreshPreview()
    {
        if (Engine.IsEditorHint() && IsInsideTree()) RebuildPreviewGeometry();
    }

    // =========================================================
    // Keep only the lightweight Generated anchor editor-owned (visible in the Scene dock).
    // Its large procedural contents stay unowned, so saving the scene never bakes hundreds
    // of meshes/colliders into the .tscn; they are rebuilt from code each time.
    private void RebuildPreviewGeometry()
    {
        var previous = GetNodeOrNull<Node3D>("Generated");
        if (previous != null)
        {
            RemoveChild(previous);
            previous.QueueFree();
        }
        _upperLevel = null; _roof = null;
        Build();
        var generated = GetNode<Node3D>("Generated");
        if (Engine.IsEditorHint() && GetTree().EditedSceneRoot == this)
            generated.Owner = this;
        ApplyPreview();
        if (Engine.IsEditorHint())
            GD.Print("[PoliceStation001] Editor shell generated. Choose GroundFloor / UpperFloor / Exterior, or Build / Refresh Preview.");
    }

    // =========================================================
    // Scene preview is independent of the runtime roof/floor visibility system and collision.
    private void ApplyPreview()
    {
        if (!GodotObject.IsInstanceValid(_upperLevel) || !GodotObject.IsInstanceValid(_roof)) return;
        if (Engine.IsEditorHint())
        {
            _upperLevel!.Visible = EditorPreview != PreviewFloor.GroundFloor;
            _roof!.Visible = EditorPreview == PreviewFloor.Exterior;
        }
        else
        {
            _upperLevel!.Visible = true;
            _roof!.Visible = ShowRoof;
        }
    }

    // =========================================================
    // The building root is centred on grade. Front entrance points toward local +Z.
    private void Build()
    {
        ValidateStairApproach(); // A malformed shell must fail before generating blocked stairs.
        var s = Settings ?? new PoliceStationSettings();
        float h0 = Mathf.Clamp(s.GroundFloorHeight, 3.8f, 5.2f);
        float h1 = Mathf.Clamp(s.UpperFloorHeight, 3.2f, 4.8f), top = h0 + h1;
        var shell = StructureGeometry.Paint(s.Exterior);
        var partitions = StructureGeometry.Paint(s.Partitions);
        var trim = StructureGeometry.Paint(s.Frames, 0.10f);
        var glass = StructureGeometry.Paint(s.WindowGlass, 0.15f);
        var floorMat = StructureGeometry.Paint(s.Concrete);
        var roofMat = StructureGeometry.Paint(s.Roof, 0.08f);
        var doors = StructureGeometry.Paint(s.DoorPanels);
        var step = StructureGeometry.Paint(s.StairTreads);
        var riser = StructureGeometry.Paint(s.StairRisers);

        var generated = StructureGeometry.Group(this, "Generated");
        StructureLayout.Occupied(generated, new Aabb(new(-Width * 0.5f, -0.4f, -Depth * 0.5f),
            new(Width, top + 1.0f, Depth)));
        var ground = StructureGeometry.Group(generated, "Level_0");
        StructureLayout.Level(ground, 0, 0f);
        _upperLevel = StructureGeometry.Group(generated, "Level_1");
        StructureLayout.Level(_upperLevel, 1, h0);
        var groundBody = StructureGeometry.Body(ground);
        var upperBody = StructureGeometry.Body(_upperLevel);
        var clear = new StructureClearance(generated);

        var groundDeck = Role(ground, "GroundFloorSlab", "deck");
        var groundWalls = Role(ground, "GroundFloorWalls", "walls");
        var upperDeck = Role(_upperLevel, "UpperFloorDeck", "deck");
        var upperWalls = Role(_upperLevel, "UpperFloorWalls", "walls");
        // Upper guardrails disappear with the upper floor in the GroundFloor editor view.
        var upperGuards = Role(_upperLevel, "StairwellGuardrails", "contents");
        _roof = Role(_upperLevel, "FlatRoof", "roof");

        // Ground slab ends at Y=0 for easy placement on existing prepared terrain.
        // Its real collision can be disabled via UseTerrainSupport if graded Terrain3D supports the whole floor.
        StructureGeometry.Box(groundDeck, s.UseTerrainSupport ? null : groundBody, "GroundSlab",
            new(0, -0.10f, 0), new(Width - 0.24f, 0.20f, Depth - 0.24f), floorMat);

        BuildExterior(groundWalls, groundBody, clear, "G", 0f, h0, true, shell, trim, glass, doors);
        BuildGroundRooms(groundWalls, groundBody, clear, h0, partitions, trim, doors);

        // The four upper-floor spans leave a real, non-colliding void above the new front-right stair core.
        const float deckThickness = 0.24f, inset = 0.18f;
        float slabY = h0 - deckThickness * 0.5f;
        FloorSpan(upperDeck, upperBody, "WestDeck", -Width * 0.5f + inset, ShaftMinX,
            -Depth * 0.5f + inset, Depth * 0.5f - inset, slabY, deckThickness, floorMat);
        FloorSpan(upperDeck, upperBody, "EastDeck", ShaftMaxX, Width * 0.5f - inset,
            -Depth * 0.5f + inset, Depth * 0.5f - inset, slabY, deckThickness, floorMat);
        FloorSpan(upperDeck, upperBody, "ShaftSouthDeck", ShaftMinX, ShaftMaxX,
            -Depth * 0.5f + inset, ShaftMinZ, slabY, deckThickness, floorMat);
        FloorSpan(upperDeck, upperBody, "ShaftNorthDeck", ShaftMinX, ShaftMaxX,
            ShaftMaxZ, Depth * 0.5f - inset, slabY, deckThickness, floorMat);

        BuildExterior(upperWalls, upperBody, clear, "U", h0, top, false, shell, trim, glass, doors);
        BuildUpperRooms(upperWalls, upperBody, clear, h0, top, partitions, trim, doors);
        BuildStairs(generated, upperBody, upperGuards, clear, h0, step, riser, trim);

        // Roof is one inexpensive solid deck with a shallow parapet, kept separate for top-down reveal.
        StructureGeometry.Box(_roof, upperBody, "RoofSlab", new(0, top + 0.13f, 0),
            new(Width + 0.5f, 0.26f, Depth + 0.5f), roofMat);
        foreach (float z in new[] { -Depth * 0.5f, Depth * 0.5f })
            StructureGeometry.Box(_roof, null, "RoofParapetFrontRear", new(0, top + 0.53f, z),
                new(Width + 0.5f, 0.8f, 0.30f), trim);
        foreach (float x in new[] { -Width * 0.5f, Width * 0.5f })
            StructureGeometry.Box(_roof, null, "RoofParapetSides", new(x, top + 0.53f, 0),
                new(0.30f, 0.8f, Depth + 0.5f), trim);

        var markers = StructureGeometry.Group(generated, "Markers");
        Marker(markers, "FrontEntrance", new(0, 0, 32.6f));
        Marker(markers, "PublicLobby", new(0, 0, 25f));
        Marker(markers, "GroundMainCorridor", new(0, 0, -5f));
        Marker(markers, "RearServiceExit", new(0, 0, -32.6f));
        Marker(markers, "StairFoot", new(LowerFlightX, 0, StairSouthZ));
        Marker(markers, "StairApproach", new(LowerFlightX, 0f, StairSouthZ - MinimumStairApproach));
        Marker(markers, "StairTurnLanding", new((LowerFlightX + UpperFlightX) * 0.5f, h0 * 0.5f, 29.2f));
        Marker(markers, "StairUpperLanding", new(UpperFlightX, h0, ShaftMinZ + 0.8f));
        Marker(markers, "UpperOpenPlanOffice", new(14f, h0, 14f));
        Marker(markers, "UpperBathroom", new(6f, h0, -27f));
    }

    // =========================================================
    // Group each complete straight exterior wall independently, including headers/frames/panes.
    private static void BuildExterior(Node3D walls, StaticBody3D body, StructureClearance clear,
        string prefix, float baseY, float top, bool ground, Material shell, Material trim,
        Material glass, Material door)
    {
        foreach (float z in new[] { -Depth * 0.5f, Depth * 0.5f })
        {
            string side = z > 0 ? "Front" : "Rear";
            var group = WallGroup(walls, prefix + side + "Wall");
            float cursor = -Width * 0.5f;
            foreach (float x in new[] { -18f, -11f, 0f, 11f, 18f })
            {
                bool entrance = ground && x == 0f;
                float width = entrance ? (z > 0 ? 4.4f : 2.6f) : 3.4f;
                float left = x - width * 0.5f, right = x + width * 0.5f;
                StructureGeometry.WallX(group, body, prefix + side + "Span", cursor, left,
                    z, baseY, top, shell);
                if (entrance)
                {
                    float head = baseY + 3.0f;
                    StructureGeometry.WallX(group, body, prefix + side + "DoorHeader",
                        left, right, z, head, top, shell);
                    DoorFrameX(group, prefix + side + "Entrance", x, z, baseY, width, head, trim);
                    // First-shell *visible open leaves*, intentionally noncolliding and not yet animated.
                    OpenDoorLeafX(group, prefix + side + "OpenLeafL", left, z, baseY, width > 4f ? 1.98f : 2.25f, z > 0, door);
                    if (width > 4f)
                        OpenDoorLeafX(group, prefix + side + "OpenLeafR", right, z, baseY, 1.98f, z > 0, door);
                    clear.Reserve(prefix + side + "Entrance", new(x, baseY + 1.45f, z),
                        new(width + 0.50f, 2.9f, 3.7f));
                }
                else
                    WindowX(group, body, clear, prefix + side + "Window", left, right, z,
                        baseY, top, shell, trim, glass);
                cursor = right;
            }
            StructureGeometry.WallX(group, body, prefix + side + "End", cursor,
                Width * 0.5f, z, baseY, top, shell);
        }
        foreach (float x in new[] { -Width * 0.5f, Width * 0.5f })
        {
            string side = x < 0 ? "West" : "East";
            var group = WallGroup(walls, prefix + side + "Wall");
            float cursor = -Depth * 0.5f;
            foreach (float z in SideWindowZ)
            {
                const float width = 3.6f;
                float a = z - width * 0.5f, b = z + width * 0.5f;
                StructureGeometry.WallZ(group, body, prefix + side + "Span", x,
                    cursor, a, baseY, top, shell);
                WindowZ(group, body, clear, prefix + side + "Window", x, a, b, baseY, top,
                    shell, trim, glass);
                cursor = b;
            }
            StructureGeometry.WallZ(group, body, prefix + side + "End", x,
                cursor, Depth * 0.5f, baseY, top, shell);
        }
    }

    // =========================================================
    // One major public lobby leads into a 5m-wide corridor; side rooms have true door gaps.
    private static void BuildGroundRooms(Node3D walls, StaticBody3D body, StructureClearance clear,
        float height, Material partition, Material trim, Material doors)
    {
        // Generous front public lobby feeds the long main hall, smaller waiting room and new stairs.
        var lobby = WallGroup(walls, "PublicLobbyRearWall");
        DoorPartitionX(lobby, body, clear, "LobbyToHall", -24f, 24f, 19f, 0f, height,
            0f, 4.4f, partition, trim, doors);

        // The waiting/intake room has its own front-left enclosure and wide accessible door.
        var waiting = WallGroup(walls, "WaitingRoomWall");
        const float wx = -7.4f; // On solid frontage between the -11m window and main doorway.
        StructureGeometry.WallZ(waiting, body, "WaitingRear", wx, 19.2f, 23.6f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(waiting, body, "WaitingFront", wx, 26.4f, 32f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(waiting, body, "WaitingHeader", wx, 23.6f, 26.4f, 2.85f, height, partition, 0.18f);
        DoorFrameZ(waiting, "WaitingEntry", wx, 25f, 0f, 2.8f, 2.85f, trim);
        clear.Reserve("WaitingEntry", new(wx, 1.45f, 25f), new(3.6f, 2.9f, 3.8f));

        // Stair entrance is positioned BESIDE a level vestibule, not alongside steps/rails.
        // Enter through the west wall, turn, then travel 2.2m on a flat apron before step one.
        var core = WallGroup(walls, "LobbyStairCore");
        const float doorHalfZ = 1.4f;
        float doorStart = StairEntryZ - doorHalfZ, doorEnd = StairEntryZ + doorHalfZ;
        StructureGeometry.WallZ(core, body, "CoreRear", CoreWallX, 19f, doorStart,
            0f, height, partition, 0.18f);
        StructureGeometry.WallZ(core, body, "CoreFront", CoreWallX, doorEnd, 32f,
            0f, height, partition, 0.18f);
        StructureGeometry.WallZ(core, body, "CoreHead", CoreWallX, doorStart, doorEnd,
            2.9f, height, partition, 0.18f);
        DoorFrameZ(core, "StairCoreEntry", CoreWallX, StairEntryZ, 0f,
            doorHalfZ * 2f, 2.9f, trim);
        clear.Reserve("StairCoreEntry", new(CoreWallX, 1.45f, StairEntryZ),
            new(4.4f, 2.9f, 4f));

        // Non-repeating door spacings give each functional zone a different length.
        CorridorSide(walls, body, clear, "GroundWestHall", -CorridorHalfWidth, 0f, height,
            new[] { -27f, -18f, -5f, 12f }, partition, trim, doors);
        CorridorSide(walls, body, clear, "GroundEastHall", CorridorHalfWidth, 0f, height,
            new[] { -26f, -7f, 10.5f }, partition, trim, doors);

        // Larger west front admin zone, split central interview pair and varied rear service rooms.
        foreach (float z in new[] { -23.5f, -10.4f, 4f })
        {
            var wall = WallGroup(walls, "WestRoomBoundary");
            InteriorPerimeterPartitionX(wall, body, "WestDivider", -24f, -CorridorHalfWidth,
                z, 0, height, partition);
        }
        var interview = WallGroup(walls, "WestInterviewPartition");
        StructureGeometry.WallZ(interview, body, "InterviewRear", -12f, -10.8f, -4.4f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(interview, body, "InterviewFront", -12f, -1.6f, 3.8f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(interview, body, "InterviewHeader", -12f, -4.4f, -1.6f, 2.85f, height, partition, 0.18f);
        DoorFrameZ(interview, "InterviewEntry", -12f, -3f, 0f, 2.8f, 2.85f, trim);
        clear.Reserve("InterviewEntry", new(-12f, 1.45f, -3f), new(3.6f, 2.9f, 3.6f));

        // Larger east-front operations area, a connected records/interview pair, and broad rear service.
        foreach (float z in new[] { -18f, -4f })
        {
            var wall = WallGroup(walls, "EastRoomBoundary");
            InteriorPerimeterPartitionX(wall, body, "EastDivider", CorridorHalfWidth, 24f,
                z, 0, height, partition);
        }
        var records = WallGroup(walls, "EastRecordsPartition");
        StructureGeometry.WallZ(records, body, "RecordsRear", 13f, -17.8f, -9.5f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(records, body, "RecordsFront", 13f, -6.7f, -1.2f, 0, height, partition, 0.18f);
        StructureGeometry.WallZ(records, body, "RecordsHeader", 13f, -9.5f, -6.7f, 2.85f, height, partition, 0.18f);
        DoorFrameZ(records, "RecordsEntry", 13f, -8.1f, 0f, 2.8f, 2.85f, trim);
        clear.Reserve("RecordsEntry", new(13f, 1.45f, -8.1f), new(3.6f, 2.9f, 3.6f));

        clear.Reserve("GroundMainCorridor", new(0, 1.4f, -6f), new(4.75f, 2.8f, 50f));
        clear.Reserve("PublicLobbyRoute", new(-1f, 1.4f, 25.5f), new(11f, 2.8f, 12f));
        clear.Reserve("StairLobbyApproach", new(8.5f, 1.4f, 21.2f),
            new(5.8f, 2.8f, 3.6f));
        clear.Reserve("StairBottomLanding", new(LowerFlightX, 1.4f,
            StairSouthZ - MinimumStairApproach * 0.5f),
            new(4.0f, 2.8f, MinimumStairApproach));
    }

    // =========================================================
    // West wing becomes enclosed offices; east/front remains a single large open-plan office.
    // The east-rear end contains a separately enclosed future bathroom (empty in PS0).
    private static void BuildUpperRooms(Node3D walls, StaticBody3D body, StructureClearance clear,
        float baseY, float top, Material partition, Material trim, Material doors)
    {
        CorridorSide(walls, body, clear, "UpperWestHall", -CorridorHalfWidth, baseY, top,
            new[] { -25f, -12f, 0f, 12f }, partition, trim, doors);
        CorridorSide(walls, body, clear, "UpperEastHall", CorridorHalfWidth, baseY, top,
            new[] { -26.5f, -15f, 12f }, partition, trim, doors);

        foreach (float z in new[] { -8f, 8f, 20f })
        {
            var west = WallGroup(walls, "UpperWestOfficeDivider");
            InteriorPerimeterPartitionX(west, body, "OfficeDivision", -24f, -CorridorHalfWidth,
                z, baseY, top, partition);
        }
        var east = WallGroup(walls, "UpperEastOfficeDivider");
        // Large open office above the lobby, separate from the rear staff rooms.
        DoorPartitionX(east, body, clear, "OpenPlanToRear", CorridorHalfWidth, 24f,
            -9f, baseY, top, 13f, 3.0f, partition, trim, doors);

        // Empty 6.5 x 7m bathroom enclosure with corridor access at Z=-26.5.
        var bathroomWest = WallGroup(walls, "UpperBathroomEastWall");
        StructureGeometry.WallZ(bathroomWest, body, "BathroomSide", 9f,
            -31f, -23f, baseY, top, partition, 0.18f);
        var bathroomNorth = WallGroup(walls, "UpperBathroomNorthWall");
        StructureGeometry.WallX(bathroomNorth, body, "BathroomBack", CorridorHalfWidth, 9f,
            -23f, baseY, top, partition, 0.18f);

        clear.Reserve("UpperMainCorridor", new(0, baseY + 1.4f, -6f),
            new(4.75f, 2.8f, 50f));
        clear.Reserve("UpperStairLanding", new(UpperFlightX, baseY + 1.25f, 19.4f),
            new(5.0f, 2.5f, 4.8f));
    }

    // =========================================================
    // Corridor wall is one logical fade group but retains independent collision around every door.
    private static void CorridorSide(Node3D walls, StaticBody3D body, StructureClearance clear,
        string name, float x, float bottom, float top, float[] doors,
        Material partition, Material trim, Material doorMat)
    {
        var group = WallGroup(walls, name);
        const float half = 1.15f, head = 2.72f;
        float cursor = -31.5f;
        foreach (float z in doors)
        {
            float a = z - half, b = z + half;
            StructureGeometry.WallZ(group, body, name + "Span", x, cursor, a,
                bottom, top, partition, 0.18f);
            StructureGeometry.WallZ(group, body, name + "DoorHead", x, a, b,
                bottom + head, top, partition, 0.18f);
            DoorFrameZ(group, name + "OfficeDoor", x, z, bottom, half * 2f, bottom + head, trim);
            // Door leaves are parked open along the wall; collision/hinge interactions are later work.
            StructureGeometry.Box(group, null, name + "OpenDoorLeaf",
                new(x > 0 ? x + 0.9f : x - 0.9f, bottom + 1.33f, a),
                new(1.78f, 2.62f, 0.075f), doorMat);
            clear.Reserve(name + "Door", new(x, bottom + 1.45f, z), new(3.2f, 2.9f, 3.3f));
            cursor = b;
        }
        StructureGeometry.WallZ(group, body, name + "EndSpan", x, cursor, 19f,
            bottom, top, partition, 0.18f);
    }

    // =========================================================
    // Cut a horizontal partition around one genuine doorway and protect the full volume.
    private static void DoorPartitionX(Node3D group, StaticBody3D body, StructureClearance clear,
        string name, float start, float end, float z, float bottom, float top,
        float center, float width, Material wall, Material trim, Material doors)
    {
        float left = center - width * 0.5f, right = center + width * 0.5f, head = bottom + 2.90f;
        EnsureClearPerimeterWindowJunction(start, end, z, name);
        StructureGeometry.WallX(group, body, name + "Left", start, left, z, bottom, top, wall, 0.18f);
        StructureGeometry.WallX(group, body, name + "Header", left, right, z, head, top, wall, 0.18f);
        StructureGeometry.WallX(group, body, name + "Right", right, end, z, bottom, top, wall, 0.18f);
        DoorFrameX(group, name, center, z, bottom, width, head, trim);
        clear.Reserve(name, new(center, bottom + 1.45f, z), new(width + 0.4f, 2.9f, 3.6f));
    }

    // =========================================================
    // A true hole in an X-facing facade: separate sill, header and glass with paired collision.
    private static void WindowX(Node3D group, StaticBody3D body, StructureClearance clear,
        string name, float a, float b, float z, float bottom, float top,
        Material wall, Material trim, Material glass)
    {
        float sill = bottom + 0.92f, head = bottom + 2.85f, mid = (a + b) * 0.5f;
        StructureGeometry.WallX(group, body, name + "Sill", a, b, z, bottom, sill, wall);
        StructureGeometry.WallX(group, body, name + "Header", a, b, z, head, top, wall);
        StructureGeometry.Box(group, body, name + "Glass",
            new(mid, (sill + head) * 0.5f, z), new(b - a - 0.08f, head - sill - 0.08f, 0.06f), glass);
        StructureGeometry.Box(group, null, name + "SillTrim", new(mid, sill, z + 0.15f),
            new(b - a, 0.09f, 0.15f), trim);
        foreach (float x in new[] { a, b })
            StructureGeometry.Box(group, null, name + "Jamb", new(x, (sill + head) * 0.5f, z),
                new(0.10f, head - sill, 0.16f), trim);
        clear.Reserve(name, new(mid, (sill + head) * 0.5f, z),
            new(b - a - 0.10f, head - sill - 0.10f, 1.1f));
    }

    // =========================================================
    // A true hole in a Z-facing facade with matching window glazing and clearance.
    private static void WindowZ(Node3D group, StaticBody3D body, StructureClearance clear,
        string name, float x, float a, float b, float bottom, float top,
        Material wall, Material trim, Material glass)
    {
        float sill = bottom + 0.92f, head = bottom + 2.85f, mid = (a + b) * 0.5f;
        StructureGeometry.WallZ(group, body, name + "Sill", x, a, b, bottom, sill, wall);
        StructureGeometry.WallZ(group, body, name + "Header", x, a, b, head, top, wall);
        StructureGeometry.Box(group, body, name + "Glass", new(x, (sill + head) * 0.5f, mid),
            new(0.06f, head - sill - 0.08f, b - a - 0.08f), glass);
        StructureGeometry.Box(group, null, name + "SillTrim", new(x, sill, mid),
            new(0.15f, 0.09f, b - a), trim);
        foreach (float z in new[] { a, b })
            StructureGeometry.Box(group, null, name + "Jamb", new(x, (sill + head) * 0.5f, z),
                new(0.16f, head - sill, 0.10f), trim);
        clear.Reserve(name, new(x, (sill + head) * 0.5f, mid),
            new(1.1f, head - sill - 0.10f, b - a - 0.10f));
    }

    // =========================================================
    // A door frame is visual only; its structural hole is physically open.
    private static void DoorFrameX(Node3D group, string name, float x, float z,
        float bottom, float width, float head, Material trim)
    {
        foreach (float edge in new[] { x - width * 0.5f, x + width * 0.5f })
            StructureGeometry.Box(group, null, name + "Jamb",
                new(edge, (bottom + head) * 0.5f, z), new(0.11f, head - bottom, 0.20f), trim);
        StructureGeometry.Box(group, null, name + "Top",
            new(x, head + 0.06f, z), new(width + 0.15f, 0.11f, 0.22f), trim);
    }

    // =========================================================
    // Corridor doors face along the X axis and have Z-axis jamb separation.
    private static void DoorFrameZ(Node3D group, string name, float x, float z,
        float bottom, float width, float head, Material trim)
    {
        foreach (float edge in new[] { z - width * 0.5f, z + width * 0.5f })
            StructureGeometry.Box(group, null, name + "Jamb",
                new(x, (bottom + head) * 0.5f, edge), new(0.20f, head - bottom, 0.11f), trim);
        StructureGeometry.Box(group, null, name + "Top",
            new(x, head + 0.06f, z), new(0.22f, 0.11f, width + 0.15f), trim);
    }

    // =========================================================
    // Park an exterior entrance leaf 90 degrees open, well outside the clear aperture.
    // No leaf collider/hinge in PS0; this is a deliberate static editor-visible placeholder.
    private static void OpenDoorLeafX(Node3D group, string name, float hingeX, float z,
        float bottom, float leafWidth, bool front, Material doorMat)
    {
        float zDir = front ? -1f : 1f;
        StructureGeometry.Box(group, null, name,
            new(hingeX, bottom + 1.34f, z + zDir * leafWidth * 0.5f),
            new(0.075f, 2.68f, leafWidth), doorMat);
    }

    // =========================================================
    // A real upper-floor opening plus a smooth sloped collider; individual visible risers do not
    // obstruct CharacterBody3D. The rising flight heads toward +Z, so pitch is negative.
    private static void BuildStairs(Node3D generated, StaticBody3D upperBody, Node3D upperGuards, StructureClearance clear,
        float rise, Material tread, Material riser, Material rail)
    {
        var stairs = StructureGeometry.Group(generated, "Stairs_0_to_1");
        stairs.SetMeta("connects_floor_from", 0); stairs.SetMeta("connects_floor_to", 1);
        stairs.SetMeta("lower_elevation", 0f); stairs.SetMeta("upper_elevation", rise);
        stairs.AddToGroup("building_stairs");
        var visuals = Role(stairs, "StairVisuals", "stairs");
        var body = StructureGeometry.Body(stairs);

        // Twelve steps, a full 2.4m U-turn landing, then twelve steps back toward -Z.
        float halfRise = rise * 0.5f;
        StairFlight(visuals, body, "Lower", LowerFlightX, StairSouthZ, StairNorthZ,
            0f, halfRise, tread, riser, rail);
        StructureGeometry.Box(visuals, body, "TurnLanding",
            new((LowerFlightX + UpperFlightX) * 0.5f, halfRise - 0.11f,
                (StairNorthZ + ShaftMaxZ) * 0.5f),
            new(8.6f, 0.22f, ShaftMaxZ - StairNorthZ), tread);
        StairFlight(visuals, body, "Upper", UpperFlightX, StairNorthZ, StairSouthZ,
            halfRise, halfRise, tread, riser, rail);

        // Top landing joins the permanently solid second floor at the shaft's south edge.
        StructureGeometry.Box(visuals, body, "UpperLanding",
            new(UpperFlightX, rise - 0.11f, (ShaftMinZ + StairSouthZ + 0.2f) * 0.5f),
            new(4f, 0.22f, StairSouthZ + 0.2f - ShaftMinZ), tread);
        foreach (float x in new[] { ShaftMinX, ShaftMaxX })
            StructureGeometry.Box(upperGuards, upperBody, "UpperShaftRail",
                new(x, rise + 0.54f, (StairSouthZ + ShaftMaxZ) * 0.5f),
                new(0.12f, 1.08f, ShaftMaxZ - StairSouthZ), rail);
        StructureGeometry.Box(upperGuards, upperBody, "UpperNorthRail",
            new((ShaftMinX + ShaftMaxX) * 0.5f, rise + 0.54f, ShaftMaxZ),
            new(ShaftMaxX - ShaftMinX, 1.08f, 0.12f), rail);
        clear.Reserve("FrontUStairwell",
            new((ShaftMinX + ShaftMaxX) * 0.5f, rise * 0.5f + 0.45f, (ShaftMinZ + ShaftMaxZ) * 0.5f),
            new(ShaftMaxX - ShaftMinX, rise + 1.7f, ShaftMaxZ - ShaftMinZ));
        clear.Reserve("UpperStairExit", new(UpperFlightX, rise + 1.2f, 19.4f),
            new(4.8f, 2.4f, 4.8f));
    }

    // =========================================================
    // One cheap smooth slope collider per flight; each of twelve visual treads/risers is noncolliding.
    private static void StairFlight(Node3D visuals, StaticBody3D body, string name,
        float x, float startZ, float endZ, float baseY, float rise,
        Material tread, Material riser, Material rail)
    {
        const int count = 12;
        const float width = 3.2f;
        float run = Mathf.Abs(endZ - startZ), depth = run / count, dy = rise / count;
        bool north = endZ > startZ;
        for (int i = 0; i < count; i++)
        {
            float z = Mathf.Lerp(startZ, endZ, (i + 0.5f) / count);
            float y = baseY + (i + 1) * dy;
            StructureGeometry.Box(visuals, null, name + "Tread",
                new(x, y - 0.035f, z), new(width, 0.07f, depth - 0.015f), tread);
            StructureGeometry.Box(visuals, null, name + "Riser",
                new(x, y - dy * 0.5f, z + (north ? 1f : -1f) * depth * 0.45f),
                new(width, dy, 0.065f), riser);
        }
        float pitch = (north ? -1f : 1f) * Mathf.Atan2(rise, run);
        float length = Mathf.Sqrt(run * run + rise * rise);
        body.AddChild(new CollisionShape3D { Name = name + "WalkableRamp",
            Position = new(x, baseY + rise * 0.5f - 0.025f, (startZ + endZ) * 0.5f),
            Rotation = new(pitch, 0f, 0f), Shape = new BoxShape3D { Size = new(width, 0.14f, length) } });
        foreach (float edge in new[] { x - 1.76f, x + 1.76f })
        {
            StructureGeometry.Box(visuals, body, name + "Rail",
                new(edge, baseY + rise * 0.5f + 0.90f, (startZ + endZ) * 0.5f),
                new(0.10f, 0.11f, length), rail, pitch);
            for (int i = 0; i <= 5; i++)
            {
                float t = i / 5f;
                StructureGeometry.Box(visuals, null, name + "Baluster",
                    new(edge, baseY + rise * t + 0.43f, Mathf.Lerp(startZ, endZ, t)),
                    new(0.10f, 0.85f, 0.10f), rail);
            }
        }
    }

    // =========================================================
    // Structural junction validator: an internal partition must meet solid wall, not window glass.
    // Check real window spans plus half a metre of pier margin; fail on bad authoring geometry.
    private static void EnsureClearPerimeterWindowJunction(float a, float b, float z, string name)
    {
        if (a > -Width * 0.5f + 0.25f && b < Width * 0.5f - 0.25f) return;
        foreach (float windowZ in SideWindowZ)
            if (Mathf.Abs(z - windowZ) < SideWindowWidth * 0.5f + 0.5f)
                throw new System.InvalidOperationException(
                    $"PoliceStation001 {name}: divider at Z={z} hits side window centred on Z={windowZ}. Move the wall to a solid pier (0.5m margin).");
    }

    // =========================================================
    // Required for all full-length interior X partitions on both floors.
    private static void InteriorPerimeterPartitionX(Node3D visual, StaticBody3D body, string name,
        float a, float b, float z, float bottom, float top, Material material)
    {
        EnsureClearPerimeterWindowJunction(a, b, z, name);
        StructureGeometry.WallX(visual, body, name, a, b, z, bottom, top, material, 0.18f);
    }

    // =========================================================
    // A level entry apron must exist in *both directions*: side of the railing and
    // from the doorway centre to the bottom step. This is a structural check, not a
    // furniture-only reservation, and must fail on authoring regression.
    private static void ValidateStairApproach()
    {
        const float lowerRailOffset = 1.76f, lobbyRearWallZ = 19f;
        float sidewaysClear = LowerFlightX - lowerRailOffset - CoreWallX;
        float lengthClear = StairSouthZ - StairEntryZ;
        if (sidewaysClear < MinimumStairApproach ||
            lengthClear < MinimumStairApproach ||
            StairSouthZ - lobbyRearWallZ < MinimumStairApproach + 0.5f ||
            ShaftMinX >= LowerFlightX - lowerRailOffset ||
            ShaftMaxX <= UpperFlightX + lowerRailOffset)
            throw new System.InvalidOperationException(
                $"PoliceStation001: blocked stairs. Minimum 2m flat approach; side={sidewaysClear:0.00}m, front={lengthClear:0.00}m.");
    }

    // =========================================================
    private static void FloorSpan(Node3D visuals, StaticBody3D body, string name,
        float minX, float maxX, float minZ, float maxZ, float y, float thickness, Material material)
    {
        if (minX >= maxX || minZ >= maxZ) return;
        StructureGeometry.Box(visuals, body, name,
            new((minX + maxX) * 0.5f, y, (minZ + maxZ) * 0.5f),
            new(maxX - minX, thickness, maxZ - minZ), material);
    }

    // =========================================================
    private static Node3D Role(Node parent, string name, string role)
    {
        var group = StructureGeometry.Group(parent, name);
        StructureLayout.Visual(group, role);
        return group;
    }

    // =========================================================
    private static Node3D WallGroup(Node3D parent, string name)
    {
        var group = StructureGeometry.Group(parent, name);
        StructureLayout.FadeGroup(group);
        return group;
    }

    // =========================================================
    private static void Marker(Node3D parent, string name, Vector3 position)
        => parent.AddChild(new Marker3D { Name = name, Position = position });
}
