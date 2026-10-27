// Rifle_001 visual pass: compact carbine silhouette with beveled receiver, shaped stock, true barrel,
// upper rail, sights, ejection port, magazine, trigger guard and inset handguard details.
// Shape only: keep original grip origin, -Z muzzle marker and existing flat material palette.
using Godot;

public partial class RifleMeshBuilder : Node3D
{
    private static readonly StandardMaterial3D Metal = new()
    { AlbedoColor = new Color(0.12f, 0.16f, 0.18f), Metallic = 0.65f, Roughness = 0.55f };
    private static readonly StandardMaterial3D Polymer = new()
    { AlbedoColor = new Color(0.26f, 0.30f, 0.24f), Roughness = 0.9f };
    private static readonly StandardMaterial3D Recess = new()
    { AlbedoColor = new Color(0.075f, 0.095f, 0.10f), Metallic = 0.25f, Roughness = 0.72f };

    // =========================================================
    // One-time replacement for the former nine primitive boxes; all geometry stays near original bounds.
    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Generated") != null) return;
        var art = new Node3D { Name = "Generated" }; AddChild(art);

        // Rear stock has a narrow neck, widening toward the shooter's shoulder, not a floating rectangle.
        Part(art, "StockCore", new(0, 0.01f, 0.231f), new(0.087f, 0.12f, 0.278f), Polymer,
            WeaponVisualGeometry.TaperForward);
        Part(art, "StockUpperComb", new(0, 0.079f, 0.245f), new(0.079f, 0.034f, 0.227f), Recess);
        Part(art, "StockHeel", new(0, -0.047f, 0.280f), new(0.073f, 0.028f, 0.172f), Polymer);
        Part(art, "ButtPad", new(0, 0.014f, 0.362f), new(0.103f, 0.151f, 0.023f), Recess);
        Part(art, "StockNeck", new(0, 0.018f, 0.080f), new(0.068f, 0.078f, 0.129f), Metal);

        // Angled pistol grip and an open three-sided trigger guard under the actual receiver.
        Part(art, "PistolGrip", new(0, -0.112f, 0.016f), new(0.075f, 0.190f, 0.082f), Polymer,
            WeaponVisualGeometry.TaperRear, new Vector3(-0.22f, 0, 0));
        Part(art, "GripBackstrap", new(0, -0.143f, 0.054f), new(0.060f, 0.110f, 0.017f), Recess);
        Part(art, "TriggerGuardFront", new(0, -0.120f, -0.100f), new(0.015f, 0.082f, 0.014f), Metal);
        Part(art, "TriggerGuardBottom", new(0, -0.161f, -0.045f), new(0.019f, 0.016f, 0.125f), Metal);
        Part(art, "Trigger", new(0, -0.116f, -0.021f), new(0.014f, 0.053f, 0.015f), Recess,
            WeaponVisualGeometry.Box, new Vector3(-0.13f, 0, 0));

        // Distinct upper and lower receivers give depth and believable mechanical seams from first person.
        Part(art, "UpperReceiver", new(0, 0.069f, -0.157f), new(0.100f, 0.088f, 0.322f), Metal);
        Part(art, "LowerReceiver", new(0, -0.013f, -0.126f), new(0.095f, 0.076f, 0.279f), Metal);
        Part(art, "ReceiverSideCoverL", new(-0.053f, 0.029f, -0.149f),
            new(0.008f, 0.071f, 0.229f), Polymer);
        Part(art, "EjectionPort", new(0.054f, 0.072f, -0.184f), new(0.006f, 0.038f, 0.115f), Recess,
            WeaponVisualGeometry.Box);
        Part(art, "BoltCarrier", new(0.059f, 0.073f, -0.180f), new(0.008f, 0.019f, 0.069f), Metal);
        Tube(art, "ChargingKnob", new(0.072f, 0.035f, 0.019f), 0.035f, 0.036f, Recess);
        Part(art, "SafetySelector", new(-0.059f, -0.008f, 0.009f), new(0.018f, 0.020f, 0.030f), Recess);
        for (int i = 0; i < 2; i++)
            Part(art, "ReceiverPin", new(-0.060f, -0.021f, -0.065f - i * 0.123f),
                new(0.011f, 0.015f, 0.015f), Recess);

