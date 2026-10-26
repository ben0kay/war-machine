// Original heavy biped with cyan sensor pods and layered industrial armour. Shared primitive resources, built once.
using Godot;

public static class RobotMeshBuilder
{
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Joint = new() { Radius = 0.5f, Height = 1, RadialSegments = 12, Rings = 6 };
    public static readonly string[] LegNames = { "Left", "Right" };

    // =========================================================
    // Broad upper silhouette, two substantial legs and independently turning cyan head hardware.
    public static Node3D Build(Node3D visual, RobotSettings settings)
    {
        var armour = Material(settings.ArmourColor);
        var joints = Material(settings.JointColor);
        var inset = Material(settings.ArmourColor.Darkened(0.4f));
        var glow = Material(settings.SensorColor);
        glow.EmissionEnabled = true; glow.Emission = settings.SensorColor;
        glow.EmissionEnergyMultiplier = Mathf.Max(0, settings.EmissionEnergy);
        var body = new Node3D { Name = "BodyRoot", Position = Vector3.Up * settings.HipHeight }; visual.AddChild(body);
        Part(body, "Chassis", Box, Vector3.Zero, settings.BodySize, armour);
        Part(body, "SlopedChest", Box, new(0, 0.1f, -1.65f), new(4.25f, 1.75f, 0.5f), armour).Rotation = new Vector3(-0.16f, 0, 0);
        Part(body, "DorsalReactor", Box, new(0, 0.35f, 1.55f), new(2.7f, 2.1f, 0.85f), joints);
        Part(body, "Undercarriage", Box, new(0, -1.3f, 0), new(2.7f, 0.85f, 2.3f), inset);
        for (int x = -1; x <= 1; x++)
        {
            Part(body, $"BellyFin{x}", Box, new(x * 0.8f, -1.9f, -0.5f), new(0.25f, 1.1f, 0.8f), joints);
            Part(body, $"ChestVent{x}", Box, new(x * 0.65f, -0.25f, -1.94f), new(0.4f, 0.75f, 0.08f), inset);
        }
        var head = new Node3D { Name = "HeadPivot", Position = new(0, 1.6f, -0.35f) }; body.AddChild(head);
        Part(head, "SensorHousing", Box, Vector3.Zero, new(4.1f, 1.15f, 2.2f), armour);
        Part(head, "ArmouredBrow", Box, new(0, 0.6f, -0.2f), new(4.65f, 0.35f, 2.4f), joints).Rotation = new Vector3(-0.12f, 0, 0);
        Part(head, "OpticRecess", Box, new(0, -0.05f, -1.13f), new(2.6f, 0.55f, 0.12f), inset);
        Part(head, "CyanOptic", Box, new(0, -0.05f, -1.22f), new(1.8f, 0.22f, 0.09f), glow);
        head.AddChild(new Marker3D { Name = "SensorSocket", Position = new(0, -0.05f, -1.32f) });
        for (int side = -1; side <= 1; side += 2)
        {
            var pod = new Node3D { Name = side < 0 ? "LeftSensorPod" : "RightSensorPod",
                Position = new(side * 3.4f, 0.45f, 0.15f), Rotation = new(0, side * -0.16f, side * 0.16f) };
            head.AddChild(pod);
            Part(pod, "PodArmour", Box, Vector3.Zero, new(2.75f, 1.45f, 3.1f), armour);
            Part(pod, "PodTopPlate", Box, new(0, 0.78f, 0), new(3.0f, 0.22f, 3.35f), joints);
            Part(pod, "PodInset", Box, new(0, 0, -1.59f), new(2.25f, 0.9f, 0.1f), inset);
            Part(pod, "CyanSensorArray", Box, new(0, 0, -1.67f), new(2.0f, 0.58f, 0.08f), glow);
            for (int rib = -2; rib <= 2; rib++)
                Part(pod, $"OpticRib{rib}", Box, new(rib * 0.36f, 0, -1.73f), new(0.07f, 0.7f, 0.12f), joints);
            Part(body, $"HipGuard{side}", Box, new(side * 2.2f, -0.65f, 0.2f), new(1.25f, 1.8f, 2.2f), armour);
        }
        for (int i = 0; i < LegNames.Length; i++)
        {
            var hip = new Node3D { Name = LegNames[i] + "Hip", Position = HipOffset(i, settings) }; body.AddChild(hip);
            Part(hip, "HipBearing", Joint, Vector3.Zero, Vector3.One * 1.7f, joints);
            Part(hip, "UpperArmour", Box, new(0, 0, -settings.UpperLegLength * 0.5f), new(1.4f, 1.55f, settings.UpperLegLength), armour);
            Part(hip, "HydraulicRail", Box, new(0.8f, 0, -settings.UpperLegLength * 0.5f), new(0.22f, 0.28f, settings.UpperLegLength * 0.9f), joints);
            var knee = new Node3D { Name = "Knee" }; hip.AddChild(knee);
            Part(knee, "KneeBearing", Joint, Vector3.Zero, Vector3.One * 1.5f, joints);
            Part(knee, "KneeShield", Box, new(0, 0.35f, 0), new(1.7f, 0.65f, 1.2f), armour);
            Part(knee, "LowerStrut", Box, new(0, 0, -settings.LowerLegLength * 0.5f), new(1.0f, 1.2f, settings.LowerLegLength), joints);
            Part(knee, "ShinArmour", Box, new(0, 0.68f, -settings.LowerLegLength * 0.42f), new(1.3f, 0.24f, settings.LowerLegLength * 0.65f), armour);
            var foot = new Node3D { Name = "Foot" }; knee.AddChild(foot);
            Part(foot, "HeavyFoot", Box, new(0, 0.35f, -0.25f), new(2.0f, 0.7f, 3.0f), armour);
            for (int toe = -1; toe <= 1; toe++)
                Part(foot, $"Toe{toe}", Box, new(toe * 0.58f, 0.2f, -1.8f), new(0.48f, 0.4f, 0.8f), joints);
        }
        return body;
    }

    // =========================================================
    // Two hips sit below the chassis; their visual knees bend forward rather than splaying sideways.
    public static Vector3 HipOffset(int leg, RobotSettings settings) =>
        new((leg == 0 ? -1 : 1) * settings.BodySize.X * 0.4f, -0.4f, 0.25f);

    // =========================================================
    // Separate armour/inset/joint finishes keep the silhouette readable in mist without per-frame material work.
    private static StandardMaterial3D Material(Color color) => new() { AlbedoColor = color, Metallic = 0.65f, Roughness = 0.58f };

    // =========================================================
    // Primitive art nodes have no processing or collision; only the root actor is physical.
    private static MeshInstance3D Part(Node3D parent, string name, Mesh mesh, Vector3 position, Vector3 size, Material material)
    {
        var part = new MeshInstance3D { Name = name, Mesh = mesh, Position = position, Scale = size, MaterialOverride = material };
        parent.AddChild(part); return part;
    }
}
