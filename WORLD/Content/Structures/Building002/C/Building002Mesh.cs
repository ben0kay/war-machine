// Building002: walk-in timber ranger lodge with genuine window/door openings and a divided interior.
// C# creates the provisional low-poly art and static collision once; imported replacements belong in Artwork/.
using Godot;

[Tool]
public partial class Building002Mesh : Node3D
{
    [Export] public Building002Settings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;

    // =========================================================
    // Procedural art is built only once at scene load; idle scene has no ticking callbacks.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build();
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
    // Solid outside shell with a real 2m walk-in entrance, windows and two passable interior door gaps.
    private void Build()
    {
        var s = Settings ?? new Building002Settings();
        float w = Mathf.Clamp(s.Width, 14f, 24f), d = Mathf.Clamp(s.Depth, 10f, 18f);
        float h = Mathf.Clamp(s.WallHeight, 3.4f, 5f), rise = Mathf.Clamp(s.RoofRise, 1.4f, 3.5f);
        float hx = w * 0.5f, hz = d * 0.5f;
        var generated = StructureGeometry.Group(this, "Generated");
        var shell = StructureGeometry.Group(generated, "Exterior");
        var detail = StructureGeometry.Group(generated, "TimberAndProps");
        var rooms = StructureGeometry.Group(generated, "Rooms");
        _roof = StructureGeometry.Group(generated, "RoofVisuals"); _roof.Visible = ShowRoof;
        var body = StructureGeometry.Body(generated);
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
        StructureGeometry.Box(detail, null, "DoorThreshold", new(0, 0.06f, front + 0.2f),
            new(2.1f, 0.08f, 0.50f), stone);
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

        // A partition to make the right side into a separate room, with its own real door gap.
        const float dividingX = 2.7f, gapA = -0.8f, gapB = 1.35f;
        StructureGeometry.WallZ(rooms, body, "PartitionRear", dividingX, -hz + 0.25f, gapA, 0, 2.95f, woodLight, 0.15f);
        StructureGeometry.WallZ(rooms, body, "PartitionFront", dividingX, gapB, hz - 0.25f, 0, 2.95f, woodLight, 0.15f);
        StructureGeometry.WallZ(rooms, body, "PartitionHeader", dividingX, gapA, gapB, 2.65f, 2.95f, woodLight, 0.15f);
        // Rear left office/bedroom: a second partition with a different walk-through gap.
        float partitionZ = -hz + 3.35f;
        StructureGeometry.WallX(rooms, body, "RearOfficeLeft", -hx + 0.25f, -4.8f,
            partitionZ, 0, 2.95f, woodLight, 0.15f);
        StructureGeometry.WallX(rooms, body, "RearOfficeRight", -2.9f, dividingX - 0.2f,
            partitionZ, 0, 2.95f, woodLight, 0.15f);
        StructureGeometry.WallX(rooms, body, "RearOfficeLintel", -4.8f, -2.9f,
            partitionZ, 2.6f, 2.95f, woodLight, 0.15f);
        // Interior furniture is modest but gives the rooms real gameplay scale.
        StructureGeometry.Box(rooms, body, "ReceptionDesk", new(-3.1f, 0.66f, hz - 2.15f),
            new(2.7f, 1.3f, 0.7f), woodDark);
        StructureGeometry.Box(rooms, null, "DeskTop", new(-3.1f, 1.34f, hz - 2.15f),
            new(2.9f, 0.10f, 0.95f), woodLight);
        StructureGeometry.Box(rooms, body, "OfficeCabinet", new(-hx + 1.1f, 1.04f, -hz + 1.3f),
            new(1.45f, 2.05f, 0.75f), woodDark);
        StructureGeometry.Box(rooms, body, "BunkBedBase", new(hx - 2f, 0.54f, -hz + 2.1f),
            new(1.65f, 0.96f, 3.2f), trim);
        StructureGeometry.Box(rooms, null, "Mattress", new(hx - 2f, 1.08f, -hz + 2.1f),
            new(1.53f, 0.17f, 3.04f), slateLight);
        StructureGeometry.Box(rooms, null, "Pillow", new(hx - 2f, 1.20f, -hz + 0.8f),
            new(1.20f, 0.17f, 0.53f), stone);
        StructureGeometry.Box(rooms, body, "EquipmentLocker", new(hx - 1.2f, 1.02f, hz - 1.35f),
            new(0.95f, 2.04f, 1.06f), trim);
        StructureGeometry.Box(rooms, null, "LockerDoor", new(hx - 1.75f, 1.06f, hz - 1.35f),
            new(0.06f, 1.82f, 0.93f), slate);
        StructureGeometry.Box(rooms, body, "Stove", new(-hx + 1.38f, 0.92f, 0.15f),
            new(1.05f, 1.72f, 1.06f), trim);
        StructureGeometry.Box(rooms, null, "StoveFirebox", new(-hx + 1.38f, 0.98f, 0.72f),
            new(0.64f, 0.56f, 0.06f), accent);

        // Thick timber framework and a porch that visually distinguishes this from Building001.
        foreach (float x in new[] { -hx, hx })
        {
            StructureGeometry.Box(detail, null, "CornerPostFront", new(x, h * 0.5f, front),
                new(0.29f, h, 0.30f), trim);
            StructureGeometry.Box(detail, null, "CornerPostBack", new(x, h * 0.5f, -hz),
                new(0.29f, h, 0.30f), trim);
        }
        // Porch deck remains visually above level terrain but is deliberately non-colliding.
        StructureGeometry.Box(detail, null, "FrontVeranda", new(0, 0.07f, front + 1.38f),
            new(w - 1.8f, 0.10f, 3.25f), woodLight);
        for (float x = -hx + 0.75f; x < hx - 0.25f; x += 0.85f)
            StructureGeometry.Box(detail, null, "VerandaBoard", new(x, 0.128f, front + 1.35f),
                new(0.022f, 0.006f, 3.13f), woodDark);
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
            StructureGeometry.Box(detail, null, "PorchHandRail", new(x, 1.00f, front + 1.43f),
                new(0.14f, 0.12f, 2.50f), trim);
            for (int j = 0; j < 3; j++)
                StructureGeometry.Box(detail, null, "PorchBaluster", new(x, 0.57f, front + 0.43f + j * 0.83f),
                    new(0.10f, 1.05f, 0.10f), woodDark);
        }

