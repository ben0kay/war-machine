// Startup-only authoring metadata and cached layout shared by authored/imported/procedural structures.
using Godot;
using System.Collections.Generic;

public sealed class StructureLayout
{
    public sealed class Floor { public int Level; public float Elevation; }
    public sealed class Part { public Node3D Root = null!; public int Level; public string Role = ""; }
    public readonly List<Floor> Floors = new();
    public readonly List<Part> Parts = new();
    public readonly List<Aabb> Zones = new();

    // =========================================================
    // Bounds are local to the tagged node; floor elevations are local to the building root.
    public static void Occupied(Node3D node, Aabb bounds) => node.SetMeta("structure_occupied_bounds", bounds);

    // =========================================================
    public static void Visual(Node3D node, string role) => node.SetMeta("structure_visual_role", role);

    // =========================================================
    // OV-W1: a marked group (walls, doorframe, optional door, opening trim) fades together.
    // Group identity is the node itself, not a global building name or a merged mesh.
    public const string FadeGroupMeta = "structure_wall_fade_group";
    public static void FadeGroup(Node3D group) => group.SetMeta(FadeGroupMeta, true);

    // =========================================================
    public static void Level(Node3D node, int level, float elevation)
    { node.SetMeta("floor_level", level); node.SetMeta("floor_elevation", elevation); }

    // =========================================================
    public static StructureLayout Read(Node3D building)
    {
        var layout = new StructureLayout(); layout.Visit(building, building, 0);
        layout.Floors.Sort((a, b) => a.Elevation.CompareTo(b.Elevation));
        if (layout.Floors.Count == 0) layout.Floors.Add(new Floor { Level = 0, Elevation = 0 });
        return layout;
    }

    // =========================================================
    // Role subtrees must be disjoint; stop beneath a visual root so its meshes cannot acquire two fade owners.
    private void Visit(Node node, Node3D building, int level)
    {
        if (node is CharacterBody3D || node is CameraOccluder || node is StructureVisibility) return;
        if (node.HasMeta("floor_level") && node.HasMeta("floor_elevation"))
        {
            level = node.GetMeta("floor_level").AsInt32();
            Floors.Add(new Floor { Level = level, Elevation = node.GetMeta("floor_elevation").AsSingle() });
        }
        if (node is Node3D spatial && node.HasMeta("structure_occupied_bounds"))
            Zones.Add(CameraOcclusionMath.TransformBounds(node.GetMeta("structure_occupied_bounds").AsAabb(),
                building.GlobalTransform.AffineInverse() * spatial.GlobalTransform));
        if (node is Node3D visual && node.HasMeta("structure_visual_role"))
        {
            string role = node.GetMeta("structure_visual_role").AsString();
            if (role != "walls" && role != "roof" && role != "deck" && role != "contents" && role != "stairs")
                GD.PushWarning($"Unknown structure visual role '{role}'; treating as contents.");
            Parts.Add(new Part { Root = visual, Level = level, Role = role }); return;
        }
        foreach (Node child in node.GetChildren()) Visit(child, building, level);
    }
}
