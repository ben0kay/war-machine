// Reusable outside-building reveal: one cached bounds provider, target opacity and event-driven occupancy.
// Existing section owners remain the sole mesh writers. No physics query, frame loop or mesh scan at runtime.
using Godot;
using System.Collections.Generic;

public partial class BuildingObstructionVisibility : Node3D, ICameraOcclusionProvider, ICameraFadeTarget, ICameraOpacityOverride
{
    public event System.Action? OcclusionChanged;
    public bool IsValid => _initialized && IsInsideTree() && !IsQueuedForDeletion();
    public float ObstructedOpacity { get; private set; }
    private float _obstructionRadius;
    private Node3D _building = null!;
    private PlayerController _player = null!;
    private Camera _camera = null!;
    private readonly List<CameraOccluder> _sections = new();
    private Aabb _bounds;
    private readonly struct Part
    {
        public readonly MeshInstance3D Mesh;
        public readonly Transform3D ToMesh;
        public readonly Aabb Bounds;
        public readonly float RadiusScale;
        public Part(MeshInstance3D mesh, Transform3D toMesh)
        {
            Mesh = mesh; ToMesh = toMesh; Bounds = mesh.GetAabb();
            RadiusScale = Mathf.Max(toMesh.Basis.X.Length(), Mathf.Max(toMesh.Basis.Y.Length(), toMesh.Basis.Z.Length()));
        }
    }
    private readonly List<Part> _parts = new();
    private Aabb[] _occupiedZones = System.Array.Empty<Aabb>();
    private System.Func<bool>? _isInsideFloor;
    private bool _inside, _initialized, _occupancySet;

    // =========================================================
    // World supplies per-prefab local occupancy volumes and a copied authoring opacity, not building-name rules.
    public void Initialize(Node3D building, PlayerController player, Camera camera, float opacity, float obstructionRadius, Aabb[] zones, System.Func<bool>? isInsideFloor = null)
    {
        _building = building; _player = player; _camera = camera; ObstructedOpacity = Mathf.Clamp(opacity, 0, 1);
        _obstructionRadius = Mathf.Clamp(obstructionRadius, 0f, 1f);
        _occupiedZones = zones; _isInsideFloor = isInsideFloor;
        CacheSections(building);
        // Cache uncovered art in one passive owner, excluding all meshes already controlled by section/floor owners.
        var remaining = new CameraOccluder { Name = "RemainingVisuals", ObjectRoot = building, CreateObstructionProxy = false };
        foreach (var section in _sections) foreach (var mesh in section.Meshes) remaining.ExcludedMeshes.Add(mesh);
        AddChild(remaining); remaining.InitializeBounds(); _sections.Add(remaining);
        bool first = true;
        foreach (var section in _sections) foreach (var mesh in section.Meshes)
        {
            var bounds = CameraOcclusionMath.TransformBounds(mesh.GetAabb(), building.GlobalTransform.AffineInverse() * mesh.GlobalTransform);
            _bounds = first ? bounds : _bounds.Merge(bounds); first = false;
            _parts.Add(new Part(mesh, mesh.GlobalTransform.AffineInverse() * building.GlobalTransform));
        }
        SetProcess(false); SetPhysicsProcess(false);
        _initialized = !first; _player.PositionChanged += OnPlayerMoved;
        OnPlayerMoved(player.GlobalPosition); SetNotifyTransform(true);
    }

    // =========================================================
    // Startup traversal only. Keep entities and nested building-visibility owners out of the cached art set.
    private void CacheSections(Node root)
    {
        if (root == this || root is CharacterBody3D) return;
        if (root is CameraOccluder section) { section.InitializeBounds(); _sections.Add(section); return; }
        foreach (Node child in root.GetChildren()) CacheSections(child);
    }

