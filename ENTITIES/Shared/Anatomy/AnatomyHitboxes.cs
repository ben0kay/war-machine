// One non-monitoring area per participating actor; cached shape owners follow its existing visual pose.
using Godot;
using System.Collections.Generic;

public partial class AnatomyHitboxes : Area3D
{
    private readonly struct Part
    {
        public readonly uint Owner;
        public readonly Node3D Pivot;
        public readonly Transform3D Offset;
        public Part(uint owner, Node3D pivot, Vector3 centre)
        { Owner = owner; Pivot = pivot; Offset = new Transform3D(Basis.Identity, centre); }
    }
    private readonly List<Part> _parts = new();
    private readonly Dictionary<uint, BodyRegion> _regions = new();
    private static readonly Dictionary<AnatomyHitboxes, Rid> Bodies = new();
    public static ulong RegistryVersion { get; private set; }
    public AnatomySettings RuntimeSettings { get; private set; } = null!;

    // =========================================================
    // Restore registration when the same actor is detached and re-entered without rebuilding its art.
    public override void _EnterTree()
    {
        if (GetParent() is CollisionObject3D body)
        { Bodies[this] = body.GetRid(); RegistryVersion++; }
    }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Existing actor layers remain unchanged. Firearms substitute these shapes for registered capsules only.
    public void Initialize(CollisionObject3D body, AnatomySettings settings)
    {
        RuntimeSettings = (AnatomySettings)settings.Duplicate(true);
        CollisionLayer = body.CollisionLayer; CollisionMask = 0;
        Monitoring = false; Monitorable = false; InputRayPickable = false;
        Bodies[this] = body.GetRid(); RegistryVersion++;
    }

    // =========================================================
    // Build one conservative box around this pivot's visible meshes, excluding separately supplied child pivots.
    public void AddRegion(Node3D pivot, BodyRegion region, HashSet<Node3D> boundaries)
    {
        Aabb bounds = default; bool found = false;
        Gather(pivot, pivot, boundaries, ref bounds, ref found);
        if (!found) return;
        float padding = float.IsFinite(RuntimeSettings.Padding) ? Mathf.Clamp(RuntimeSettings.Padding, 0, 0.02f) : 0;
        Vector3 size = bounds.Size + Vector3.One * padding * 2;
        uint owner = CreateShapeOwner(pivot);
        ShapeOwnerAddShape(owner, new BoxShape3D { Size = size, Margin = 0.001f });
        _regions[owner] = region;
        _parts.Add(new Part(owner, pivot, bounds.GetCenter()));
    }

    // =========================================================
    private static void Gather(Node3D node, Node3D pivot, HashSet<Node3D> boundaries, ref Aabb bounds, ref bool found)
    {
        if (node != pivot && boundaries.Contains(node)) return;
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            Aabb box = (pivot.GlobalTransform.AffineInverse() * mesh.GlobalTransform) * mesh.GetAabb();
            bounds = found ? bounds.Merge(box) : box; found = true;
        }
        foreach (Node child in node.GetChildren())
            if (child is Node3D spatial) Gather(spatial, pivot, boundaries, ref bounds, ref found);
    }

    // =========================================================
    // Called by the owner's existing pose/movement lifecycle, never an independent frame callback.
    public void SyncPose()
    {
        Transform3D inverse = GlobalTransform.AffineInverse();
        foreach (Part part in _parts)
            ShapeOwnerSetTransform(part.Owner, inverse * part.Pivot.GlobalTransform * part.Offset);
    }

    // =========================================================
    // Remove every physical query shape for a severed region, including thigh and shin owners.
    public void DisableRegion(BodyRegion region)
    {
        foreach (var entry in _regions)
            if (entry.Value == region) ShapeOwnerSetDisabled(entry.Key, true);
    }

    // =========================================================
    // Explicit actor reset/reuse restores hurtboxes without recreating geometry.
    public void RestoreRegions(uint layer)
    {
        foreach (var entry in _regions) ShapeOwnerSetDisabled(entry.Key, false);
        CollisionLayer = layer;
    }

    // =========================================================
    public BodyRegion RegionForShape(int shape)
        => shape >= 0 && _regions.TryGetValue(ShapeFindOwner(shape), out BodyRegion region) ? region : BodyRegion.Unknown;

    // =========================================================
    // Rebuilt only when registration changes or a firearm changes holder, with no scene-tree scans.
    public static void ConfigureShotQuery(PhysicsRayQueryParameters3D query, CollisionObject3D holder)
    {
        var exclude = new Godot.Collections.Array<Rid> { holder.GetRid() };
        foreach (var entry in Bodies)
        {
            exclude.Add(entry.Value);
            if (entry.Value == holder.GetRid()) exclude.Add(entry.Key.GetRid());
        }
        query.Exclude = exclude; query.CollideWithAreas = true;
    }

    // =========================================================
    public void Disable() { CollisionLayer = 0; }

    // =========================================================
    public override void _ExitTree()
    { if (Bodies.Remove(this)) RegistryVersion++; }
}
