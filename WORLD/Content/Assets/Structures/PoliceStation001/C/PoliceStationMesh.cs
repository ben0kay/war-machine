// PS0: two-storey 48x64m police-station shell with real openings, walkable stairs and editor-only floor preview.
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
    private const float StairX = -13f, StairLowZ = -29.5f, StairHighZ = -19.5f;
    private const float ShaftMinX = -16f, ShaftMaxX = -10f, ShaftMinZ = -30f, ShaftMaxZ = -15.8f;
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
        _roof = Role(_upperLevel, "FlatRoof", "roof");

        // Ground slab ends at Y=0 for easy placement on existing prepared terrain.
        // Its real collision can be disabled via UseTerrainSupport if graded Terrain3D supports the whole floor.
        StructureGeometry.Box(groundDeck, s.UseTerrainSupport ? null : groundBody, "GroundSlab",
            new(0, -0.10f, 0), new(Width - 0.24f, 0.20f, Depth - 0.24f), floorMat);

        BuildExterior(groundWalls, groundBody, clear, "G", 0f, h0, true, shell, trim, glass, doors);
        BuildGroundRooms(groundWalls, groundBody, clear, h0, partitions, trim, doors);

        // Four independent deck spans: the stairwell stays physically open, not a painted hatch.
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
        BuildStairs(generated, upperBody, clear, h0, step, riser, trim);

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
        Marker(markers, "StairFoot", new(StairX, 0, StairLowZ));
        Marker(markers, "StairUpperLanding", new(StairX, h0, ShaftMaxZ + 0.8f));
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
            foreach (float z in new[] { -26f, -13f, 0f, 13f, 25f })
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
        var lobby = WallGroup(walls, "GroundLobbyRearPartition");
        DoorPartitionX(lobby, body, clear, "LobbyToHall", -24f, 24f, 19f, 0f, height,
            0f, 4.4f, partition, trim, doors);
        CorridorSide(walls, body, clear, "GroundWestHall", -CorridorHalfWidth, 0f, height,
            new[] { -25f, -12f, 0f, 12f }, partition, trim, doors);
        CorridorSide(walls, body, clear, "GroundEastHall", CorridorHalfWidth, 0f, height,
            new[] { -25f, -12f, 0f, 12f }, partition, trim, doors);

        foreach (float z in new[] { -19f, -6f, 7f })
        {
            var west = WallGroup(walls, "GroundWestRoomDivider");
            if (z == -19f)
            {
                // The stair flight crosses this partition; keep a full-height gap with no lintel.
                StructureGeometry.WallX(west, body, "StairDividerWest", -24f, ShaftMinX - 0.4f,
                    z, 0, height, partition, 0.18f);
                StructureGeometry.WallX(west, body, "StairDividerEast", ShaftMaxX + 0.4f,
                    -CorridorHalfWidth, z, 0, height, partition, 0.18f);
            }
            else StructureGeometry.WallX(west, body, "WestRoomDivider", -24f, -CorridorHalfWidth,
                z, 0, height, partition, 0.18f);
            var east = WallGroup(walls, "GroundEastRoomDivider");
            StructureGeometry.WallX(east, body, "EastRoomDivider", CorridorHalfWidth, 24f,
                z, 0, height, partition, 0.18f);
        }
        clear.Reserve("GroundMainCorridor", new(0, 1.4f, -6f), new(4.75f, 2.8f, 50f));
        clear.Reserve("GroundPublicLobby", new(0, 1.4f, 25.5f), new(8f, 2.8f, 12f));
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
            StructureGeometry.WallX(west, body, "OfficeDivision", -24f, -CorridorHalfWidth,
                z, baseY, top, partition, 0.18f);
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
        clear.Reserve("UpperStairLanding", new(StairX, baseY + 1.25f, -16.3f),
            new(4.7f, 2.5f, 5.0f));
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
    private static void BuildStairs(Node3D generated, StaticBody3D upperBody, StructureClearance clear,
        float rise, Material tread, Material riser, Material rail)
    {
        var stairs = StructureGeometry.Group(generated, "Stairs_0_to_1");
        stairs.SetMeta("connects_floor_from", 0); stairs.SetMeta("connects_floor_to", 1);
        stairs.SetMeta("lower_elevation", 0f); stairs.SetMeta("upper_elevation", rise);
        stairs.AddToGroup("building_stairs");
        var visuals = Role(stairs, "StairVisuals", "stairs");
        var body = StructureGeometry.Body(stairs);
        const int count = 24;
        float run = StairHighZ - StairLowZ, stepDepth = run / count, stepRise = rise / count;
        const float walkWidth = 3.2f;
        for (int i = 0; i < count; i++)
        {
            float z = StairLowZ + (i + 0.5f) * stepDepth, top = (i + 1) * stepRise;
            StructureGeometry.Box(visuals, null, "StairTread", new(StairX, top - 0.035f, z),
                new(walkWidth, 0.07f, stepDepth - 0.015f), tread);
            StructureGeometry.Box(visuals, null, "StairRiser",
                new(StairX, top - stepRise * 0.5f, z - stepDepth * 0.45f),
                new(walkWidth, stepRise, 0.065f), riser);
        }
        float pitch = -Mathf.Atan2(rise, run), sloped = Mathf.Sqrt(run * run + rise * rise);
        body.AddChild(new CollisionShape3D { Name = "SmoothWalkableRamp",
            Position = new(StairX, rise * 0.5f - 0.025f, (StairHighZ + StairLowZ) * 0.5f),
            Rotation = new(pitch, 0, 0),
            Shape = new BoxShape3D { Size = new(walkWidth, 0.14f, sloped) } });
        // Top landing bridges the remaining floor-deck opening; connection is collision solid.
        StructureGeometry.Box(visuals, body, "UpperLanding", new(StairX, rise - 0.11f,
            (StairHighZ + ShaftMaxZ) * 0.5f),
            new(4.2f, 0.22f, ShaftMaxZ - StairHighZ), tread);
        foreach (float x in new[] { StairX - 1.75f, StairX + 1.75f })
        {
            StructureGeometry.Box(visuals, body, "SlopingHandrail",
                new(x, rise * 0.5f + 0.90f, (StairHighZ + StairLowZ) * 0.5f),
                new(0.10f, 0.11f, sloped), rail, pitch);
            for (int i = 0; i <= 6; i++)
            {
                float u = i / 6f;
                StructureGeometry.Box(visuals, null, "StairPost",
                    new(x, rise * u + 0.43f, StairLowZ + run * u),
                    new(0.10f, 0.85f, 0.10f), rail);
            }
        }
        // Upper guardrails remain collidable, separate from the actual walkable shaft.
        foreach (float x in new[] { ShaftMinX, ShaftMaxX })
            StructureGeometry.Box(visuals, upperBody, "UpperShaftGuard",
                new(x, rise + 0.52f, (StairLowZ + StairHighZ) * 0.5f),
                new(0.12f, 1.04f, run), rail);
        clear.Reserve("Stairwell", new(StairX, rise * 0.5f + 0.4f,
            (ShaftMinZ + ShaftMaxZ) * 0.5f),
            new(ShaftMaxX - ShaftMinX, rise + 1.6f, ShaftMaxZ - ShaftMinZ));
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
