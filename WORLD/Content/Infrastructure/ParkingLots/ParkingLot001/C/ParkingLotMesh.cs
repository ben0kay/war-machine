// ParkingLot001: standalone reusable asphalt, marked parking bays, sidewalk and curb-access apron.
// One-time Godot C# geometry, static surface collision and passive Marker3D slots. No site/world dependencies.
using Godot;

[Tool]
public partial class ParkingLotMesh : Node3D
{
    [Export] public ParkingLotSettings Settings { get; set; } = null!;

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
        StructureGeometry.Box(surfaces, collision, "AsphaltApron",
            new(0, thickness * 0.5f, 0), new(width, thickness, depth), asphalt);
        StructureGeometry.Box(surfaces, collision, "DrivewayStub",
            new(0, thickness * 0.5f, halfDepth + driveLength * 0.5f),
            new(driveWidth, thickness, driveLength), asphalt);
        BuildEdgeRamps(surfaces, collision, halfWidth, halfDepth, driveWidth * 0.5f,
            driveLength, topY, Mathf.Clamp(s.EdgeRampWidth, 0.25f, 2), asphalt);
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
        StructureGeometry.Box(sidewalk, collision, "EntranceAccess",
            new(0, topY - 0.008f, walkwayCenterZ),
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
    // Follow the complete apron+driveway outline, including its two concave joins. Flat slab colliders
    // stay authoritative on top; these outward shoulders replace the abrupt 12cm step from any side.
    private static void BuildEdgeRamps(Node3D visuals, StaticBody3D collision, float halfWidth,
        float halfDepth, float driveHalf, float driveLength, float top, float width, Material material)
    {
        Vector2[] edge = { new(-halfWidth, -halfDepth), new(halfWidth, -halfDepth),
            new(halfWidth, halfDepth), new(driveHalf, halfDepth), new(driveHalf, halfDepth + driveLength),
            new(-driveHalf, halfDepth + driveLength), new(-driveHalf, halfDepth), new(-halfWidth, halfDepth) };
        var outer = new Vector2[edge.Length];
        for (int i = 0; i < edge.Length; i++)
        {
            Vector2 incoming = (edge[i] - edge[(i + edge.Length - 1) % edge.Length]).Normalized();
            Vector2 outgoing = (edge[(i + 1) % edge.Length] - edge[i]).Normalized();
            // All edges are axis-aligned/right-angle: summed outward normals form an exact miter.
            outer[i] = edge[i] + new Vector2(incoming.Y + outgoing.Y, -incoming.X - outgoing.X) * width;
        }
        using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < edge.Length; i++)
        {
            int next = (i + 1) % edge.Length;
            Vector3 a = new(edge[i].X, top, edge[i].Y), b = new(edge[next].X, top, edge[next].Y);
            Vector3 c = new(outer[next].X, -0.02f, outer[next].Y), d = new(outer[i].X, -0.02f, outer[i].Y);
            foreach (var face in new[] { (a, b, c), (a, c, d) })
            {
                // Godot front faces are clockwise: use the reversed cross product.
                Vector3 normal = (face.Item3 - face.Item1).Cross(face.Item2 - face.Item1).Normalized();
                bool reverse = normal.Y < 0; if (reverse) normal = -normal;
                foreach (Vector3 vertex in new[] { face.Item1, reverse ? face.Item3 : face.Item2,
                    reverse ? face.Item2 : face.Item3 })
                { surface.SetNormal(normal); surface.AddVertex(vertex); }
            }
            // Solid volume rather than one-way triangle collision. Bury bottom/toe in the ground;
            // the top edge meets the slab exactly, with a small collision margin at shared seams.
            collision.AddChild(new CollisionShape3D { Name = $"WalkableAsphaltShoulder{i + 1:00}",
                Shape = new ConvexPolygonShape3D { Margin = 0.001f, Points = new[] {
                    a, b, c, d, new Vector3(a.X, -0.10f, a.Z), new Vector3(b.X, -0.10f, b.Z),
                    new Vector3(c.X, -0.10f, c.Z), new Vector3(d.X, -0.10f, d.Z) } } });
        }
        var mesh = surface.Commit();
        var shoulderMaterial = (StandardMaterial3D)material.Duplicate();
        shoulderMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        visuals.AddChild(new MeshInstance3D { Name = "SlopedAsphaltShoulders", Mesh = mesh, MaterialOverride = shoulderMaterial });
    }

    // =========================================================
    private static void AddMarker(Node3D parent, string name, Vector3 position)
        => parent.AddChild(new Marker3D { Name = name, Position = position });
}
