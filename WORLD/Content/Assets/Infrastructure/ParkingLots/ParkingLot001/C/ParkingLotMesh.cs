// ParkingLot001: standalone reusable asphalt, marked parking bays, sidewalk and curb-access apron.
// One-time Godot C# geometry, static surface collision and passive Marker3D slots. No site/world dependencies.
using Godot;

[Tool]
public partial class ParkingLotMesh : Node3D
{
    [Export] public ParkingLotSettings Settings { get; set; } = null!;
    // Site mode: native graded terrain owns the walking surface; retain only raised curb collision.
    [Export] public bool UseTerrainSupport { get; set; }

    // =========================================================
    // The entire prefab is generated exactly once per instance; never polls the terrain or player.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") == null) Build();
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    // =========================================================
    // -Z faces the future store, +Z is the road. Y=0 is the foundation's underside.
    private void Build()
    {
        var s = Settings ?? new ParkingLotSettings();
        float width = Mathf.Clamp(s.Width, 34f, 60f);
        float depth = Mathf.Clamp(s.Depth, 24f, 42f);
        float thickness = Mathf.Clamp(s.AsphaltThickness, 0.06f, 0.25f);
        float halfWidth = width * 0.5f, halfDepth = depth * 0.5f;
        float topY = thickness;
        float sidewalkDepth = Mathf.Clamp(s.SidewalkDepth, 1.6f, 3.5f);
        float sidewalkHeight = Mathf.Clamp(s.SidewalkHeight, 0.10f, 0.24f);
        float gap = Mathf.Clamp(s.EntranceGapWidth, 2f, 4.5f);
        float bayWidth = Mathf.Clamp(s.BayWidth, 2.6f, 3.6f);
        float bayDepth = Mathf.Clamp(s.BayDepth, 4.8f, 6.5f);
        float accessGap = Mathf.Clamp(s.CentralDriveGap, 6f, 12f);
        float interiorAisle = Mathf.Clamp(s.InteriorAisleWidth, 5.5f, 8f);
        float markingWidth = Mathf.Clamp(s.LineWidth, 0.04f, 0.15f);
        float driveWidth = Mathf.Clamp(s.DrivewayWidth, 5.5f, 12f);
        float driveLength = Mathf.Clamp(s.DrivewayLength, 3f, 12f);

        // Reserve a 2m side margin and keep the complete stall row on the main asphalt.
        int requestedBays = Mathf.Clamp(s.BayCount, 1, 16);
        int bayCount = Mathf.Min(requestedBays, Mathf.FloorToInt((width - 4f) / bayWidth));
        if (bayCount < requestedBays)
            GD.PushWarning($"ParkingLot001: {requestedBays} bays do not fit; showing {bayCount}.");

        var generated = StructureGeometry.Group(this, "Generated");
        var surfaces = StructureGeometry.Group(generated, "Surfaces");
        surfaces.Position = Vector3.Up * 0.01f; // Render above a coincident terrain floor, not another physical step.
        var painted = StructureGeometry.Group(generated, "PaintedLines");
        var sidewalk = StructureGeometry.Group(generated, "Sidewalk");
        var markers = StructureGeometry.Group(generated, "Markers");
        var bays = StructureGeometry.Group(markers, "ParkingSpots");
        var collision = StructureGeometry.Body(generated); // Same static layer-1 physics as other solid world props.

        var asphalt = StructureGeometry.Paint(s.AsphaltColor);
        var wornAsphalt = StructureGeometry.Paint(s.AsphaltWearColor);
        var concrete = StructureGeometry.Paint(s.ConcreteColor);
        var curb = StructureGeometry.Paint(s.CurbColor);
        var paint = StructureGeometry.Paint(s.LineColor);

        // One shallow collidable apron and one same-height southern driveway extension.
        StructureGeometry.Box(surfaces, UseTerrainSupport ? null : collision, "AsphaltApron",
            new(0, thickness * 0.5f, 0), new(width, thickness, depth), asphalt);
        StructureGeometry.Box(surfaces, UseTerrainSupport ? null : collision, "DrivewayStub",
            new(0, thickness * 0.5f, halfDepth + driveLength * 0.5f),
            new(driveWidth, thickness, driveLength), asphalt);
        // A small subdued asphalt patch, with no additional collider, breaks the uniform colour.
        StructureGeometry.Box(surfaces, null, "OldAsphaltPatch",
            new(-halfWidth * 0.32f, topY + 0.002f, 0.9f),
            new(width * 0.26f, 0.004f, 1.15f), wornAsphalt);

        // A raised northern pedestrian strip with a centrally lowered walking access.
        float northEdge = -halfDepth + 0.25f;
        float curbZ = northEdge + sidewalkDepth;
        float walkwayCenterZ = (northEdge + curbZ) * 0.5f;
        float segmentLength = (width - 0.50f - gap) * 0.5f;
        float walkwayY = topY + sidewalkHeight * 0.5f;
        float segmentX = gap * 0.5f + segmentLength * 0.5f;
        foreach (float side in new[] { -1f, 1f })
        {
            StructureGeometry.Box(sidewalk, collision, "RaisedWalkway",
                new(side * segmentX, walkwayY, walkwayCenterZ),
                new(segmentLength, sidewalkHeight, sidewalkDepth), concrete);
            // Shallow curb rim is separate geometry/physics; the central gap stays free of a raised kerb.
            StructureGeometry.Box(sidewalk, collision, "CurbRim",
                new(side * segmentX, topY + sidewalkHeight * 0.44f, curbZ + 0.015f),
                new(segmentLength, sidewalkHeight * 0.88f, 0.20f), curb);
        }
        // A low central access strip that aligns with the future convenience-store entrance.
        StructureGeometry.Box(sidewalk, null, "EntranceAccess",
            new(0, topY + 0.002f, walkwayCenterZ),
            new(gap, 0.020f, sidewalkDepth), concrete);

        // The front of the bays is near the sidewalk; their southern ends open into the driving aisle.
        float headZ = curbZ + 0.52f;
        float bayEndZ = headZ + bayDepth;
        float rowLeft = -(bayCount * bayWidth) * 0.5f;
        float paintY = topY + 0.007f;
        for (int i = 0; i <= bayCount; i++)
        {
            float x = rowLeft + i * bayWidth;
            StructureGeometry.Box(painted, null, "BayDivider",
                new(x, paintY, (headZ + bayEndZ) * 0.5f),
                new(markingWidth, 0.012f, bayDepth), paint);
        }
        StructureGeometry.Box(painted, null, "BayHeadStripe",
            new(0, paintY, headZ), new(bayCount * bayWidth, 0.012f, markingWidth), paint);

        // Each parking slot has one stable, correctly oriented marker. Future Car001 faces local -Z.
        for (int i = 0; i < bayCount; i++)
        {
            float x = rowLeft + (i + 0.5f) * bayWidth;
            AddMarker(bays, $"Bay_{i + 1:00}", new(x, topY, (headZ + bayEndZ) * 0.5f));
        }

        // Add optional central and road-facing rows, with an open crossing from the driveway to
        // the original storefront aisle. Banked stalls never overlap the central through-route.
        int rows = Mathf.Clamp(s.ParkingRows, 1, 3);
        int sideBays = Mathf.Max(0, Mathf.FloorToInt((width * 0.5f - 1.5f - accessGap * 0.5f) / bayWidth));
        float nextStartZ = bayEndZ + interiorAisle;
        int markerNumber = bayCount + 1;
        for (int row = 1; row < rows; row++)
        {
            float rowEndZ = nextStartZ + bayDepth;
            if (sideBays == 0 || rowEndZ > halfDepth - 1.4f)
            {
                GD.PushWarning($"ParkingLot001: extra row {row + 1} does not fit within asphalt.");
                break;
            }
            // Outer row heads face south; the middle row heads north. Both share the
            // second 6m driving aisle and leave the main 9.3m central approach unobstructed.
            bool faceSouth = row == 2;
            for (int side = -1; side <= 1; side += 2)
            {
                float left = side < 0
                    ? -accessGap * 0.5f - sideBays * bayWidth : accessGap * 0.5f;
                for (int i = 0; i <= sideBays; i++)
                {
                    float x = left + i * bayWidth;
                    StructureGeometry.Box(painted, null, "ExtraBayDivider",
                        new(x, paintY, (nextStartZ + rowEndZ) * 0.5f),
                        new(markingWidth, 0.012f, bayDepth), paint);
                }
                StructureGeometry.Box(painted, null, "ExtraBayHeadStripe",
                    new(left + sideBays * bayWidth * 0.5f, paintY,
                        faceSouth ? rowEndZ : nextStartZ),
                    new(sideBays * bayWidth, 0.012f, markingWidth), paint);
                for (int i = 0; i < sideBays; i++)
                {
                    float x = left + (i + 0.5f) * bayWidth;
                    bays.AddChild(new Marker3D
                    {
                        Name = $"Bay_{markerNumber++:00}",
                        Position = new Vector3(x, topY, (nextStartZ + rowEndZ) * 0.5f),
                        Rotation = new Vector3(0, faceSouth ? Mathf.Pi : 0, 0)
                    });
                }
            }
            nextStartZ = rowEndZ + interiorAisle;
        }

        // Short access-road markings are part of the lot only, not a world-wide road manager.
        foreach (float x in new[] { -driveWidth * 0.5f + 0.15f, driveWidth * 0.5f - 0.15f })
            StructureGeometry.Box(painted, null, "DrivewayEdge",
                new(x, paintY, halfDepth + driveLength * 0.5f),
                new(markingWidth, 0.012f, driveLength - 0.36f), paint);
        for (int i = 0; i < 2; i++)
            StructureGeometry.Box(painted, null, "DrivewayCentreDash",
                new(0, paintY, halfDepth + 1.3f + i * 2.45f),
                new(markingWidth, 0.012f, 0.95f), paint);

        // Independent orientation/approach anchors. Not gameplay positions until the site owner uses them.
        AddMarker(markers, "Entrance", new(0, topY, curbZ + 0.9f));
        AddMarker(markers, "Storefront", new(0, topY, -halfDepth - 0.35f));
        AddMarker(markers, "RoadConnection", new(0, topY, halfDepth + driveLength));
    }

    // =========================================================
    private static void AddMarker(Node3D parent, string name, Vector3 position)
        => parent.AddChild(new Marker3D { Name = name, Position = position });
}
