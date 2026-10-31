// Rifle_001: grounded, machined C# carbine artwork. Visual-only grouped assemblies.
// Builds once beneath Visual/Generated; original weapon root, muzzle marker and all mechanics stay untouched.
using Godot;

public partial class RifleMeshBuilder : Node3D
{
    // Reused matte/brushed PBR tones. Moderate reflectivity makes broad chamfers readable without neon.
    private static readonly StandardMaterial3D Receiver = new()
    { AlbedoColor = new Color(0.20f, 0.225f, 0.235f), Metallic = 0.76f, Roughness = 0.36f };
    private static readonly StandardMaterial3D EdgeSteel = new()
    { AlbedoColor = new Color(0.27f, 0.285f, 0.29f), Metallic = 0.82f, Roughness = 0.28f };
    private static readonly StandardMaterial3D DarkSteel = new()
    { AlbedoColor = new Color(0.095f, 0.11f, 0.12f), Metallic = 0.65f, Roughness = 0.48f };
    private static readonly StandardMaterial3D Polymer = new()
    { AlbedoColor = new Color(0.29f, 0.32f, 0.265f), Metallic = 0.05f, Roughness = 0.72f };
    private static readonly StandardMaterial3D Grip = new()
    { AlbedoColor = new Color(0.17f, 0.19f, 0.17f), Metallic = 0.08f, Roughness = 0.86f };
    private static readonly StandardMaterial3D Marking = new()
    { AlbedoColor = new Color(0.54f, 0.55f, 0.48f), Metallic = 0.28f, Roughness = 0.50f };
    private static readonly Mesh Bevel = WeaponVisualGeometry.BroadChamfer;
    private static readonly Mesh TaperedBevel = WeaponVisualGeometry.BroadTaperRear;

