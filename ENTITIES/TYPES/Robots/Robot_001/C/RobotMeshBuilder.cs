// Original industrial quadruped art. Unit meshes are shared; dimensions live on instance transforms.
using Godot;

public static class RobotMeshBuilder
{
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Joint = new() { Radius = 0.5f, Height = 1, RadialSegments = 10, Rings = 5 };
    public static readonly string[] LegNames = { "FrontLeft", "FrontRight", "RearLeft", "RearRight" };

    // =========================================================
    // Build once per actor; generated unit geometry is reused by every segment and robot instance.
    public static Node3D Build(Node3D visual, RobotSettings settings)
    {
        var armour = Material(settings.ArmourColor);
        var joints = Material(settings.JointColor);
        var glow = Material(settings.SensorColor);
        glow.EmissionEnabled = true; glow.Emission = settings.SensorColor;
        glow.EmissionEnergyMultiplier = Mathf.Max(0, settings.EmissionEnergy);
        var body = new Node3D { Name = "BodyRoot", Position = Vector3.Up * settings.HipHeight };
        visual.AddChild(body);
        Part(body, "Chassis", Box, Vector3.Zero, settings.BodySize, armour);
        Part(body, "DorsalArmour", Box, new(0, 1.2f, 0.4f), new(3.8f, 0.7f, 4.2f), armour);
        for (int side = -1; side <= 1; side += 2)
        {
            Part(body, $"Flank{side}", Box, new(side * 2.25f, 0, 0.3f), new(0.6f, 1.5f, 4.2f), joints);
            for (int vent = 0; vent < 4; vent++)
                Part(body, $"Vent{side}_{vent}", Box, new(side * 2.58f, 0.3f, -0.9f + vent * 0.65f), new(0.08f, 0.35f, 0.3f), armour);
        }
        var head = new Node3D { Name = "HeadPivot", Position = new(0, 1.6f, -1.9f) };
        body.AddChild(head);
        Part(head, "SensorHousing", Box, Vector3.Zero, new(2.4f, 1.4f, 1.8f), armour);
        Part(head, "Brow", Box, new(0, 0.7f, -0.3f), new(2.8f, 0.25f, 1.8f), joints);
        Part(head, "CyanOptic", Box, new(0, 0, -0.93f), new(1.7f, 0.22f, 0.08f), glow);
        head.AddChild(new Marker3D { Name = "SensorSocket", Position = new(0, 0, -1.02f) });
        for (int i = 0; i < 4; i++)
        {
            var hip = new Node3D { Name = LegNames[i] + "Hip", Position = HipOffset(i, settings) };
            body.AddChild(hip);
            Part(hip, "HipJoint", Joint, Vector3.Zero, Vector3.One * 1.2f, joints);
            Part(hip, "UpperArmour", Box, new(0, 0, -settings.UpperLegLength * 0.5f), new(0.65f, 0.8f, settings.UpperLegLength), armour);
            var knee = new Node3D { Name = "Knee" }; hip.AddChild(knee);
            Part(knee, "KneeJoint", Joint, Vector3.Zero, Vector3.One * 0.85f, joints);
            Part(knee, "LowerStrut", Box, new(0, 0, -settings.LowerLegLength * 0.5f), new(0.38f, 0.48f, settings.LowerLegLength), joints);
            var foot = new Node3D { Name = "Foot" }; knee.AddChild(foot);
            Part(foot, "Pad", Box, new(0, 0.18f, 0), new(1.25f, 0.36f, 1.6f), armour);
        }
        return body;
    }

    // =========================================================
    // Matching front/rear attachment positions are shared with the gait solver.
    public static Vector3 HipOffset(int leg, RobotSettings settings) =>
        new((leg % 2 == 0 ? -1 : 1) * settings.BodySize.X * 0.5f, -0.4f,
            (leg < 2 ? -1 : 1) * settings.BodySize.Z * 0.36f);

    // =========================================================
    // Create one material per finish per actor, never per frame or per limb.
    private static StandardMaterial3D Material(Color color) => new() { AlbedoColor = color, Metallic = 0.75f, Roughness = 0.65f };

    // =========================================================
    // Mesh nodes contain no scripts, update loops or collision.
    private static void Part(Node3D parent, string name, Mesh mesh, Vector3 position, Vector3 size, Material material)
    {
        parent.AddChild(new MeshInstance3D { Name = name, Mesh = mesh, Position = position, Scale = size, MaterialOverride = material });
    }
}
