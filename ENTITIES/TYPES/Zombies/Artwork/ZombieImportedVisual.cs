// Imported segmented zombie art follows the same pivot contract as the procedural rig.
// The procedural builder remains the fallback when the GLB is disabled or cannot be loaded.
using Godot;
using System;
using System.Collections.Generic;

public static class ZombieImportedVisual
{
    private const string Path = "res://ENTITIES/TYPES/Zombies/Artwork/ZombieFirstPass.glb";
    private static PackedScene? _scene;
    private static bool _attempted, _warnedRig, _loggedSuccess;

    // =========================================================
    // Import once; each zombie receives its own transform nodes while mesh/material resources stay shared.
    // Godot can add one or more scene-root wrappers when it imports a GLB, so search for the rig itself.
    public static bool TryPopulate(Node3D root)
    {
        if (!_attempted)
        {
            _attempted = true;
            if (ResourceLoader.Exists(Path)) _scene = GD.Load<PackedScene>(Path);
            if (_scene == null)
                GD.PushWarning($"[ZombieArtwork] Cannot load {Path}; procedural zombie artwork is being used. Check the Godot import.");
        }
        if (_scene == null) return false;

        Node imported = _scene.Instantiate();
        Node3D? rig = FindRig(imported);
        if (rig == null)
        {
            if (!_warnedRig)
            {
                _warnedRig = true;
                GD.PushWarning($"[ZombieArtwork] GLB loaded, but its required pivots were not found. "
                    + $"Using procedural artwork. Imported nodes: {DescribeHierarchy(imported)}");
            }
            imported.Free();
            return false;
        }

        // Transfer ONLY the three rig roots. The torso retains head, hips and arms; both legs
        // remain separately removable. Wrappers, lights and other import-only nodes are discarded.
        foreach (string name in new[] { "UpperBody", "LeftLeg", "RightLeg" })
        {
            Node3D limbRoot = rig.GetNode<Node3D>(name);
            rig.RemoveChild(limbRoot);
            root.AddChild(limbRoot);
        }
        imported.Free();

        if (!_loggedSuccess)
        {
            _loggedSuccess = true;
            GD.Print("[ZombieArtwork] ZombieFirstPass.glb active for standard zombies; procedural fallback preserved.");
        }
        return true;
    }

    // =========================================================
    // Match the exact gameplay joints even if Godot renamed repeated Elbow/Knee nodes on import.
    // All renaming is limited to a validated imported instance; the authored GLB is not modified.
    private static Node3D? FindRig(Node node)
    {
        if (node is Node3D candidate && NormalizeRig(candidate)) return candidate;
        foreach (Node child in node.GetChildren())
        {
            Node3D? found = FindRig(child);
            if (found != null) return found;
        }
        return null;
    }

    // =========================================================
    private static bool NormalizeRig(Node3D root)
    {
        Node3D? upper = FindJoint(root, "UpperBody");
        Node3D? leftLeg = FindJoint(root, "LeftLeg");
        Node3D? rightLeg = FindJoint(root, "RightLeg");
        if (upper == null || leftLeg == null || rightLeg == null) return false;

        Node3D? head = FindJoint(upper, "Head");
        Node3D? hips = FindJoint(upper, "Hips");
        Node3D? leftArm = FindJoint(upper, "LeftArm");
        Node3D? rightArm = FindJoint(upper, "RightArm");
        if (head == null || hips == null || leftArm == null || rightArm == null) return false;

        Node3D? leftElbow = FindJoint(leftArm, "Elbow");
        Node3D? rightElbow = FindJoint(rightArm, "Elbow");
        Node3D? leftKnee = FindJoint(leftLeg, "Knee");
        Node3D? rightKnee = FindJoint(rightLeg, "Knee");
        if (leftElbow == null || rightElbow == null || leftKnee == null || rightKnee == null) return false;

        // Canonical names are required by gait, anatomy, limb removal and corpse posing.
        CanonicalName(upper, "UpperBody");
        CanonicalName(leftLeg, "LeftLeg");
        CanonicalName(rightLeg, "RightLeg");
        CanonicalName(head, "Head");
        CanonicalName(hips, "Hips");
        CanonicalName(leftArm, "LeftArm");
        CanonicalName(rightArm, "RightArm");
        CanonicalName(leftElbow, "Elbow");
        CanonicalName(rightElbow, "Elbow");
        CanonicalName(leftKnee, "Knee");
        CanonicalName(rightKnee, "Knee");
        return true;
    }

    // =========================================================
    private static Node3D? FindJoint(Node parent, string expected)
    {
        Node3D? renamed = null;
        foreach (Node child in parent.GetChildren())
        {
            if (child is not Node3D joint) continue;
            string name = joint.Name.ToString();
            if (name == expected) return joint;
            if (renamed == null && IsImporterNumberSuffix(name, expected)) renamed = joint;
        }
        return renamed;
    }

    // Godot importers can disambiguate duplicate node names as Knee2, Knee_2 or Knee.002.
    private static bool IsImporterNumberSuffix(string name, string expected)
    {
        if (!name.StartsWith(expected, StringComparison.Ordinal) || name.Length <= expected.Length) return false;
        int index = expected.Length;
        if (name[index] == '_' || name[index] == '.' || name[index] == '-') index++;
        if (index >= name.Length) return false;
        for (; index < name.Length; index++)
            if (!char.IsDigit(name[index])) return false;
        return true;
    }

    private static void CanonicalName(Node3D node, string name)
    {
        if (node.Name.ToString() != name) node.Name = name;
    }

    // =========================================================
    // On a failed first import, print the imported paths once instead of silently hiding the cause.
    private static string DescribeHierarchy(Node root)
    {
        var names = new List<string>();
        CollectNames(root, "", names);
        return string.Join(", ", names);
    }

    private static void CollectNames(Node node, string prefix, List<string> names)
    {
        if (names.Count >= 36) return;
        string name = prefix.Length == 0 ? node.Name.ToString() : prefix + "/" + node.Name;
        names.Add(name);
        foreach (Node child in node.GetChildren())
            CollectNames(child, name, names);
    }
}