    // =========================================================
    // Separate named groups keep the art easy to inspect and replace. Visual recoil moves all of them together.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        var art = Group(this, "Generated");
        BuildStock(Group(art, "Stock"));
        BuildReceiver(Group(art, "Receiver"));
        BuildMagazineAndGrip(Group(art, "ControlsAndMagazine"));
        BuildHandguard(Group(art, "Handguard"));
        BuildBarrel(Group(art, "BarrelAndMuzzle"));
        BuildSights(Group(art, "IronSights"));
        BuildMarkings(Group(art, "SurfaceMarkings"));
        SetProcess(false); SetPhysicsProcess(false);
    }

    #region Grouped rifle assemblies

    // =========================================================
    // Contiguous stock shell with a wider butt pad, shaped cheek line and a small metal end cap.
    private static void BuildStock(Node3D part)
    {
        Part(part, "StockShell", new(0, 0.009f, 0.230f), new(0.097f, 0.136f, 0.278f),
            Polymer, TaperedBevel);
        Part(part, "StockShoulder", new(0, 0.019f, 0.095f), new(0.082f, 0.092f, 0.112f),
            Receiver, Bevel);
        Part(part, "CheekRest", new(0, 0.091f, 0.246f), new(0.083f, 0.032f, 0.227f),
            Grip, Bevel);
        Part(part, "StockHeel", new(0, -0.052f, 0.285f), new(0.080f, 0.033f, 0.156f),
            Polymer, Bevel);
        Part(part, "ButtPadBacking", new(0, 0.011f, 0.354f), new(0.109f, 0.157f, 0.026f),
            Receiver, Bevel);
        Part(part, "ButtPadRubber", new(0, 0.011f, 0.374f), new(0.110f, 0.152f, 0.020f),
            Grip, Bevel);
        foreach (float x in new[] { -0.050f, 0.050f })
        {
            Part(part, "StockInset", new(x, -0.002f, 0.243f), new(0.005f, 0.057f, 0.155f),
                Grip, Bevel);
        }
    }

    // =========================================================
    // Layered receiver and edge catches: no giant unbroken flat side or coplanar detail faces.
    private static void BuildReceiver(Node3D part)
    {
        Part(part, "UpperReceiver", new(0, 0.068f, -0.158f), new(0.109f, 0.102f, 0.325f),
            Receiver, Bevel);
        Part(part, "LowerReceiver", new(0, -0.012f, -0.129f), new(0.098f, 0.080f, 0.283f),
            DarkSteel, Bevel);
        Part(part, "UpperSpine", new(0, 0.121f, -0.163f), new(0.081f, 0.025f, 0.284f),
            Receiver, Bevel);
        Part(part, "RearReceiverCap", new(0, 0.070f, 0.008f), new(0.103f, 0.101f, 0.023f),
            EdgeSteel, Bevel);
        // Positive-X ejection port is on the open/right side, slightly proud of the receiver face.
        Part(part, "EjectionPortInset", new(0.056f, 0.067f, -0.174f),
            new(0.007f, 0.044f, 0.120f), DarkSteel, WeaponVisualGeometry.Box);
        Part(part, "EjectionBolt", new(0.060f, 0.066f, -0.176f),
            new(0.005f, 0.020f, 0.084f), EdgeSteel, WeaponVisualGeometry.Box);
        Part(part, "EjectionCatch", new(0.065f, 0.047f, -0.129f),
            new(0.007f, 0.009f, 0.019f), Receiver, Bevel);
        Tube(part, "ChargingHandle", new(0.069f, 0.028f, 0.005f), 0.030f, 0.044f, DarkSteel);
        // Stock line and fore-end are visually bridged by a slim receiver edge band.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.055f;
            Part(part, "UpperBevelBand", new(x, 0.104f, -0.150f),
                new(0.009f, 0.013f, 0.265f), EdgeSteel, Bevel);
            Part(part, "LowerSideInset", new(side * 0.052f, -0.013f, -0.130f),
                new(0.006f, 0.044f, 0.176f), Polymer, Bevel);
            Part(part, "ReceiverRearLock", new(side * 0.055f, 0.010f, 0.015f),
                new(0.011f, 0.022f, 0.025f), DarkSteel, Bevel);
        }
    }

    // =========================================================
    // Grip, trigger and magazine sit in a common receiver assembly; ribs read as molded details.
    private static void BuildMagazineAndGrip(Node3D part)
    {
        Part(part, "PistolGrip", new(0, -0.116f, 0.014f), new(0.080f, 0.196f, 0.091f),
            Grip, TaperedBevel, new Vector3(-0.21f, 0, 0));
        Part(part, "GripBackstrap", new(0, -0.139f, 0.060f), new(0.064f, 0.115f, 0.021f),
            Polymer, Bevel);
        for (int i = 0; i < 3; i++)
        {
            Part(part, "GripTread", new(0.041f, -0.076f - i * 0.036f, 0.016f + i * 0.009f),
                new(0.005f, 0.008f, 0.066f), Polymer, WeaponVisualGeometry.Box);
        }
        Part(part, "TriggerGuardFront", new(0, -0.122f, -0.101f),
            new(0.015f, 0.084f, 0.016f), Receiver);
        Part(part, "TriggerGuardBottom", new(0, -0.165f, -0.042f),
            new(0.019f, 0.018f, 0.132f), Receiver);
        Part(part, "Trigger", new(0, -0.117f, -0.020f),
            new(0.015f, 0.056f, 0.015f), DarkSteel, WeaponVisualGeometry.Box,
            new Vector3(-0.13f, 0, 0));
        Part(part, "MagazineWell", new(0, -0.066f, -0.180f),
            new(0.083f, 0.074f, 0.151f), Receiver, Bevel);
        Part(part, "MagazineShell", new(0, -0.153f, -0.185f),
            new(0.072f, 0.166f, 0.117f), Polymer, TaperedBevel,
            new Vector3(-0.10f, 0, 0));
        Part(part, "MagazineFloorplate", new(0, -0.236f, -0.178f),
            new(0.078f, 0.023f, 0.122f), DarkSteel, Bevel);
        for (int i = 0; i < 3; i++)
            Part(part, "MagazineRib", new(0.039f, -0.106f - i * 0.044f, -0.182f),
                new(0.007f, 0.012f, 0.096f), Grip, WeaponVisualGeometry.Box);
    }

    // =========================================================
    // Shaped handguard, visible structural top/bottom rails and repeatable vent slots.
    private static void BuildHandguard(Node3D part)
    {
        Part(part, "HandguardShell", new(0, 0.046f, -0.440f),
            new(0.117f, 0.114f, 0.266f), Polymer, TaperedBevel);
        Part(part, "HandguardTop", new(0, 0.101f, -0.435f),
            new(0.089f, 0.024f, 0.250f), Receiver, Bevel);
        Part(part, "HandguardBottom", new(0, -0.015f, -0.430f),
            new(0.083f, 0.024f, 0.250f), DarkSteel, Bevel);
        Part(part, "HandguardNose", new(0, 0.045f, -0.564f),
            new(0.109f, 0.105f, 0.027f), Receiver, Bevel);
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.061f;
            Part(part, "HandguardSidePanel", new(x, 0.027f, -0.443f),
                new(0.008f, 0.065f, 0.213f), Grip, Bevel);
            for (int i = 0; i < 3; i++)
                Part(part, "VentRecess", new(side * 0.066f, 0.037f, -0.350f - i * 0.074f),
                    new(0.004f, 0.020f, 0.044f), DarkSteel, WeaponVisualGeometry.Box);
            Part(part, "HandguardEdgeBand", new(side * 0.056f, 0.088f, -0.442f),
                new(0.008f, 0.013f, 0.230f), EdgeSteel, Bevel);
        }
        // Rail anchors overlap the rear receiver spine and the fore-end, so no floating rail pieces.
        Part(part, "RailBase", new(0, 0.116f, -0.310f),
            new(0.073f, 0.025f, 0.497f), DarkSteel, Bevel);
        Part(part, "RailUpperTrack", new(0, 0.135f, -0.310f),
            new(0.057f, 0.018f, 0.493f), Receiver, Bevel);
        for (int i = 0; i < 9; i++)
            Part(part, "PicatinnyTooth", new(0, 0.147f, -0.075f - i * 0.052f),
                new(0.081f, 0.015f, 0.015f), EdgeSteel, Bevel);
    }

    // =========================================================
    // Continuous metal path from the handguard to the muzzle face. Muzzle stays at local (0,.055,-.81).
    private static void BuildBarrel(Node3D part)
    {
        Tube(part, "BarrelCore", new(0, 0.055f, -0.647f), 0.052f, 0.281f, Receiver);
        Tube(part, "BarrelHeatShield", new(0, 0.055f, -0.619f), 0.069f, 0.190f, DarkSteel);
        Tube(part, "GasBlock", new(0, 0.055f, -0.615f), 0.077f, 0.072f, Receiver);
        Tube(part, "BarrelForwardCollar", new(0, 0.055f, -0.750f), 0.067f, 0.076f, EdgeSteel);
        Tube(part, "MuzzleBrake", new(0, 0.055f, -0.781f), 0.069f, 0.057f, DarkSteel);
        Tube(part, "MuzzleFrontRim", new(0, 0.055f, -0.801f), 0.071f, 0.015f, Receiver);
        Tube(part, "BoreRecess", new(0, 0.055f, -0.809f), 0.028f, 0.002f, DarkSteel);
        // Two slim muzzle slots on the sides, not detached floating fins.
        for (int side = -1; side <= 1; side += 2)
            Part(part, "MuzzleBrakePort", new(side * 0.035f, 0.055f, -0.777f),
                new(0.004f, 0.011f, 0.023f), Grip, WeaponVisualGeometry.Box);
    }

    // =========================================================
    // Both sight frames have explicit saddles extending into their supports. No free-standing ears.
    private static void BuildSights(Node3D part)
    {
        // Rear base overlaps the rear portion of the top rail, and two pillars form an open aperture.
        Part(part, "RearSightSaddle", new(0, 0.149f, -0.072f),
            new(0.094f, 0.032f, 0.052f), DarkSteel, Bevel);
        Part(part, "RearSightFoot", new(0, 0.159f, -0.067f),
            new(0.080f, 0.031f, 0.041f), Receiver, Bevel);
        for (int side = -1; side <= 1; side += 2)
        {
            Part(part, "RearSightUpright", new(side * 0.034f, 0.192f, -0.063f),
                new(0.014f, 0.072f, 0.034f), DarkSteel, Bevel);
            Part(part, "RearSightPivot", new(side * 0.045f, 0.173f, -0.063f),
                new(0.012f, 0.022f, 0.024f), EdgeSteel, Bevel);
        }
        Part(part, "RearSightBridge", new(0, 0.228f, -0.063f),
            new(0.079f, 0.014f, 0.036f), Receiver, Bevel);
        // Front assembly sits over the barrel on a continuous bridge, not on air.
        Part(part, "FrontSightBarrelClamp", new(0, 0.088f, -0.705f),
            new(0.079f, 0.042f, 0.069f), Receiver, Bevel);
        Part(part, "FrontSightBridge", new(0, 0.111f, -0.705f),
            new(0.078f, 0.023f, 0.055f), DarkSteel, Bevel);
        for (int side = -1; side <= 1; side += 2)
            Part(part, "FrontSightGuard", new(side * 0.026f, 0.150f, -0.706f),
                new(0.012f, 0.074f, 0.028f), Receiver, Bevel);
        Part(part, "FrontSightPost", new(0, 0.155f, -0.706f),
            new(0.008f, 0.071f, 0.012f), DarkSteel, WeaponVisualGeometry.Box);
    }

    // =========================================================
    // Engraved-looking mechanical marks are small inset/raised geometry, not a glowing sci-fi decal.
    private static void BuildMarkings(Node3D part)
    {
        Part(part, "RightSerialPlate", new(0.064f, -0.005f, -0.080f),
            new(0.009f, 0.030f, 0.070f), Receiver, Bevel);
        for (int i = 0; i < 3; i++)
            Part(part, "SerialBar", new(0.071f, -0.014f + i * 0.010f, -0.087f),
                new(0.003f, 0.003f, 0.035f - i * 0.005f), Marking, WeaponVisualGeometry.Box);
        Part(part, "SelectorDisc", new(-0.060f, -0.004f, 0.009f),
            new(0.014f, 0.029f, 0.029f), EdgeSteel, Bevel);
        Part(part, "SelectorPointer", new(-0.071f, 0.010f, 0.009f),
            new(0.007f, 0.008f, 0.024f), Marking, WeaponVisualGeometry.Box);
        // Tiny paired receiver fasteners with visible heads on each cheek.
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 2; i++)
                Part(part, "ReceiverFastener", new(side * 0.058f, -0.024f, -0.056f - i * 0.137f),
                    new(0.012f, 0.017f, 0.017f), EdgeSteel, Bevel);
        Part(part, "MagazineIndex", new(0.041f, -0.205f, -0.184f),
            new(0.004f, 0.008f, 0.056f), Marking, WeaponVisualGeometry.Box);
    }

    #endregion

    // =========================================================
    private static Node3D Group(Node3D parent, string name)
    { var group = new Node3D { Name = name }; parent.AddChild(group); return group; }

    // =========================================================
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size,
        Material material, Mesh? shape = null, Vector3? rotation = null)
        => WeaponVisualGeometry.Part(parent, name, position, size, material, shape, rotation);

    // =========================================================
    private static void Tube(Node3D parent, string name, Vector3 position,
        float diameter, float length, Material material)
        => WeaponVisualGeometry.Tube(parent, name, position, diameter, length, material);
}
