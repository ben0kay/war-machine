// Original heavy biped with cyan sensor pods and layered industrial armour. Shared primitive resources, built once.
using Godot;

public static class RobotMeshBuilder
{
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private static readonly SphereMesh Joint = new() { Radius = 0.5f, Height = 1, RadialSegments = 12, Rings = 6 };
    // Half-metre radius, one-metre length before scaling: shared by every static cable segment.
    private static readonly CylinderMesh CableMesh = new()
    { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 8 };
    public static readonly string[] LegNames = { "Left", "Right" };

    // =========================================================
    // Broad upper silhouette, two substantial legs and independently turning cyan head hardware.
    public static Node3D Build(Node3D visual, RobotSettings settings)
    {
        // MAT1: one shared instance of each finish for this robot's primitive parts.
        // Existing cyan emissive optics/markers stay StandardMaterial3D until MAT0 is authorized.
        var armour = RobotMaterialFactory.Armour(settings);
        var joints = RobotMaterialFactory.Joint(settings, settings.JointColor);
        var inset = RobotMaterialFactory.Rubber(settings, settings.ArmourColor.Darkened(0.4f));
        var glow = Material(settings.SensorColor);
        glow.EmissionEnabled = true; glow.Emission = settings.SensorColor;
        glow.EmissionEnergyMultiplier = Mathf.Max(0, settings.EmissionEnergy);
        // Detail palette is deliberately darker/more restrained than the main cyan scanning optics.
        var edge = RobotMaterialFactory.Steel(settings, settings.JointColor.Lightened(0.22f));
        var recess = RobotMaterialFactory.Rubber(settings, settings.JointColor.Darkened(0.45f));
        var cable = RobotMaterialFactory.Rubber(settings, settings.JointColor.Darkened(0.64f));
        var steel = RobotMaterialFactory.Steel(settings, settings.JointColor.Lightened(0.32f));
        var marking = Material(new Color(0.60f, 0.52f, 0.33f));
        var accent = Material(settings.SensorColor.Darkened(0.60f));
        accent.EmissionEnabled = true; accent.Emission = settings.SensorColor;
        accent.EmissionEnergyMultiplier = Mathf.Max(0f, settings.EmissionEnergy * settings.DetailGlowMultiplier);
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
        if (settings.ExtraMeshDetail) DetailHead(head, armour, joints, edge, recess, accent, marking);
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
            if (settings.ExtraMeshDetail) DetailPod(pod, side, armour, edge, recess, steel, accent);
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
            if (settings.ExtraMeshDetail)
                DetailLeg(hip, knee, foot, settings, armour, joints, edge, recess, steel, cable, accent, marking);
        }
        if (settings.ExtraMeshDetail)
            DetailBody(body, settings, armour, joints, edge, recess, steel, cable, accent, marking);
        return body;
    }


    // =========================================================
    // Fixed industrial body plating, engine hardware, cooling grilles, cable trays and ID markings.
    // Details stay under BodyRoot so patrol/gait applies one shared transform; no new physics.
    private static void DetailBody(Node3D body, RobotSettings s, Material armour, Material joints,
        Material edge, Material recess, Material steel, Material cable, Material accent, Material marking)
    {
        // Layered angular breastplate and braced shoulders make the walker read as a heavy machine at distance.
        Part(body, "ChestCentrePlate", Box, new(0, 0.23f, -1.97f),
            new(2.55f, 1.62f, 0.22f), armour).Rotation = new Vector3(-0.15f, 0, 0);
        Part(body, "ChestKeel", Box, new(0, -0.59f, -2.04f),
            new(0.36f, 0.58f, 0.22f), steel).Rotation = new Vector3(0, 0, 0.12f);
        for (int side = -1; side <= 1; side += 2)
        {
            Part(body, $"ChestSideShield{side}", Box, new(side * 1.74f, 0.13f, -1.95f),
                new(0.98f, 1.68f, 0.28f), joints).Rotation = new Vector3(-0.10f, 0, side * 0.16f);
            Part(body, $"ChestRib{side}", Box, new(side * 1.22f, -0.40f, -2.13f),
                new(0.14f, 0.90f, 0.16f), edge).Rotation = new Vector3(0, 0, side * 0.10f);
            Part(body, $"ShoulderOverhang{side}", Box, new(side * 2.57f, 0.63f, -0.28f),
                new(1.47f, 0.42f, 2.76f), armour).Rotation = new Vector3(0, 0, -side * 0.12f);
            Part(body, $"ShoulderUnderbrace{side}", Box, new(side * 2.57f, 0.18f, 0.10f),
                new(0.65f, 0.65f, 2.08f), joints);
            Part(body, $"SideEngineHousing{side}", Box, new(side * 2.39f, -0.25f, 1.05f),
                new(0.35f, 0.86f, 1.28f), recess);
            Part(body, $"SideStatusStrip{side}", Box, new(side * 2.60f, -0.24f, 1.00f),
                new(0.052f, 0.085f, 0.70f), accent);

            // Bundled exposed power conduits follow only BodyRoot; don't stretch across animated hips.
            Cable(body, $"MainPowerCable{side}", cable, 0.10f,
                new(side * 1.82f, 0.60f, 1.47f),
                new(side * 2.48f, 0.23f, 1.64f),
                new(side * 2.56f, -0.58f, 1.13f));
            Cable(body, $"ReturnCable{side}", joints, 0.054f,
                new(side * 1.67f, 0.39f, 1.56f),
                new(side * 2.37f, -0.02f, 1.74f),
                new(side * 2.52f, -0.73f, 1.30f));
            Part(body, $"PowerClamp{side}", Box, new(side * 2.49f, 0.19f, 1.65f),
                new(0.22f, 0.23f, 0.22f), steel);

            // Small, non-emissive warning/ID bars rather than large neon stripes.
            Part(body, $"UnitMarkPlate{side}", Box, new(side * 1.81f, 0.48f, -2.13f),
                new(0.52f, 0.21f, 0.035f), recess);
            for (int bar = 0; bar < 2; bar++)
                Part(body, $"UnitMarkBar{side}_{bar}", Box,
                    new(side * 1.81f + (bar - 0.5f) * 0.16f, 0.48f, -2.16f),
                    new(0.055f, 0.14f, 0.018f), marking);
            Part(body, $"WarningSlash{side}", Box, new(side * 1.76f, -0.46f, -2.12f),
                new(0.11f, 0.30f, 0.035f), marking).Rotation = new Vector3(0, 0, 0.37f);
        }

        // Squared-off reactor spine, dark cooling slots and subtly powered exhausts.
        Part(body, "ReactorTopArmour", Box, new(0, 1.47f, 1.22f),
            new(2.62f, 0.20f, 1.38f), armour);
        Part(body, "ReactorRearGrille", Box, new(0, 0.52f, 2.02f),
            new(2.06f, 1.14f, 0.16f), recess);
        for (int rib = -3; rib <= 3; rib++)
            Part(body, $"ReactorVentRib{rib}", Box, new(rib * 0.28f, 0.53f, 2.14f),
                new(0.11f, 0.83f, 0.13f), edge);
        for (int x = -1; x <= 1; x++)
        {
            Part(body, $"DorsalCooler{x}", Box, new(x * 0.71f, 1.66f, 1.21f),
                new(0.25f, 0.43f, 1.34f), joints);
            Part(body, $"UndersideArmour{x}", Box, new(x * 0.74f, -1.62f, 0.47f),
                new(0.58f, 0.14f, 1.35f), armour);
        }
        Part(body, "RearReactorGlow", Box, new(0, 0.45f, 2.16f),
            new(0.54f, 0.10f, 0.04f), accent);
    }

    // =========================================================
    // Head detail deliberately avoids covering the central SensorSocket or cyan optic.
    // Head-local armour/pin lights turn with the scanner independently of the chassis.
    private static void DetailHead(Node3D head, Material armour, Material joints, Material edge,
        Material recess, Material accent, Material marking)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Part(head, $"JawShield{side}", Box, new(side * 1.87f, -0.33f, -1.04f),
                new(0.43f, 0.64f, 0.39f), armour).Rotation = new Vector3(0, 0, -side * 0.12f);
            Part(head, $"HeadTempleArmour{side}", Box, new(side * 1.83f, 0.18f, -0.24f),
                new(0.34f, 0.74f, 1.73f), joints);
            Part(head, $"HeadPowerDot{side}", Joint, new(side * 1.73f, 0.13f, -1.13f),
                new(0.17f, 0.17f, 0.10f), accent);
            Part(head, $"HeadMark{side}", Box, new(side * 1.45f, 0.64f, -0.83f),
                new(0.35f, 0.035f, 0.33f), marking);
        }
        Part(head, "ReinforcedBrowLip", Box, new(0, 0.50f, -1.18f),
            new(3.38f, 0.20f, 0.34f), armour);
        Part(head, "LowerOpticGuard", Box, new(0, -0.43f, -1.08f),
            new(2.30f, 0.17f, 0.30f), edge);
        Part(head, "HeadDorsalRail", Box, new(0, 0.71f, 0.34f),
            new(1.40f, 0.21f, 1.32f), recess);
        Part(head, "HeadDorsalFin", Box, new(0, 0.91f, 0.36f),
            new(0.24f, 0.25f, 1.21f), joints);
    }

    // =========================================================
    // Sensor pods remain non-weapon visual hardware with armour shutters, bolts and tiny status accents.
    private static void DetailPod(Node3D pod, int side, Material armour, Material edge,
        Material recess, Material steel, Material accent)
    {
        Part(pod, "PodUpperCowl", Box, new(0, 0.88f, -0.45f),
            new(2.72f, 0.24f, 2.39f), armour).Rotation = new Vector3(-0.07f, 0, 0);
        Part(pod, "PodLowerJaw", Box, new(0, -0.71f, -0.60f),
            new(2.56f, 0.24f, 2.26f), edge);
        for (int x = -1; x <= 1; x += 2)
        {
            Part(pod, $"PodFrontGuard{x}", Box, new(x * 1.28f, 0.06f, -1.73f),
                new(0.18f, 1.07f, 0.24f), armour);
            Part(pod, $"PodRearBolt{x}", Joint, new(x * 0.87f, 0.87f, 0.68f),
                new(0.19f, 0.10f, 0.19f), steel);
        }
        for (int fin = -2; fin <= 2; fin++)
            Part(pod, $"PodCoolingSlot{fin}", Box, new(fin * 0.38f, 0.91f, 0.75f),
                new(0.22f, 0.03f, 0.55f), recess);
        Part(pod, "PodStatusWindow", Box, new(side * 1.38f, 0.27f, -0.35f),
            new(0.07f, 0.12f, 0.65f), accent);
    }

    // =========================================================
    // Each leg's added armour, lines and pistons are children of its own animated hip/knee/foot.
    // Wires stay within one joint section, so normal walking cannot tear them away from the rig.
    private static void DetailLeg(Node3D hip, Node3D knee, Node3D foot, RobotSettings s,
        Material armour, Material joints, Material edge, Material recess,
        Material steel, Material cable, Material accent, Material marking)
    {
        float thigh = s.UpperLegLength, shin = s.LowerLegLength;
        for (int side = -1; side <= 1; side += 2)
        {
            Part(hip, $"ThighSideShield{side}", Box,
                new(side * 0.77f, 0.12f, -thigh * 0.42f),
                new(0.28f, 1.53f, thigh * 0.72f), armour)
                .Rotation = new Vector3(0.04f, 0, side * 0.10f);
            Part(hip, $"HipCoupler{side}", Joint, new(side * 0.76f, 0.02f, -0.38f),
                new(0.55f, 0.50f, 0.55f), steel);
            Cable(hip, $"ThighHydraulic{side}", steel, 0.11f,
                new(side * 0.93f, 0.04f, -0.65f),
                new(side * 0.96f, 0.02f, -thigh * 0.44f),
                new(side * 0.86f, 0.08f, -thigh * 0.80f));
            Cable(hip, $"ThighCable{side}", cable, 0.07f,
                new(side * 1.01f, 0.34f, -0.65f),
                new(side * 1.10f, 0.39f, -thigh * 0.44f),
                new(side * 0.98f, 0.32f, -thigh * 0.79f));

            Part(knee, $"KneeOuterPlate{side}", Box, new(side * 0.71f, 0.23f, -0.08f),
                new(0.28f, 0.84f, 1.30f), armour);
            Part(knee, $"KneeBolt{side}", Joint, new(side * 0.86f, 0.20f, -0.02f),
                new(0.27f, 0.27f, 0.27f), steel);
            Part(knee, $"ShinSideGuard{side}", Box, new(side * 0.52f, 0.24f, -shin * 0.53f),
                new(0.21f, 0.75f, shin * 0.68f), armour);
            Cable(knee, $"ShinPowerLine{side}", cable, 0.075f,
                new(side * 0.65f, 0.43f, -shin * 0.17f),
                new(side * 0.75f, 0.39f, -shin * 0.57f),
                new(side * 0.60f, 0.32f, -shin * 0.87f));

            Part(foot, $"HeelAnvil{side}", Box, new(side * 0.66f, 0.20f, 0.70f),
                new(0.45f, 0.40f, 0.76f), joints);
            Part(foot, $"ToeTipArmour{side}", Box, new(side * 0.57f, 0.28f, -2.11f),
                new(0.53f, 0.24f, 0.38f), armour);
            Part(foot, $"FootSideIdentifier{side}", Box, new(side * 1.02f, 0.41f, -0.20f),
                new(0.055f, 0.13f, 0.55f), marking);
        }
        Part(hip, "ThighFrontBracing", Box, new(0, 0.78f, -thigh * 0.47f),
            new(1.30f, 0.20f, thigh * 0.56f), edge);
        Part(hip, "ThighShieldStencil", Box, new(0, 0.91f, -thigh * 0.40f),
            new(0.62f, 0.035f, 0.13f), marking);
        Part(knee, "KneeForwardRam", Box, new(0, 0.67f, -0.18f),
            new(1.26f, 0.36f, 0.80f), joints);
        Part(knee, "ShinArmourKeel", Box, new(0, 0.83f, -shin * 0.61f),
            new(0.48f, 0.22f, shin * 0.52f), edge);
        Part(knee, "KneeCyanIndicator", Box, new(0, 0.91f, -0.38f),
            new(0.27f, 0.035f, 0.13f), accent);
        Part(foot, "FootFrontRam", Box, new(0, 0.63f, -0.96f),
            new(1.54f, 0.27f, 1.11f), armour);
        Part(foot, "FootHeelSpine", Box, new(0, 0.76f, 0.43f),
            new(0.56f, 0.18f, 1.15f), edge);
    }

    // =========================================================
    // Static short cable segments share one cached cylinder mesh, their parent rig part and one material.
    // The curve is authored with fixed points; no procedural physics/spring simulation or per-frame work.
    private static void Cable(Node3D parent, string name, Material material,
        float radius, params Vector3[] points)
    {
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 delta = points[i] - points[i - 1];
            float length = delta.Length();
            if (length < 0.001f) continue;
            var piece = Part(parent, $"{name}_{i}", CableMesh,
                (points[i] + points[i - 1]) * 0.5f,
                new Vector3(radius * 2f, length, radius * 2f), material);
            piece.Quaternion = new Quaternion(Vector3.Up, delta / length);
        }
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
