// One-time structure authoring guard: protected doors, windows, stairs and circulation volumes.
// Reservations stay under Generated for future prefab/GLB placement passes; no gameplay processing.
using Godot;
using System.Collections.Generic;

public sealed class StructureClearance
{
    public const string ReservedMeta = "structure_reserved_clearance";
    private readonly Node3D _root;
    private readonly List<(string Name, Aabb Bounds)> _volumes = new();

    // =========================================================
    // Every volume is expressed in the owning building's local coordinates.
    public StructureClearance(Node3D generated)
    { _root = StructureGeometry.Group(generated, "ClearanceVolumes"); }

    // =========================================================
    // Preserve a named authored reservation so later GLB/item placement can honor the same layout.
    public void Reserve(string name, Vector3 center, Vector3 size)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0) return;
        var bounds = new Aabb(center - size * 0.5f, size);
        var marker = new Node3D { Name = name };
        marker.SetMeta(ReservedMeta, bounds); _root.AddChild(marker);
        _volumes.Add((name, bounds));
    }

    // =========================================================
    // Check the entire intended fixture's box, not just its pivot. Only authoring/spawn calls this.
    public bool Allows(string name, Vector3 center, Vector3 size)
    {
        var bounds = new Aabb(center - size * 0.5f, size);
        foreach (var area in _volumes)
            if (bounds.Intersects(area.Bounds))
            {
                GD.PushWarning($"Structure fixture '{name}' intersects reserved clearance '{area.Name}'; skipped.");
                return false;
            }
        return true;
    }

    // =========================================================
    // Optional future prefabs/GLB placement can reuse existing reservations, with no new runtime scan.
    public static bool AllowsPlacement(Node3D clearanceRoot, Aabb proposed)
    {
        foreach (Node child in clearanceRoot.GetChildren())
            if (child.HasMeta(ReservedMeta) && proposed.Intersects(child.GetMeta(ReservedMeta).AsAabb()))
                return false;
        return true;
    }
}
