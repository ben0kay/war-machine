// SOC0: shared one-shot static collision generation for GLB placeables.
// Mode 1: combine imported mesh bounds into one cheap BoxShape3D.
// Mode 2: use authored native-metre boxes for open spaces (shelters, shelves, table legs).
// Never create concave/triangle mesh colliders or per-frame physics work.
using Godot;

public static class StaticObjectCollision
{
    // =========================================================
    // Runs once after PlaceableVisual applies visual size. The collider lives next
    // to Model, not underneath its nonuniform scale. Layer 1 blocks player,
    // zombies, hitscan projectiles and existing physical vision checks.
    public static void Build(PlaceableVisual owner, Node3D model, int mode,
        Godot.Collections.Array<StaticCollisionBox> boxes, Vector3 scale)
    {
        if (mode != 1 && mode != 2 || owner.GetNodeOrNull<StaticBody3D>("StaticCollision") != null) return;
        if (mode == 2 && (boxes == null || boxes.Count == 0))
        { GD.PushWarning($"{owner.Name}: CustomBoxes selected without boxes; object is noncolliding."); return; }

        Aabb bounds = default;
        bool found = false;
        if (mode == 1) CollectBounds(owner, model, ref bounds, ref found);
        if (mode == 1 && !found)
        { GD.PushWarning($"{owner.Name}: no GLB MeshInstance3D bounds for automatic collision."); return; }

        // For authored custom boxes, the body inherits only the model's ORIENTATION
        // and offset; each shape has its own explicit nonuniformly scaled dimensions.
        var body = new StaticBody3D { Name = "StaticCollision", CollisionLayer = 1u, CollisionMask = 1u };
        if (mode == 2) body.Transform = new Transform3D(model.Transform.Basis.Orthonormalized(), model.Position);
        owner.AddChild(body);

        if (mode == 1) AddBox(body, "AutoBounds", bounds.GetCenter(), bounds.Size);
        else
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                StaticCollisionBox? entry = boxes[i];
                if (entry == null) continue;
                if (!Finite(entry.Center) || !Finite(entry.Size) || entry.Size.X <= 0
                    || entry.Size.Y <= 0 || entry.Size.Z <= 0) continue;
                AddBox(body, $"Box_{i}", entry.Center * scale, entry.Size * scale);
            }
            if (body.GetChildCount() == 0)
            { GD.PushWarning($"{owner.Name}: no valid custom collision boxes."); body.QueueFree(); }
        }
    }

    // =========================================================
    // Scan only visual children of the shared Model once. Imported GLB meshes
    // may have nested pivots or offsets; transform all eight AABB corners into
    // the scene-root space AFTER the size master is applied.
    private static void CollectBounds(Node3D owner, Node node, ref Aabb bounds, ref bool found)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            Aabb local = mesh.GetAabb();
            Vector3 from = local.Position, to = local.Position + local.Size;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new(x == 0 ? from.X : to.X, y == 0 ? from.Y : to.Y, z == 0 ? from.Z : to.Z);
                Vector3 point = owner.ToLocal(mesh.ToGlobal(corner));
                if (!Finite(point)) continue;
                if (!found) { bounds = new Aabb(point, Vector3.Zero); found = true; }
                else bounds = bounds.Expand(point);
            }
        }
        foreach (Node child in node.GetChildren()) CollectBounds(owner, child, ref bounds, ref found);
    }

    // =========================================================
    // =========================================================
    private static void AddBox(StaticBody3D body, string name, Vector3 centre, Vector3 size)
    {
        var shape = new BoxShape3D { Size = new Vector3(
            Mathf.Max(0.03f, size.X), Mathf.Max(0.03f, size.Y), Mathf.Max(0.03f, size.Z)) };
        body.AddChild(new CollisionShape3D { Name = name, Shape = shape, Position = centre });
    }

    // =========================================================
    private static bool Finite(Vector3 value)
        => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
