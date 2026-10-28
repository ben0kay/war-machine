// Lightweight woodland deer silhouette with tapered neck, shorter torso and fine jointed legs.
// All four hip pivots retain their names for the shared AI gait; build once, no visual update loop.
// Meshes are decorative and do not collide; actor physics remains one shared capsule.
using Godot;

[Tool]
public partial class DeerMeshBuilder : Node3D
{
    [Export] public DeerSettings Settings { get; set; } = null!;

    private static readonly SphereMesh Rounded = new() { Radius = 0.5f, Height = 1f, RadialSegments = 12, Rings = 8 };
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly CylinderMesh NeckMesh = new()
        { TopRadius = 0.34f, BottomRadius = 0.60f, Height = 1f, RadialSegments = 10 };
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

        // Short, shallow torso with a pronounced higher shoulder and narrower hindquarters.
        // The overlapping rounded pieces break up the former long cylindrical "sausage" profile.
        Part(art, "Barrel", new(0, 1.19f, 0.10f), new(0.53f, 0.50f, 1.27f), Rounded, coat);
        Part(art, "Chest", new(0, 1.28f, -0.48f), new(0.53f, 0.59f, 0.54f), Rounded, coat);
        Part(art, "Rump", new(0, 1.14f, 0.66f), new(0.51f, 0.51f, 0.49f), Rounded, coat);
        Part(art, "LightBelly", new(0, 0.99f, 0.06f), new(0.37f, 0.14f, 0.81f), Rounded, pale);

        // Four independent straight legs with lower tapered-looking shins and dark pointed hooves.
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 0.215f;
            foreach (float z in new[] { -0.46f, 0.62f })
            {
                string name = (side < 0 ? "L" : "R") + (z < 0 ? "Front" : "Rear");
                // Pivot at the hip so deer motion can swing these four legs without a second animator.
                var pivot = new Node3D { Name = name + "LegPivot", Position = new Vector3(x, 1.07f, z) };
                art.AddChild(pivot);
                Part(pivot, name + "UpperLeg", new(0f, -0.28f, 0f),
                    new(0.145f, 0.56f, 0.15f), Rounded, coat);
                Part(pivot, name + "Knee", new(0f, -0.55f, -0.02f),
                    new(0.11f, 0.11f, 0.12f), Rounded, coat);
                Rod(pivot, name + "LowerLeg", new(0f, -0.53f, -0.02f),
                    new(0f, -0.91f, z > 0 ? 0.04f : -0.045f), 0.048f, coat);
                Part(pivot, name + "Hoof", new(0f, -0.99f, -0.065f),
                    new(0.135f, 0.13f, 0.18f), Box, hoof);
            }
        }

        // Slimmer, naturally forward-leaning neck and small wedge-like face (forward = -Z).
        Rod(art, "Neck", new(0, 1.29f, -0.56f), new(0, 1.63f, -0.98f),
            0.17f, coat, NeckMesh);
        Rod(art, "Throat", new(0, 1.29f, -0.71f), new(0, 1.53f, -1.05f), 0.085f, pale);
        Part(art, "Head", new(0, 1.68f, -1.08f), new(0.29f, 0.29f, 0.38f), Rounded, coat);
        Part(art, "LongMuzzle", new(0, 1.59f, -1.28f), new(0.18f, 0.14f, 0.30f), Rounded, pale);
        Part(art, "Nose", new(0, 1.56f, -1.425f), new(0.11f, 0.07f, 0.075f), Rounded, hoof);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(art, side < 0 ? "LeftEar" : "RightEar",
                new(side * 0.225f, 1.85f, -1.005f), new(0.27f, 0.10f, 0.17f), Rounded, coat,
                new(-7f, 0f, side * -20f));
            Part(art, side < 0 ? "LeftEye" : "RightEye",
                new(side * 0.152f, 1.72f, -1.19f), new(0.036f, 0.036f, 0.036f), Rounded, eye);
        }

        // The little white tail is obvious from the rear in the overhead camera.
        Part(art, "WhiteTail", new(0, 1.14f, 0.94f), new(0.15f, 0.16f, 0.20f), Rounded, pale);
        if (s.HasAntlers) BuildAntlers(art, antler);
    }

    // =========================================================
    // Short branching antlers signal a stag without imported art or complex procedural topology.
    private static void BuildAntlers(Node3D root, Material antler)
    {
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 0.10f;
            string n = side < 0 ? "Left" : "Right";
            Vector3 basePoint = new(x, 1.81f, -1.00f);
            Vector3 mid = new(side * 0.16f, 1.98f, -1.01f);
            Vector3 tip = new(side * 0.25f, 2.13f, -1.03f);
            Rod(root, n + "AntlerBase", basePoint, mid, 0.032f, antler);
            Rod(root, n + "AntlerMain", mid, tip, 0.025f, antler);
            Rod(root, n + "AntlerTine", mid, new(side * 0.14f, 2.09f, -1.15f), 0.019f, antler);
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
        float radius, Material material, Mesh? mesh = null)
    {
        Vector3 delta = end - start;
        if (delta.LengthSquared() < 0.000001f) return;
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = mesh ?? RodMesh, Position = (start + end) * 0.5f,
            Quaternion = new Quaternion(Vector3.Up, delta.Normalized()),
            Scale = new Vector3(radius * 2f, delta.Length(), radius * 2f),
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        });
    }
}
