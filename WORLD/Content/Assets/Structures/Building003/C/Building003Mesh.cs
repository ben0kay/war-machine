// Building003: abandoned rural fuel station with a walk-in shop, closed service bay and pump canopy.
// One-shot Godot C# mesh construction and static collision. Artwork/ is reserved for imported models.
using Godot;

[Tool]
public partial class Building003Mesh : Node3D
{
    [Export] public Building003Settings Settings { get; set; } = null!;
    [Export] public bool ShowRoof { get; set; } = true;
    private Node3D? _roof;

    // =========================================================
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        Build(); DeclareVisibility();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Both roof/canopy visuals hide for a readable overhead preview; roof solidity remains unchanged.
    public void SetRoofVisible(bool visible)
    {
        ShowRoof = visible;
        if (GodotObject.IsInstanceValid(_roof)) _roof!.Visible = visible;
    }

    // =========================================================
    // Physical store entrance and rear garage exit, with separated panes/shutter and navigable interior.
    private void Build()
    {
        var s = Settings ?? new Building003Settings();
        float w = Mathf.Clamp(s.Width, 18f, 28f), d = Mathf.Clamp(s.Depth, 10f, 17f);
        float h = Mathf.Clamp(s.WallHeight, 3.5f, 5.5f);
        float hx = w * 0.5f, hz = d * 0.5f, front = hz, back = -hz;
        var generated = StructureGeometry.Group(this, "Generated");
        var shell = StructureGeometry.Group(generated, "StationShell");
        var interior = StructureGeometry.Group(generated, "ShopAndServiceInterior");
        var props = StructureGeometry.Group(generated, "ExteriorProps");
        _roof = StructureGeometry.Group(generated, "RoofAndCanopy"); _roof.Visible = ShowRoof;
        var stationRoof = StructureGeometry.Group(_roof, "StationRoof");
        var canopy = StructureGeometry.Group(_roof, "PumpCanopy");
        var body = StructureGeometry.Body(generated);

        var wall = StructureGeometry.Paint(s.Walls);
        var wallLight = StructureGeometry.Paint(s.Walls.Lightened(0.13f));
        var darkWall = StructureGeometry.Paint(s.Walls.Darkened(0.18f));
        var metal = StructureGeometry.Paint(s.Roof, 0.24f);
        var trim = StructureGeometry.Paint(s.Trim, 0.30f);
        var trimLight = StructureGeometry.Paint(s.Trim.Lightened(0.25f), 0.16f);
        var glass = StructureGeometry.Paint(s.Glass, 0.1f);
        var red = StructureGeometry.Paint(s.Accent);
        var concrete = StructureGeometry.Paint(s.Concrete);
        var concreteDark = StructureGeometry.Paint(s.Concrete.Darkened(0.27f));
        var rubber = StructureGeometry.Paint(new Color(0.11f, 0.13f, 0.13f));

        // The floor and asphalt forecourt are only thin decorative overlays; Terrain3D handles foot grounding.
        StructureGeometry.Box(interior, null, "IndoorConcreteSlab", new(0, 0.05f, 0),
            new(w - 0.20f, 0.09f, d - 0.20f), concreteDark);
        StructureGeometry.Box(props, null, "PumpForecourt", new(0, 0.034f, front + 7f),
            new(w + 3.6f, 0.065f, 13f), concrete);
        StructureGeometry.Box(props, null, "CustomerWalkway", new(-4.5f, 0.06f, front + 0.75f),
            new(w * 0.49f, 0.09f, 1.55f), concreteDark);

        // Front wall (+Z). A real open 2.1m door; glass storefront; a closed, solid service shutter.
        const float windowL = -6.8f, windowR = -3.8f, doorL = -3.1f, doorR = -1.0f;
        const float garageL = 1.7f, garageR = 7.5f, winLo = 0.95f, winHi = 3.18f;
        StructureGeometry.WallX(shell, body, "FrontLeft", -hx, windowL, front, 0, h, wall);
        StructureGeometry.WallX(shell, body, "FrontShopWindowSill", windowL, windowR, front, 0, winLo, wall);
        StructureGeometry.WallX(shell, body, "FrontShopWindowHead", windowL, windowR, front, winHi, h, wall);
        StructureGeometry.WallX(shell, body, "FrontWindowToDoor", windowR, doorL, front, 0, h, wall);
        StructureGeometry.WallX(shell, body, "EntryLintel", doorL, doorR, front, 2.88f, h, wall);
        StructureGeometry.WallX(shell, body, "DoorToService", doorR, garageL, front, 0, h, wall);
        StructureGeometry.WallX(shell, body, "ServiceLintel", garageL, garageR, front, 3.55f, h, wall);
        StructureGeometry.WallX(shell, body, "FrontRight", garageR, hx, front, 0, h, wall);

        // Storefront panes are their own colliders, not a solid wall hidden behind a window texture.
        StructureGeometry.Box(shell, body, "StorefrontGlass", new((windowL + windowR) * 0.5f, (winHi + winLo) * 0.5f, front + 0.14f),
            new(windowR - windowL, winHi - winLo, 0.06f), glass);
        foreach (float x in new[] { windowL, windowR })
            StructureGeometry.Box(props, null, "StoreWindowJamb", new(x, (winHi + winLo) * 0.5f, front + 0.18f),
                new(0.13f, winHi - winLo + 0.17f, 0.17f), trim);
        StructureGeometry.Box(props, null, "StoreWindowCross", new((windowL + windowR) * 0.5f, 2.0f, front + 0.20f),
            new(windowR - windowL + 0.10f, 0.11f, 0.12f), trim);
        StructureGeometry.Box(props, null, "StoreWindowMiddle", new((windowL + windowR) * 0.5f, 2.05f, front + 0.20f),
            new(0.10f, winHi - winLo, 0.12f), trim);

        // Open door: hinged leaf parked inside to the side without a physical blocker across the opening.
        StructureGeometry.Box(props, null, "DoorJambL", new(doorL, 1.42f, front + 0.19f),
            new(0.12f, 2.84f, 0.18f), trim);
        StructureGeometry.Box(props, null, "DoorJambR", new(doorR, 1.42f, front + 0.19f),
            new(0.12f, 2.84f, 0.18f), trim);
        StructureGeometry.Box(props, null, "DoorHeadTrim", new((doorL + doorR) * 0.5f, 2.91f, front + 0.19f),
            new(2.30f, 0.13f, 0.19f), trim);
        StructureGeometry.Box(props, null, "DoorThreshold", new((doorL + doorR) * 0.5f, 0.09f, front + 0.25f),
            new(2.10f, 0.07f, 0.65f), concrete);
        StructureGeometry.Box(props, null, "ProppedOpenDoor", new(doorL - 0.07f, 1.42f, front - 0.90f),
            new(0.09f, 2.68f, 1.68f), trimLight);
        // Garage bay shutter is intentionally closed and blocks passage, unlike the adjacent shop door.
        StructureGeometry.Box(shell, body, "GarageShutter", new((garageL + garageR) * 0.5f, 1.775f, front + 0.04f),
            new(garageR - garageL, 3.55f, 0.14f), metal);
        for (int row = 0; row < 11; row++)
            StructureGeometry.Box(props, null, "ShutterRib", new((garageL + garageR) * 0.5f, 0.25f + row * 0.29f, front + 0.13f),
                new(garageR - garageL - 0.16f, 0.05f, 0.06f), trimLight);
        foreach (float x in new[] { garageL, garageR })
            StructureGeometry.Box(props, null, "ShutterGuide", new(x, 1.8f, front + 0.17f),
                new(0.14f, 3.63f, 0.23f), trim);

        // Rear is an independent staff/service entrance with no collision bar across its door gap.
        const float rearL = 6.5f, rearR = 8.6f;
        StructureGeometry.WallX(shell, body, "BackLeft", -hx, rearL, back, 0, h, wall);
        StructureGeometry.WallX(shell, body, "BackDoorHeader", rearL, rearR, back, 2.77f, h, wall);
        StructureGeometry.WallX(shell, body, "BackRight", rearR, hx, back, 0, h, wall);
        StructureGeometry.Box(props, null, "ServiceExitFrame", new((rearL + rearR) * 0.5f, 2.82f, back - 0.14f),
            new(2.32f, 0.13f, 0.22f), trim);
        StructureGeometry.Box(props, null, "BackThreshold", new((rearL + rearR) * 0.5f, 0.06f, back - 0.10f),
            new(2.18f, 0.08f, 0.50f), concrete);
        // Store side has a genuine window opening. Service side is blank industrial cladding.
        float windowStart = -2.0f, windowEnd = 1.55f, lo = 1.46f, hi = 3.07f;
        StructureGeometry.WallZ(shell, body, "StoreSideRear", -hx, back, windowStart, 0, h, wall);
        StructureGeometry.WallZ(shell, body, "StoreSideFront", -hx, windowEnd, front, 0, h, wall);
        StructureGeometry.WallZ(shell, body, "StoreSideSill", -hx, windowStart, windowEnd, 0, lo, wall);
        StructureGeometry.WallZ(shell, body, "StoreSideLintel", -hx, windowStart, windowEnd, hi, h, wall);
        StructureGeometry.Box(shell, body, "StoreSideGlass", new(-hx - 0.14f, (lo + hi) * 0.5f, (windowStart + windowEnd) * 0.5f),
            new(0.06f, hi - lo, windowEnd - windowStart), glass);
        StructureGeometry.Box(props, null, "StoreSideMullion", new(-hx - 0.21f, (lo + hi) * 0.5f, (windowStart + windowEnd) * 0.5f),
            new(0.10f, hi - lo, 0.10f), trim);
        StructureGeometry.WallZ(shell, body, "RightWall", hx, back, front, 0, h, wall);

        // Divide garage and shop with a physical wall and a clear 2.1m internal passage.
        const float divider = 1.7f, openA = -2.6f, openB = -0.5f;
        StructureGeometry.WallZ(interior, body, "InteriorWallRear", divider, back + 0.25f, openA, 0, 3.4f, darkWall, 0.17f);
        StructureGeometry.WallZ(interior, body, "InteriorWallFront", divider, openB, front - 0.25f, 0, 3.4f, darkWall, 0.17f);
        StructureGeometry.WallZ(interior, body, "PassageLintel", divider, openA, openB, 2.7f, 3.4f, darkWall, 0.17f);
        // Shelving aisles have individual collision, and all aisles remain accessible from the entry.
        for (int aisle = 0; aisle < 2; aisle++)
        {
            float x = -7.6f + aisle * 2.95f;
            StructureGeometry.Box(interior, body, "ShelfBase", new(x, 0.91f, -1.10f),
                new(0.72f, 1.72f, 3.10f), trim);
            for (int shelf = 0; shelf < 3; shelf++)
                StructureGeometry.Box(interior, null, "ShelfProducts", new(x, 0.38f + shelf * 0.52f, -1.10f),
                    new(0.82f, 0.16f, 3.0f), red);
        }
        StructureGeometry.Box(interior, body, "CheckoutCounter", new(-6.0f, 0.65f, front - 1.38f),
            new(2.45f, 1.25f, 0.80f), darkWall);
        StructureGeometry.Box(interior, null, "CheckoutTop", new(-6f, 1.30f, front - 1.38f),
            new(2.65f, 0.10f, 0.90f), concrete);
        StructureGeometry.Box(interior, null, "CashRegister", new(-5.5f, 1.52f, front - 1.40f),
            new(0.57f, 0.34f, 0.43f), trimLight);
        StructureGeometry.Box(interior, body, "ColdCabinet", new(-hx + 0.88f, 1.28f, back + 1.15f),
            new(1.15f, 2.5f, 1.38f), metal);
        StructureGeometry.Box(interior, null, "ColdCabinetGlass", new(-hx + 1.50f, 1.29f, back + 1.15f),
            new(0.07f, 2.1f, 1.02f), glass);
        // Workbench inside the garage, leaving enough room to enter from the rear exit.
        StructureGeometry.Box(interior, body, "ServiceWorkbench", new(hx - 1.6f, 0.63f, 1.12f),
            new(1.20f, 1.20f, 3.10f), darkWall);
        StructureGeometry.Box(interior, null, "WorkbenchTop", new(hx - 1.6f, 1.28f, 1.12f),
            new(1.38f, 0.12f, 3.30f), concrete);
        for (int i = 0; i < 3; i++)
            StructureGeometry.Box(interior, null, "WorkbenchTools", new(hx - 1.70f, 1.46f, 0.18f + i * 0.90f),
                new(0.17f, 0.35f, 0.17f), red);

        // Roof/parapet/HVAC are separate from the open ground-floor interior.
        StructureGeometry.Box(stationRoof, body, "FlatRoof", new(0, h + 0.10f, 0),
            new(w + 0.50f, 0.22f, d + 0.50f), metal);
        StructureGeometry.Box(stationRoof, null, "RoofParapetFront", new(0, h + 0.42f, front),
            new(w + 0.45f, 0.58f, 0.25f), trim);
        StructureGeometry.Box(stationRoof, null, "RoofParapetRear", new(0, h + 0.42f, back),
            new(w + 0.45f, 0.58f, 0.25f), trim);
        foreach (float x in new[] { -hx, hx })
            StructureGeometry.Box(stationRoof, null, "RoofParapetSide", new(x, h + 0.42f, 0),
                new(0.25f, 0.58f, d + 0.40f), trim);
        StructureGeometry.Box(stationRoof, null, "ACUnit", new(-hx * 0.48f, h + 0.62f, -1.5f),
            new(2.1f, 0.88f, 1.50f), trimLight);
        for (int i = 0; i < 5; i++)
            StructureGeometry.Box(stationRoof, null, "ACVentSlots", new(-hx * 0.48f - 0.74f + i * 0.37f,
                h + 1.11f, -1.5f), new(0.20f, 0.06f, 1.13f), rubber);

        // Deep independent gas-station canopy: a conspicuous roofline in the top-down distance.
        float canopyZ = front + 7.3f, canopyFront = canopyZ + 5.6f;
        StructureGeometry.Box(canopy, null, "PumpCanopy", new(-0.6f, 5.65f, canopyZ),
            new(w + 1.6f, 0.28f, 11.2f), metal);
        StructureGeometry.Box(canopy, null, "CanopyFrontFascia", new(-0.6f, 5.36f, canopyFront),
            new(w + 1.75f, 0.75f, 0.32f), trim);
        StructureGeometry.Box(canopy, null, "CanopyRedBand", new(-0.6f, 5.45f, canopyFront + 0.18f),
            new(w + 1.75f, 0.18f, 0.045f), red);
        StructureGeometry.Box(canopy, null, "CanopyRearFascia", new(-0.6f, 5.37f, canopyZ - 5.6f),
            new(w + 1.75f, 0.65f, 0.25f), trim);
        for (int side = -1; side <= 1; side += 2)
            for (int depth = -1; depth <= 1; depth += 2)
            {
                float x = side * (hx - 1.9f) - 0.6f;
                float z = canopyZ + depth * 4.60f;
                StructureGeometry.Box(props, body, "CanopySupport", new(x, 2.68f, z),
                    new(0.39f, 5.35f, 0.39f), concreteDark);
                StructureGeometry.Box(props, null, "SupportCollar", new(x, 0.31f, z),
                    new(0.65f, 0.62f, 0.65f), concrete);
            }

        // Two pump islands with physical pumps, hoses, guards and nozzle silhouettes.
        for (int i = 0; i < 2; i++)
        {
            float x = -5.1f + i * 8.4f;
            StructureGeometry.Box(props, null, "PumpIsland", new(x, 0.10f, canopyZ),
                new(2.60f, 0.20f, 4.0f), concreteDark);
            StructureGeometry.Box(props, body, "FuelPumpCabinet", new(x, 1.03f, canopyZ),
                new(0.95f, 2.0f, 1.0f), wallLight);
            StructureGeometry.Box(props, null, "PumpScreen", new(x, 1.53f, canopyZ + 0.53f),
                new(0.64f, 0.56f, 0.05f), glass);
            StructureGeometry.Box(props, null, "PumpTop", new(x, 2.08f, canopyZ),
                new(1.14f, 0.14f, 1.13f), red);
            foreach (int side in new[] { -1, 1 })
            {
                StructureGeometry.Cylinder(props, "HoseLoopConnector",
                    new(x + side * 0.56f, 1.21f, canopyZ), 0.13f, 0.82f, rubber, 8);
                StructureGeometry.Box(props, null, "Nozzle", new(x + side * 0.63f, 1.18f, canopyZ + 0.35f),
                    new(0.12f, 0.13f, 0.65f), trim);
            }
            StructureGeometry.Box(props, body, "IslandGuardA", new(x, 0.52f, canopyZ - 1.78f),
                new(0.15f, 1.02f, 0.15f), red);
            StructureGeometry.Box(props, body, "IslandGuardB", new(x, 0.52f, canopyZ + 1.78f),
                new(0.15f, 1.02f, 0.15f), red);
        }

        // Human-scale forecourt markings and an aged roadside price sign (no glowing textures or dynamic lights).
        StructureGeometry.Box(props, null, "StoreNamePanel", new(-4.50f, h - 0.35f, front + 0.18f),
            new(8.3f, 0.65f, 0.11f), trim);
        for (int i = 0; i < 6; i++)
            StructureGeometry.Box(props, null, "SignStripe", new(-7.50f + i * 1.20f, h - 0.35f, front + 0.25f),
                new(0.55f, 0.32f, 0.05f), red);
        float signX = hx + 2.7f, signZ = canopyZ + 3.3f;
        StructureGeometry.Box(props, body, "RoadSignColumn", new(signX, 2.75f, signZ),
            new(0.43f, 5.45f, 0.43f), concreteDark);
        StructureGeometry.Box(props, null, "RoadSignFace", new(signX, 5.25f, signZ),
            new(2.6f, 1.42f, 0.26f), trim);
        StructureGeometry.Box(props, null, "RoadSignAccent", new(signX, 5.67f, signZ + 0.16f),
            new(2.25f, 0.26f, 0.06f), red);
        for (int i = 0; i < 3; i++)
            StructureGeometry.Box(props, null, "RoadSignRows", new(signX, 5.27f - i * 0.29f, signZ + 0.16f),
                new(1.75f, 0.09f, 0.06f), wallLight);
    }
    // =========================================================
    // Occupied forecourt includes the open canopy so pump contents stay solid while standing underneath it.
    private void DeclareVisibility()
    {
        var s = Settings ?? new Building003Settings();
        float w = Mathf.Clamp(s.Width, 18, 28), d = Mathf.Clamp(s.Depth, 10, 17);
        StructureLayout.Occupied(GetNode<Node3D>("Generated"), new Aabb(new Vector3(-w / 2, -0.5f, -d / 2), new Vector3(w, 7, d)));
        // A metadata-only marker carries the second local occupancy box; it has no process or physics.
        var forecourt = new Node3D { Name = "ForecourtOccupancy" }; GetNode("Generated").AddChild(forecourt);
        StructureLayout.Occupied(forecourt, new Aabb(new Vector3(-0.6f - (w + 1.75f) / 2, -0.5f, d / 2 - 0.3f), new Vector3(w + 1.75f, 7, 13.2f)));
        StructureLayout.Visual(GetNode<Node3D>("Generated/StationShell"), "walls");
        StructureLayout.Visual(GetNode<Node3D>("Generated/RoofAndCanopy/StationRoof"), "roof");
        StructureLayout.Visual(GetNode<Node3D>("Generated/RoofAndCanopy/PumpCanopy"), "roof");
        StructureLayout.Visual(GetNode<Node3D>("Generated/ShopAndServiceInterior"), "contents");
        StructureLayout.Visual(GetNode<Node3D>("Generated/ExteriorProps"), "contents");
    }

}