        StructureGeometry.GableRoof(_roof, body, w, d, h, rise, slate, slateLight, 9);
        StructureGeometry.GableEnd(shell, "EastGable",  hx, hz, h, rise, wood);
        StructureGeometry.GableEnd(shell, "WestGable", -hx, hz, h, rise, wood);
        // Flat lean-to roof over veranda, hidden with the main roof for top-down interior testing.
        StructureGeometry.Box(_roof, null, "PorchRoof", new(0, 3.52f, front + 1.48f),
            new(w - 1.55f, 0.22f, 3.44f), slate);
        StructureGeometry.Box(_roof, null, "PorchRoofFascia", new(0, 3.42f, front + 3.15f),
            new(w - 1.38f, 0.33f, 0.18f), trim);
        StructureGeometry.Box(_roof, null, "ChimneyStack", new(-w * 0.25f, h + rise + 0.48f, -1.1f),
            new(1.25f, 2.20f, 1.25f), stoneDark);
        StructureGeometry.Box(_roof, null, "ChimneyCap", new(-w * 0.25f, h + rise + 1.65f, -1.1f),
            new(1.54f, 0.19f, 1.53f), stone);
        StructureGeometry.Box(detail, null, "RangerStationSign", new(0, 3.45f, front + 0.24f),
            new(3.6f, 0.43f, 0.13f), trim);
        for (int i = 0; i < 5; i++)
            StructureGeometry.Box(detail, null, "SignInlay", new(-1.40f + i * 0.70f, 3.45f, front + 0.32f),
                new(0.35f, 0.17f, 0.045f), accent);
        AddShowcaseDetails(generated, body, s, hx, hz, h);
    }


    // =========================================================
    // A separate dressing pass: all details remain optional visuals or selective furniture collision.
    // Keep exterior openings, walk-through interior partitions and overall footprint untouched.
    private static void AddShowcaseDetails(Node3D generated, StaticBody3D body,
        Building002Settings settings, float hx, float hz, float h)
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

        // EXTERIOR: continuous horizontal clapboard/shadow-line accents. Skip the door and front window glazing.
        for (float y = 0.73f; y < h - 0.38f; y += 0.52f)
        {
            foreach (float z in new[] { -hz - 0.14f, hz + 0.14f })
            {
                if (z > 0 && y > 1.15f && y < 3.02f)
                {
                    foreach (var seg in new[] { (-hx + 0.15f, -5.55f), (-2.95f, -1.06f),
                                               (1.06f, 3.43f), (6.06f, hx - 0.15f) })
                        StructureGeometry.Box(exterior, null, "FrontWeatherboard",
                            new((seg.Item1 + seg.Item2) * 0.5f, y, z),
                            new(seg.Item2 - seg.Item1, 0.065f, 0.045f), carved);
                }
                else StructureGeometry.Box(exterior, null, "Weatherboard",
                    new(0, y, z), new(hx * 2f - 0.32f, 0.065f, 0.045f), carved);
            }
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
            StructureGeometry.Box(exterior, null, "PorchBenchSeat", new(x, 0.76f, hz + 1.98f),
                new(1.80f, 0.14f, 0.54f), pale);
            StructureGeometry.Box(exterior, null, "PorchBenchBack", new(x, 1.12f, hz + 2.22f),
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
        StructureGeometry.Box(interior, null, "LargeRangerMap", new(-1.35f, 2.14f, hz - 0.19f),
            new(2.30f, 1.28f, 0.08f), carved);
        StructureGeometry.Box(interior, null, "MapSurface", new(-1.35f, 2.14f, hz - 0.25f),
            new(2.06f, 1.03f, 0.035f), paper);
        for (int i = 0; i < 4; i++)
            StructureGeometry.Box(interior, null, "MapContours", new(-2.02f + i * 0.44f, 2.18f, hz - 0.275f),
                new(0.065f, 0.70f - (i % 2) * 0.18f, 0.018f), moss);

        for (int i = 0; i < 3; i++)
        {
            StructureGeometry.Box(interior, null, "DeskPaper", new(-3.55f + i * 0.42f, 1.41f,
                hz - 2.15f), new(0.31f, 0.018f, 0.42f), paper);
            StructureGeometry.Box(interior, null, "Pencil", new(-3.5f + i * 0.30f, 1.44f,
                hz - 2.45f), new(0.035f, 0.022f, 0.29f), yellow);
        }
        StructureGeometry.Box(interior, null, "DeskRadio", new(-2.23f, 1.54f, hz - 2.15f),
            new(0.43f, 0.35f, 0.30f), steel);
        StructureGeometry.Box(interior, null, "RadioFace", new(-2.23f, 1.55f, hz - 1.985f),
            new(0.29f, 0.17f, 0.02f), ink);
        StructureGeometry.Box(interior, null, "RadioAntenna", new(-2.35f, 1.91f, hz - 2.17f),
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
        StructureGeometry.Box(interior, null, "OfficeNotice", new(-5.05f, 2.17f, -hz + 0.18f),
            new(1.20f, 0.90f, 0.06f), paper);
        StructureGeometry.Box(interior, null, "CabinetDoorDetail", new(-hx + 1.10f, 1.16f, -hz + 1.75f),
            new(1.23f, 1.65f, 0.05f), carved);
        StructureGeometry.Box(interior, null, "BunkBlanket", new(hx - 2f, 1.20f, -hz + 2.65f),
            new(1.55f, 0.08f, 1.68f), cloth);
        StructureGeometry.Box(interior, null, "BunkFoldedBlanket", new(hx - 2f, 1.27f, -hz + 3.14f),
            new(1.27f, 0.09f, 0.50f), pale);
        StructureGeometry.Box(interior, null, "LanternBase", new(hx - 1.05f, 1.25f, -hz + 4.40f),
            new(0.30f, 0.10f, 0.30f), steel);
        StructureGeometry.Box(interior, null, "LanternGlass", new(hx - 1.05f, 1.44f, -hz + 4.40f),
            new(0.21f, 0.30f, 0.21f), yellow);
        StructureGeometry.Box(interior, null, "LanternCap", new(hx - 1.05f, 1.62f, -hz + 4.40f),
            new(0.33f, 0.10f, 0.33f), steel);
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
}
