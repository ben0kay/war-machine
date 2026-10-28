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
    private bool _budgetWarning;
    private const int TriangleBudget = 16384;
    private readonly struct Part
    {
        public readonly MeshInstance3D Mesh;
        public readonly Transform3D ToMesh;
        public readonly Aabb Bounds;
        public readonly Vector3[] Faces;
        public Part(MeshInstance3D mesh, Transform3D toMesh, Vector3[] faces)
        {
            Mesh = mesh; ToMesh = toMesh; Bounds = mesh.GetAabb();
            Faces = faces;
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
        var geometry = new Dictionary<Mesh, Vector3[]>();
        bool first = true;
        foreach (var section in _sections) foreach (var mesh in section.Meshes)
        {
            var bounds = CameraOcclusionMath.TransformBounds(mesh.GetAabb(), building.GlobalTransform.AffineInverse() * mesh.GlobalTransform);
            _bounds = first ? bounds : _bounds.Merge(bounds); first = false;
            if (!IsStructuralBlocker(mesh, building) || mesh.Mesh == null) continue;
            if (!geometry.TryGetValue(mesh.Mesh, out var faces))
            { faces = mesh.Mesh.GetFaces(); geometry.Add(mesh.Mesh, faces); }
            if (faces.Length == 0)
            { GD.PushWarning($"{mesh.GetPath()}: no triangle geometry for exterior visibility; leaving clear."); continue; }
            _parts.Add(new Part(mesh, mesh.GlobalTransform.AffineInverse() * building.GlobalTransform, faces));
        }
        if (_parts.Count == 0) GD.PushWarning($"{building.Name}: no opaque structural triangles; exterior whole-building fade remains off.");
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
    // OV1: bounds only reject candidates. All body samples must hit real structural triangles.
    // Cosmetic fading never removes these blockers, preventing fade/restore feedback loops.
    public void CollectObstructions(Vector3 from, Vector3 to, float radius, HashSet<ICameraFadeTarget> results)
    {
        if (!IsValid || _inside) return;
        var inverse = _building.GlobalTransform.AffineInverse();
        float localRadius = Mathf.Min(radius, _obstructionRadius) * Mathf.Max(inverse.Basis.X.Length(), Mathf.Max(inverse.Basis.Y.Length(), inverse.Basis.Z.Length()));

        Vector3 torso = _player.PerceptionTorso;
        Vector3 right = _camera.GlobalBasis.X;
        right.Y = 0;
        right = right.Normalized() * 0.24f;
        Vector3 lowerBody = _player.GlobalPosition + Vector3.Up * (_player.CapsuleHeight * 0.3f);
        int budget = TriangleBudget;
        if (!ObstructsSample(_player.PerceptionHead, inverse, localRadius, ref budget)
            || !ObstructsSample(torso - right, inverse, localRadius, ref budget)
            || !ObstructsSample(torso + right, inverse, localRadius, ref budget)
            || !ObstructsSample(torso, inverse, localRadius, ref budget)
            || !ObstructsSample(lowerBody, inverse, localRadius, ref budget)) return;
        results.Add(this);
    }

    // =========================================================
    // Camera-projected rays work for both perspective and orthographic views. Exit at the first
    // unblocked sample; no allocations, per-building timer, geometry traversal or material writes.
    private bool ObstructsSample(Vector3 sample, Transform3D inverse, float localRadius, ref int budget)
    {
        Vector3 worldFrom = _camera.ProjectRayOrigin(_camera.UnprojectPosition(sample));
        Vector3 sight = sample - worldFrom;
        if (sight.LengthSquared() < 0.0001f) return false;
        // Leave a 1cm endpoint margin; a surface touching/behind the player is not cover.
        Vector3 from = inverse * worldFrom;
        Vector3 to = inverse * (sample - sight.Normalized() * 0.01f);
        if (!CameraOcclusionMath.Intersects(from, to, _bounds, localRadius)) return false;
        foreach (var part in _parts)
        {
            if (!GodotObject.IsInstanceValid(part.Mesh) || !part.Mesh.IsVisibleInTree()) continue;
            Vector3 meshFrom = part.ToMesh * from, meshTo = part.ToMesh * to;
            if (!CameraOcclusionMath.Intersects(meshFrom, meshTo, part.Bounds, 0)) continue;
            for (int i = 0; i + 2 < part.Faces.Length; i += 3)
            {
                if (--budget < 0)
                {
                    if (!_budgetWarning) { GD.PushWarning($"{_building.Name}: exterior visibility triangle budget exceeded; keep opaque. Use simpler structural art/visibility meshes."); _budgetWarning = true; }
                    return false;
                }
                if (CameraOcclusionMath.IntersectsTriangle(meshFrom, meshTo,
                    part.Faces[i], part.Faces[i + 1], part.Faces[i + 2])) return true;
            }
        }
        return false;
    }

    // =========================================================
    // Cache only structural art, never furniture, characters or unrelated physics. Mixed/transparent
    // materials fail open: authored glass/cutouts must not become invisible solid occluders.
    private static bool IsStructuralBlocker(MeshInstance3D mesh, Node3D building)
    {
        string role = "";
        for (Node? node = mesh; node != null; node = node.GetParent())
        {
            if (node is CharacterBody3D) return false;
            if (node.HasMeta("structure_camera_blocker") && !node.GetMeta("structure_camera_blocker").AsBool()) return false;
            if (role.Length == 0 && node.HasMeta("structure_visual_role")) role = node.GetMeta("structure_visual_role").AsString();
            if (node == building) break;
        }
        if (role != "walls" && role != "roof" && role != "deck") return false;
        if (mesh.Mesh == null) return false;
        for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
        {
            Material? material = mesh.GetActiveMaterial(surface);
            if (material is BaseMaterial3D standard)
            {
                if (standard.Transparency != BaseMaterial3D.TransparencyEnum.Disabled) return false;
            }
            else if (material != null)
            {
                GD.PushWarning($"{mesh.GetPath()}: custom shader is not an approved opaque exterior blocker; skipped.");
                return false;
            }
        }
        return true;
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
        _parts.Clear(); _sections.Clear(); _occupiedZones = System.Array.Empty<Aabb>();
        _initialized = false;
    }
    // =========================================================
    public override void _ExitTree() => Shutdown();

}