        // A detachable, gently swept magazine; the well is a distinct solid above it.
        Part(art, "MagazineWell", new(0, -0.063f, -0.177f), new(0.078f, 0.070f, 0.142f), Metal);
        Part(art, "Magazine", new(0, -0.148f, -0.182f), new(0.068f, 0.170f, 0.113f), Polymer,
            WeaponVisualGeometry.TaperRear, new Vector3(-0.12f, 0, 0));
        Part(art, "MagazineFloorplate", new(0, -0.231f, -0.171f),
            new(0.075f, 0.020f, 0.118f), Metal);
        for (int i = 0; i < 3; i++)
            Part(art, "MagazineRib", new(0.036f, -0.099f - i * 0.048f, -0.174f),
                new(0.006f, 0.013f, 0.092f), Recess);

        // Compact handguard with top rail and recessed cooling/attachment slots (not painted texture lines).
        Part(art, "ForeEnd", new(0, 0.045f, -0.437f), new(0.105f, 0.102f, 0.261f), Polymer,
            WeaponVisualGeometry.TaperRear);
        Part(art, "Underside", new(0, -0.017f, -0.432f), new(0.074f, 0.018f, 0.227f), Recess);
        foreach (float x in new[] { -0.057f, 0.057f })
            for (int i = 0; i < 3; i++)
                Part(art, "ForeEndSlot", new(x, 0.056f, -0.346f - i * 0.084f),
                    new(0.007f, 0.026f, 0.052f), Recess, WeaponVisualGeometry.Box);
        Part(art, "TopRail", new(0, 0.106f, -0.312f), new(0.061f, 0.021f, 0.490f), Recess,
            WeaponVisualGeometry.Box);
        for (int i = 0; i < 9; i++)
            Part(art, "RailCrossbar", new(0, 0.122f, -0.075f - i * 0.052f),
                new(0.067f, 0.011f, 0.012f), Metal, WeaponVisualGeometry.Box);

        // Real round barrel with a sleeve, short muzzle device and a dark inset bore aligned to Muzzle marker.
        Tube(art, "Barrel", new(0, 0.055f, -0.647f), 0.046f, 0.261f, Metal);
        Tube(art, "GasBlock", new(0, 0.055f, -0.612f), 0.067f, 0.065f, Recess);
        Tube(art, "MuzzleDevice", new(0, 0.055f, -0.773f), 0.059f, 0.066f, Metal);
        Tube(art, "Bore", new(0, 0.055f, -0.808f), 0.026f, 0.005f, Recess);
        // Sights have side ears and a central post rather than oversized single cubes.
        for (int side = -1; side <= 1; side += 2)
        {
            Part(art, "RearSightEar", new(side * 0.029f, 0.155f, -0.061f),
                new(0.012f, 0.053f, 0.032f), Recess);
            Part(art, "FrontSightEar", new(side * 0.022f, 0.133f, -0.709f),
                new(0.012f, 0.063f, 0.027f), Metal);
        }
        Part(art, "RearSightBridge", new(0, 0.183f, -0.061f),
            new(0.070f, 0.010f, 0.026f), Metal, WeaponVisualGeometry.Box);
        Part(art, "FrontSightPost", new(0, 0.142f, -0.709f),
            new(0.009f, 0.058f, 0.012f), Recess, WeaponVisualGeometry.Box);

        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Reuse shared low-poly shapes; only the weapon-specific placement defines the model.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size,
        Material mat, Mesh? shape = null, Vector3? rotation = null)
        => WeaponVisualGeometry.Part(parent, name, position, size, mat, shape, rotation);

    private static void Tube(Node3D parent, string name, Vector3 position, float diameter, float length, Material mat)
        => WeaponVisualGeometry.Tube(parent, name, position, diameter, length, mat);
}
