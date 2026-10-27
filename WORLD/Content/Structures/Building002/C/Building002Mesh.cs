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
        for (float x in new[] { -hx + 1.05f, hx - 1.05f })
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
