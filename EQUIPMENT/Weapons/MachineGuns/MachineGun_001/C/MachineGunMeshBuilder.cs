// Original placeholder low-poly light machine gun, built once. All firing logic lives in FirearmController.
using Godot;

public partial class MachineGunMeshBuilder : Node3D
{
    private static readonly BoxMesh Cube = new() { Size = Vector3.One };

    // =========================================================
    // Weapon mount is at the grip; barrel/muzzle point along local -Z like Rifle_001.
    // Mesh is purely visual and can be swapped for an imported Artwork/ scene.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        var art = new Node3D { Name = "Generated" }; AddChild(art);
        var gunmetal = new StandardMaterial3D { AlbedoColor = new Color(0.13f, 0.18f, 0.18f),
            Metallic = 0.55f, Roughness = 0.54f };
        var dark = new StandardMaterial3D { AlbedoColor = new Color(0.07f, 0.085f, 0.09f),
            Metallic = 0.35f, Roughness = 0.72f };
        var stock = new StandardMaterial3D { AlbedoColor = new Color(0.29f, 0.34f, 0.27f),
            Roughness = 0.88f };
        var accent = new StandardMaterial3D { AlbedoColor = new Color(0.40f, 0.44f, 0.27f),
            Metallic = 0.30f, Roughness = 0.65f };
        var brass = new StandardMaterial3D { AlbedoColor = new Color(0.60f, 0.45f, 0.23f),
            Metallic = 0.65f, Roughness = 0.48f };

        // The rear half: broad stock with cheek rest, butt pad and pistol grip.
        Part(art, "Buttstock", new(0, 0.005f, 0.32f), new(0.16f, 0.15f, 0.43f), stock);
        Part(art, "ButtPad", new(0, 0.01f, 0.57f), new(0.19f, 0.23f, 0.09f), dark);
        Part(art, "CheekRest", new(0, 0.102f, 0.28f), new(0.14f, 0.055f, 0.30f), dark);
        Part(art, "PistolGrip", new(0, -0.145f, 0.065f), new(0.102f, 0.27f, 0.13f), dark);
        Part(art, "TriggerHousing", new(0, -0.075f, -0.08f), new(0.14f, 0.09f, 0.14f), gunmetal);

        // Heavier, longer receiver and a side-mounted ammunition box make the silhouette
        // instantly different from the small Rifle_001, particularly from overhead.
        Part(art, "Receiver", new(0, 0.055f, -0.18f), new(0.22f, 0.19f, 0.52f), gunmetal);
        Part(art, "ReceiverTop", new(0, 0.158f, -0.17f), new(0.17f, 0.037f, 0.44f), dark);
        Part(art, "BoltCover", new(-0.118f, 0.060f, -0.20f), new(0.033f, 0.11f, 0.26f), accent);
        Part(art, "AmmunitionBox", new(0.20f, -0.096f, -0.15f), new(0.23f, 0.25f, 0.35f), stock);
        Part(art, "AmmoBoxLip", new(0.20f, 0.041f, -0.15f), new(0.25f, 0.034f, 0.37f), dark);
        Part(art, "FeedChute", new(0.14f, 0.048f, -0.15f), new(0.14f, 0.08f, 0.20f), gunmetal);
        for (int i = 0; i < 5; i++)
            Part(art, "VisibleFeedRound", new(0.13f + i * 0.016f, 0.093f, -0.27f + i * 0.043f),
                new(0.019f, 0.018f, 0.032f), brass);

        // Front shroud and external heavy barrel, including heat vents and muzzle brake.
        Part(art, "Handguard", new(0, 0.060f, -0.56f), new(0.21f, 0.16f, 0.35f), stock);
        Part(art, "ShroudTop", new(0, 0.150f, -0.56f), new(0.20f, 0.033f, 0.31f), dark);
        foreach (float x in new[] { -0.114f, 0.114f })
            for (int i = 0; i < 4; i++)
                Part(art, "CoolingVent", new(x, 0.068f, -0.44f - i * 0.075f),
                    new(0.018f, 0.036f, 0.045f), dark);
        Barrel(art, "HeavyBarrel", new(0, 0.06f, -0.91f), 0.038f, 0.50f, gunmetal);
        Barrel(art, "MuzzleBrake", new(0, 0.06f, -1.135f), 0.064f, 0.115f, dark);
        Part(art, "MuzzleBrakeBand", new(0, 0.06f, -1.16f),
            new(0.14f, 0.095f, 0.036f), gunmetal);

        // The raised carrying handle and folded bipod are low-cost identifiable details.
        Part(art, "CarryHandleFront", new(0, 0.245f, -0.36f), new(0.038f, 0.155f, 0.044f), dark);
        Part(art, "CarryHandleRear", new(0, 0.245f, -0.05f), new(0.038f, 0.155f, 0.044f), dark);
        Part(art, "CarryHandleTop", new(0, 0.326f, -0.205f), new(0.06f, 0.045f, 0.35f), accent);
        Part(art, "RearSight", new(0, 0.196f, 0.015f), new(0.105f, 0.09f, 0.04f), dark);
        Part(art, "FrontSight", new(0, 0.151f, -1.04f), new(0.07f, 0.15f, 0.035f), dark);
        foreach (float x in new[] { -0.09f, 0.09f })
        {
            Part(art, "FoldedBipodLeg", new(x, -0.081f, -0.77f),
                new(0.028f, 0.07f, 0.36f), gunmetal);
            Part(art, "BipodFoot", new(x, -0.122f, -0.93f),
                new(0.05f, 0.02f, 0.07f), dark);
        }
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    private static void Part(Node3D root, string name, Vector3 position, Vector3 size, Material material)
    { root.AddChild(new MeshInstance3D { Name = name, Position = position, Mesh = Cube, Scale = size,
        MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }); }

    // =========================================================
    // Cylinders run along local Y, so rotate 90 degrees to make a barrel along local -Z.
    private static void Barrel(Node3D root, string name, Vector3 position,
        float radius, float length, Material material)
    {
        root.AddChild(new MeshInstance3D { Name = name, Position = position,
            Rotation = new Vector3(Mathf.Pi * 0.5f, 0, 0),
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius,
                Height = length, RadialSegments = 10 },
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }
}