    // =========================================================
    // Footprint/landing changes are event-driven; the lodge supplies its existing floor occupancy with hysteresis.
    private void OnPlayerMoved(Vector3 position)
    {
        Vector3 local = _building.ToLocal(position);
        bool inside = _isInsideFloor?.Invoke() ?? false;
        if (_isInsideFloor == null)
            foreach (var zone in _occupiedZones)
                if (zone.Grow(_inside ? 0.15f : -0.05f).HasPoint(local)) { inside = true; break; }
        if (_occupancySet && inside == _inside) return;
        _inside = inside; _occupancySet = true;
        foreach (var section in _sections) section.SetWholeBuildingMode(!inside);
        OcclusionChanged?.Invoke();
    }

    // =========================================================
    // OV1-A: a single conservative torso corridor rejects distant buildings first. A whole building
    // only fades if all five silhouette points are obstructed; corner grazing should stay opaque.
    // Uses cached visible mesh bounds, not physics queries. Geometry-accurate hits remain OV1-B.
    public void CollectObstructions(Vector3 from, Vector3 to, float radius, HashSet<ICameraFadeTarget> results)
    {
        if (!IsValid || _inside) return;
        var inverse = _building.GlobalTransform.AffineInverse();
        float localRadius = Mathf.Min(radius, _obstructionRadius) * Mathf.Max(inverse.Basis.X.Length(), Mathf.Max(inverse.Basis.Y.Length(), inverse.Basis.Z.Length()));
        if (!CameraOcclusionMath.Intersects(inverse * from, inverse * to, _bounds, localRadius)) return;

        Vector3 torso = _player.PerceptionTorso;
        Vector3 right = _camera.GlobalBasis.X;
        right.Y = 0;
        right = right.Normalized() * 0.24f;
        Vector3 lowerBody = _player.GlobalPosition + Vector3.Up * (_player.CapsuleHeight * 0.3f);
        if (!ObstructsSample(_player.PerceptionHead, inverse, localRadius)
            || !ObstructsSample(torso - right, inverse, localRadius)
            || !ObstructsSample(torso + right, inverse, localRadius)
            || !ObstructsSample(torso, inverse, localRadius)
            || !ObstructsSample(lowerBody, inverse, localRadius)) return;
        results.Add(this);
    }

    // =========================================================
    // Camera-projected rays work for both perspective and orthographic views. Exit at the first
    // unblocked sample; no allocations, per-building timer, geometry traversal or material writes.
    private bool ObstructsSample(Vector3 sample, Transform3D inverse, float localRadius)
    {
        Vector3 from = inverse * _camera.ProjectRayOrigin(_camera.UnprojectPosition(sample));
        Vector3 to = inverse * sample;
        if (!CameraOcclusionMath.Intersects(from, to, _bounds, localRadius)) return false;
        foreach (var part in _parts)
        {
            if (!GodotObject.IsInstanceValid(part.Mesh) || !part.Mesh.IsVisibleInTree()) continue;
            if (CameraOcclusionMath.Intersects(part.ToMesh * from, part.ToMesh * to,
                part.Bounds, localRadius * part.RadiusScale)) return true;
        }
        return false;
    }

    // =========================================================
    // CameraOcclusion interpolates toward the dedicated target opacity or 1, including smooth restoration on entry.
    public void SetCameraOpacity(float opacity)
    { foreach (var section in _sections) if (GodotObject.IsInstanceValid(section)) section.SetBuildingOpacity(opacity); }

    // =========================================================
    public override void _Notification(int what)
    { if (_initialized && what == NotificationTransformChanged) { OnPlayerMoved(_player.GlobalPosition); OcclusionChanged?.Invoke(); } }

    // =========================================================
    public void Shutdown()
    {
        if (!_initialized) return;
        if (GodotObject.IsInstanceValid(_player)) _player.PositionChanged -= OnPlayerMoved;
        SetCameraOpacity(1);
        foreach (var section in _sections) if (GodotObject.IsInstanceValid(section)) section.SetWholeBuildingMode(false);
        _initialized = false;
    }
    // =========================================================
    public override void _ExitTree() => Shutdown();

}
