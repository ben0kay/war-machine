// One-time procedural waist pivot for zombie/runner attack animation; keeps original global art placement.
using Godot;

public static class ZombieUpperBodyRig
{
    // Existing waist, torso, head and arms move together; legs and native collision stay rooted.
    private static readonly string[] UpperParts = {
        "SlouchedTorso", "LeanTorso", "ShoulderShape", "NarrowShoulders",
        "ExposedNeck", "BentNeck", "TornCollar", "ShirtHem", "TornShirtHem",
        "ShoulderTear", "ChestStain", "Hips", "Waist", "Head", "LeftArm", "RightArm"
    };

    // =========================================================
    // Build once per species before visual batching; cloned templates inherit the exact same pivot.
    public static void Attach(Node3D root)
    {
        var upper = new Node3D { Name = "UpperBody", Position = new Vector3(0f, 0.85f, 0f) };
        root.AddChild(upper);
        foreach (string name in UpperParts)
        {
            Node3D? part = root.GetNodeOrNull<Node3D>(name);
            if (part != null) part.Reparent(upper, true); // Keep authored world transforms unchanged.
        }
    }
}
