// Simple original low-poly deer visual, assembled once on its existing Entity.VisualRoot.
// Four long legs/hooves, horizontal body, angled neck, ears, muzzle, tail and optional branching antlers.
// Meshes are decorative and do not collide; actor physics remains one shared capsule.
using Godot;

[Tool]
public partial class DeerMeshBuilder : Node3D
{
    [Export] public DeerSettings Settings { get; set; } = null!;

    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1f, RadialSegments = 12, Rings = 8 };
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly CylinderMesh RodMesh = new()
        { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 8 };

    // =========================================================
    // The tool preview and runtime both construct once; no skeletal animation/physics callbacks are added.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") == null) Build();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Local forward is -Z, just like the shared zombie/robot actors.
    private void Build()
    {
        var s = Settings ?? new DeerSettings();
        var art = new Node3D { Name = "Generated" }; AddChild(art);
        Material coat = Paint(s.CoatColor), pale = Paint(s.BellyColor), hoof = Paint(s.HoofColor);
        Material antler = Paint(s.AntlerColor), eye = Paint(new Color(0.085f, 0.069f, 0.053f));

        // Compact, level-backed torso and lighter belly; shoulder approximately 1.35m above the feet.
        Part(art, "Barrel", new(0, 1.14f, 0.10f), new(0.60f, 0.61f, 1.58f), Rounded, coat);
        Part(art, "Chest", new(0, 1.21f, -0.59f), new(0.61f, 0.67f, 0.58f), Rounded, coat);
        Part(art, "Rump", new(0, 1.16f, 0.73f), new(0.59f, 0.60f, 0.57f), Rounded, coat);
        Part(art, "LightBelly", new(0, 0.94f, 0.04f), new(0.43f, 0.16f, 1.05f), Rounded, pale);

        // Four independent straight legs with lower tapered-looking shins and dark pointed hooves.
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 0.245f;
            foreach (float z in new[] { -0.56f, 0.65f })
            {
                string name = (side < 0 ? "L" : "R") + (z < 0 ? "Front" : "Rear");
                // Pivot at the hip so deer motion can swing these four legs without a second animator.
                var pivot = new Node3D { Name = name + "LegPivot", Position = new Vector3(x, 1.02f, z) };
                art.AddChild(pivot);
                Part(pivot, name + "UpperLeg", new(0f, -0.29f, 0f),
                    new(0.19f, 0.59f, 0.20f), Rounded, coat);
                Part(pivot, name + "Knee", new(0f, -0.52f, -0.025f),
                    new(0.16f, 0.15f, 0.17f), Rounded, coat);
                Rod(pivot, name + "LowerLeg", new(0f, -0.51f, -0.02f),
                    new(0f, -0.87f, z > 0 ? 0.045f : -0.06f), 0.07f, coat);
                Part(pivot, name + "Hoof", new(0f, -0.928f, -0.085f),
                    new(0.18f, 0.17f, 0.29f), Box, hoof);
            }
        }

        // Angled rising neck and slender face, nose to -Z. Pale throat shows the silhouette.
        Rod(art, "Neck", new(0, 1.28f, -0.68f), new(0, 1.76f, -0.95f), 0.23f, coat);
        Rod(art, "Throat", new(0, 1.30f, -0.82f), new(0, 1.64f, -1.01f), 0.12f, pale);
        Part(art, "Head", new(0, 1.79f, -1.035f), new(0.39f, 0.34f, 0.48f), Rounded, coat);
        Part(art, "LongMuzzle", new(0, 1.70f, -1.28f), new(0.25f, 0.19f, 0.39f), Rounded, pale);
        Part(art, "Nose", new(0, 1.68f, -1.45f), new(0.18f, 0.10f, 0.09f), Rounded, hoof);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(art, side < 0 ? "LeftEar" : "RightEar",
                new(side * 0.26f, 1.97f, -0.99f), new(0.31f, 0.13f, 0.20f), Rounded, coat,
                new(0, 0, side * -18f));
            Part(art, side < 0 ? "LeftEye" : "RightEye",
                new(side * 0.197f, 1.83f, -1.15f), new(0.045f, 0.045f, 0.045f), Rounded, eye);
        }

        // The little white tail is obvious from the rear in the overhead camera.
        Part(art, "WhiteTail", new(0, 1.24f, 1.01f), new(0.23f, 0.25f, 0.30f), Rounded, pale);
        if (s.HasAntlers) BuildAntlers(art, antler);
    }

    // =========================================================
    // Short branching antlers signal a stag without imported art or complex procedural topology.
    private static void BuildAntlers(Node3D root, Material antler)
    {
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 0.125f;
            string n = side < 0 ? "Left" : "Right";
            Vector3 basePoint = new(x, 1.95f, -0.94f);
            Vector3 mid = new(side * 0.21f, 2.19f, -0.96f);
            Vector3 tip = new(side * 0.37f, 2.47f, -0.94f);
            Rod(root, n + "AntlerBase", basePoint, mid, 0.055f, antler);
            Rod(root, n + "AntlerMain", mid, tip, 0.042f, antler);
            Rod(root, n + "AntlerTineA", mid, new(side * 0.17f, 2.36f, -1.18f), 0.032f, antler);
            Rod(root, n + "AntlerTineB", tip, new(side * 0.55f, 2.60f, -1.08f), 0.027f, antler);
        }
    }

    // =========================================================
    private static Material Paint(Color color) => new StandardMaterial3D
        { AlbedoColor = color, Roughness = 0.91f };

    // =========================================================
    // A unit primitive scaled by whole dimensions, not separate unique mesh resources per limb.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 dimensions,
        Mesh mesh, Material material, Vector3? rotation = null)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = mesh, Position = position,
            Scale = dimensions, RotationDegrees = rotation ?? Vector3.Zero,
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        });
    }

    // =========================================================
    // Rotate one cheap unit cylinder between two endpoints; visual-only, no additional colliders.
    private static void Rod(Node3D parent, string name, Vector3 start, Vector3 end,
        float radius, Material material)
    {
        Vector3 delta = end - start;
        if (delta.LengthSquared() < 0.000001f) return;
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = RodMesh, Position = (start + end) * 0.5f,
            Quaternion = new Quaternion(Vector3.Up, delta.Normalized()),
            Scale = new Vector3(radius * 2f, delta.Length(), radius * 2f),
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        });
    }
}
