// One broad native area by default; cached virtual limb boxes are resolved only by accepted attack queries.
using Godot;
using System.Collections.Generic;

public partial class AnatomyHitboxes : Area3D
{
    private sealed class Part
    {
        public readonly uint Owner;
        public readonly Node3D Pivot;
        public readonly Aabb Bounds;
        public readonly BodyRegion Region;
        public bool Enabled = true;
        public Part(uint owner, Node3D pivot, Aabb bounds, BodyRegion region)
        { Owner = owner; Pivot = pivot; Bounds = bounds; Region = region; }
    }
    private readonly List<Part> _parts = new();
    private readonly Dictionary<uint, BodyRegion> _regions = new();
    private static readonly Dictionary<AnatomyHitboxes, Rid> Bodies = new();
    public static ulong RegistryVersion { get; private set; }
    public static long PoseShapeUpdates { get; private set; }
    public static long VirtualBoxTests { get; private set; }
    private bool _deferred;
    public AnatomySettings RuntimeSettings { get; private set; } = null!;
    public bool RequiresPoseSync => !_deferred;
    public int PartCount => _parts.Count;

    // =========================================================
    public override void _EnterTree()
    {
        if (GetParent() is CollisionObject3D body)
        { Bodies[this] = body.GetRid(); RegistryVersion++; }
    }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // The broad sphere covers all authored standing/extended-arm/crawl poses without following individual joints.
    public void Initialize(CollisionObject3D body, AnatomySettings settings)
    {
        RuntimeSettings = (AnatomySettings)settings.Duplicate(true);
        _deferred = RuntimeSettings.DeferredHitDetection;
        CollisionLayer = body.CollisionLayer; CollisionMask = 0;
        Monitoring = false; Monitorable = false; InputRayPickable = false;
        Bodies[this] = body.GetRid(); RegistryVersion++;
        if (_deferred)
        {
            float radius = float.IsFinite(RuntimeSettings.BroadRadius) ? Mathf.Clamp(RuntimeSettings.BroadRadius, 0.5f, 10) : 1.6f;
            Vector3 centre = RuntimeSettings.BroadCentre.IsFinite() ? RuntimeSettings.BroadCentre : new Vector3(0, 0.8f, 0);
            uint owner = CreateShapeOwner(this);
            ShapeOwnerAddShape(owner, new SphereShape3D { Radius = radius, Margin = 0.001f });
            ShapeOwnerSetTransform(owner, new Transform3D(Basis.Identity, centre));
        }
    }

    // =========================================================
    // Cache local mesh bounds once. Physical boxes exist only for the explicit legacy comparison mode.
    public void AddRegion(Node3D pivot, BodyRegion region, HashSet<Node3D> boundaries)
    {
        Aabb bounds = default; bool found = false;
        Gather(pivot, pivot, boundaries, ref bounds, ref found);
        if (!found) return;
        float padding = float.IsFinite(RuntimeSettings.Padding) ? Mathf.Clamp(RuntimeSettings.Padding, 0, 0.02f) : 0;
        bounds = bounds.Grow(padding);
        uint owner = 0;
        if (RequiresPoseSync)
        {
            owner = CreateShapeOwner(pivot);
            ShapeOwnerAddShape(owner, new BoxShape3D { Size = bounds.Size, Margin = 0.001f });
            _regions[owner] = region;
        }
        _parts.Add(new Part(owner, pivot, bounds, region));
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
    // Comparison mode only. Default mode has no joint-transform reads/writes outside attacks.
    public void SyncPose()
    {
        if (!RequiresPoseSync) return;
        Transform3D inverse = GlobalTransform.AffineInverse();
        foreach (Part part in _parts)
        {
            if (!part.Enabled) continue;
            ShapeOwnerSetTransform(part.Owner, inverse * part.Pivot.GlobalTransform
                * new Transform3D(Basis.Identity, part.Bounds.GetCenter()));
            PoseShapeUpdates++;
        }
    }

    // =========================================================
    public void DisableRegion(BodyRegion region)
    {
        foreach (Part part in _parts)
            if (part.Region == region)
            { part.Enabled = false; if (RequiresPoseSync) ShapeOwnerSetDisabled(part.Owner, true); }
    }

    // =========================================================
    public void RestoreRegions(uint layer)
    {
        foreach (Part part in _parts)
        { part.Enabled = true; if (RequiresPoseSync) ShapeOwnerSetDisabled(part.Owner, false); }
        CollisionLayer = layer;
    }

    // =========================================================
    // Default mode receives virtual part indices from AnatomyRaycaster, never the native broad-sphere index.
    public BodyRegion RegionForShape(int shape)
    {
        if (!RequiresPoseSync)
            return shape >= 0 && shape < _parts.Count && _parts[shape].Enabled ? _parts[shape].Region : BodyRegion.Unknown;
        return shape >= 0 && _regions.TryGetValue(ShapeFindOwner(shape), out BodyRegion region) ? region : BodyRegion.Unknown;
    }

    // =========================================================
    public bool TryPartCentre(int index, out Vector3 centre)
    {
        centre = Vector3.Zero;
        if (index < 0 || index >= _parts.Count || !_parts[index].Enabled || CollisionLayer == 0) return false;
        Part part = _parts[index]; centre = part.Pivot.ToGlobal(part.Bounds.GetCenter());
        return centre.IsFinite();
    }

    // =========================================================
    // Transform the ray into current joint space on demand; preserve the segment fraction under rotation/scale.
    public bool TryRay(Vector3 from, Vector3 to, out float fraction, out Vector3 normal, out int index)
    {
        fraction = float.PositiveInfinity; normal = Vector3.Zero; index = -1;
        if (CollisionLayer == 0 || !IsInsideTree() || IsQueuedForDeletion()) return false;
        for (int i = 0; i < _parts.Count; i++)
        {
            Part part = _parts[i];
            if (!part.Enabled) continue;
            Transform3D inverse = part.Pivot.GlobalTransform.AffineInverse();
            VirtualAnatomyMath.SegmentBox(inverse * from, inverse * to, part.Bounds,
                out bool hit, out float t, out Vector3 localNormal);
            VirtualBoxTests++;
            if (!hit || t >= fraction) continue;
            fraction = t; index = i;
            normal = (inverse.Basis.Transposed() * localNormal).Normalized();
        }
        return index >= 0;
    }

    // =========================================================
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
    public static void ResetCounters() { PoseShapeUpdates = VirtualBoxTests = 0; AnatomyRaycaster.ResetCounters(); }

    // =========================================================
    // Explicit debug snapshot only; no counter display polling or always-running diagnostics.
    public static string Diagnostics()
    {
        int deferred = 0, legacy = 0, shapes = 0;
        foreach (var entry in Bodies)
        {
            if (!GodotObject.IsInstanceValid(entry.Key) || entry.Key.RuntimeSettings == null) continue;
            if (entry.Key.RequiresPoseSync) { legacy++; shapes += entry.Key.PartCount; }
            else { deferred++; shapes++; }
        }
        return $"[ELD-PERF1] deferred={deferred} legacy={legacy} native_anatomy_shapes={shapes} "
            + $"pose_shape_writes={PoseShapeUpdates} virtual_box_tests={VirtualBoxTests} "
            + $"native_attack_rays={AnatomyRaycaster.NativeQueries} budget_stops={AnatomyRaycaster.BudgetStops}";
    }

    // =========================================================
    public void Disable() { CollisionLayer = 0; }

    // =========================================================
    public override void _ExitTree() { if (Bodies.Remove(this)) RegistryVersion++; }
}
