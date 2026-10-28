// Opt-in scenery section fade owner: optional per-wall proxies, composed floor opacity, no update loop.
using Godot;
using System.Collections.Generic;

public partial class CameraOccluder : Node3D, ICameraFadeTarget
{
    public const uint ObstructionLayer = 1u << 7; // Physics layer 8; independent of solid/weapon layers.
    // DOOR1: moving RigidBody3D visuals inherit wall fade but must never create stationary proxies.
    public const string DynamicFadeVisualMeta = "camera_dynamic_fade_visual";
    public static event System.Action? RegistryChanged;
    // AIM1: waking top-down cursor aim only when a section crosses faded/opaque state.
    public static event System.Action? AimFadeChanged;
    [Export] public Node3D ObjectRoot { get; set; } = null!;
    private readonly List<MeshInstance3D> _meshes = new();
    private readonly List<float> _originalTransparency = new();
    private readonly List<float> _meshCameraOpacity = new();
    private bool _allowed;
    private float _cameraOpacity = 1, _presentationOpacity = 1, _buildingOpacity = 1;
    private bool _cameraFadeEnabled = true, _wholeBuildingMode;
    public System.Collections.Generic.IReadOnlyList<MeshInstance3D> Meshes => _meshes;
    public HashSet<MeshInstance3D> ExcludedMeshes { get; } = new();
    [Export] public NodePath VisualRootPath { get; set; } = new NodePath("");
    [Export] public bool FadeIndividualMeshes { get; set; }
    [Export] public bool CreateObstructionProxy { get; set; } = true;
    public bool IsValid => _allowed && !_wholeBuildingMode && IsInsideTree() && !IsQueuedForDeletion();

    // =========================================================
    // Collect this object's mesh renderers once, excluding nested characters; ordinary gameplay collision is unchanged.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(InitializeBounds));
    }

    // =========================================================
    // Deferred once so a procedural parent can finish constructing its meshes in its own Ready callback.
    public void InitializeBounds()
    {
        if (!IsInsideTree() || _allowed) return;
        ObjectRoot ??= GetParent() as Node3D;
        if (ObjectRoot == null) return;
        if (VisualRootPath.ToString().Length > 0) ObjectRoot = ObjectRoot.GetNodeOrNull<Node3D>(VisualRootPath);
        if (ObjectRoot == null) { GD.PushWarning("Camera fade visual root missing."); return; }
        for (Node? node = ObjectRoot; node != null; node = node.GetParent())
            if (node is CharacterBody3D) { GD.PushWarning("CameraOccluder ignores entities/characters."); return; }
        CollectMeshes(ObjectRoot);
        if (_meshes.Count == 0) return;
        bool first = true; Aabb bounds = default;
        foreach (var mesh in _meshes)
        {
            var local = CameraOcclusionMath.TransformBounds(mesh.GetAabb(), GlobalTransform.AffineInverse() * mesh.GlobalTransform);
            bounds = first ? local : bounds.Merge(local); first = false;
            _originalTransparency.Add(mesh.Transparency); _meshCameraOpacity.Add(1);
        }
        if (CreateObstructionProxy)
        {
            if (FadeIndividualMeshes)
            {
                // OV-W1: one proxy per physical wall piece, but one fade target per logical wall
                // assembly. The entire assembly fades if *any* member blocks the player.
                var groups = new Dictionary<Node, CameraGroupFadeTarget>();
                for (int i = 0; i < _meshes.Count; i++)
                {
                    var meshBounds = CameraOcclusionMath.TransformBounds(_meshes[i].GetAabb(),
                        GlobalTransform.AffineInverse() * _meshes[i].GlobalTransform);
                    Node? group = FindFadeGroup(_meshes[i]);
                    GodotObject target;
                    if (group != null)
                    {
                        if (!groups.TryGetValue(group, out var handle))
                        {
                            handle = new CameraGroupFadeTarget { Owner = this };
                            groups.Add(group, handle);
                        }
                        handle.MeshIndices.Add(i);
                        target = handle;
                    }
                    else target = new CameraMeshFadeTarget { Owner = this, MeshIndex = i };
                    // The mesh is still in the group opacity list; only its *static*
                    // camera obstruction proxy is omitted. A hinged leaf moves after setup.
                    if (!_meshes[i].HasMeta(DynamicFadeVisualMeta)
                        || !_meshes[i].GetMeta(DynamicFadeVisualMeta).AsBool())
                        AddProxy(meshBounds, target);
                }
            }
            else AddProxy(bounds, this);
        }
        _allowed = true; ApplyOpacity();
        SetNotifyTransform(true); RegistryChanged?.Invoke();
    }

    // =========================================================
    // The first marked ancestor under this visual role identifies a whole logical wall assembly.
    // Unmarked geometry keeps the old per-mesh fade, so legacy buildings are not changed.
    private Node? FindFadeGroup(MeshInstance3D mesh)
    {
        for (Node? node = mesh; node != null; node = node.GetParent())
        {
            if (node.HasMeta(StructureLayout.FadeGroupMeta)) return node;
            if (node == ObjectRoot) break;
        }
        return null;
    }

    // =========================================================
    // Fine-grained obstruction proxies share one group target; static Areas do not poll or process.
    private void AddProxy(Aabb bounds, GodotObject target)
    {
        var proxy = new Area3D { Name = "CameraObstructionProxy", CollisionLayer = ObstructionLayer,
            CollisionMask = 0, Monitoring = false, Monitorable = true };
        proxy.SetMeta("camera_occluder", target);
        proxy.AddChild(new CollisionShape3D { Position = bounds.GetCenter(),
            Shape = new BoxShape3D { Size = bounds.Size.Max(Vector3.One * 0.05f) } });
        AddChild(proxy);
    }

    // =========================================================
    // Traversal happens once at setup, not in a camera query. Trees use their dedicated instance provider instead.
    private void CollectMeshes(Node root)
    {
        if (root is CharacterBody3D || root is CameraOccluder) return;
        if (root is MeshInstance3D mesh && !ExcludedMeshes.Contains(mesh)) _meshes.Add(mesh);
        foreach (var child in root.GetChildren()) CollectMeshes(child);
    }

    // =========================================================
    // Forward+ per-instance transparency preserves shared source materials and authored transparency.
    public void SetCameraOpacity(float opacity)
    { _cameraOpacity = Mathf.Clamp(opacity, 0, 1); ApplyOpacity(); }

    // =========================================================
    // Floor presentation composes with camera obstruction instead of competing for mesh transparency.
    public void SetPresentationOpacity(float opacity)
    {
        opacity = Mathf.Clamp(opacity, 0, 1);
        if (Mathf.IsEqualApprox(opacity, _presentationOpacity)) return;
        _presentationOpacity = opacity; ApplyOpacity();
    }

    // =========================================================
    // Outside whole-building fallback multiplies through the existing owner; no competing mesh writer.
    public void SetBuildingOpacity(float opacity)
    {
        opacity = Mathf.Clamp(opacity, 0, 1);
        if (Mathf.IsEqualApprox(opacity, _buildingOpacity)) return;
        _buildingOpacity = opacity; ApplyOpacity();
    }

    // =========================================================
    // Lower-storey walls restore upstairs even when a broad camera proxy still intersects the sight corridor.
    public void SetCameraFadeEnabled(bool enabled)
    {
        if (_cameraFadeEnabled == enabled) return;
        _cameraFadeEnabled = enabled; ApplyOpacity();
    }

    // =========================================================
    // Outside buildings participate as one target. Ignore and unregister sectional camera factors until entry.
    public void SetWholeBuildingMode(bool whole)
    {
        if (_wholeBuildingMode == whole) return;
        _wholeBuildingMode = whole; _cameraOpacity = 1;
        for (int i = 0; i < _meshCameraOpacity.Count; i++) _meshCameraOpacity[i] = 1;
        ApplyOpacity(); RegistryChanged?.Invoke();
    }

    // =========================================================
    private void ApplyOpacity()
    {
        bool changed = false;
        for (int i = 0; i < _meshes.Count; i++) changed |= ApplyMeshOpacity(i);
        if (changed) AimFadeChanged?.Invoke(); // A whole-building reveal produces one notification per owner.
    }

    // =========================================================
    // Camera interpolation touches only the obstructing wall mesh, preserving other walls and floor presentation.
    public void SetMeshCameraOpacity(int index, float opacity)
    {
        _meshCameraOpacity[index] = Mathf.Clamp(opacity, 0, 1);
        if (ApplyMeshOpacity(index)) AimFadeChanged?.Invoke();
    }

    // =========================================================
    // An obstruction on one piece fades all meshes in that authored wall assembly.
    // AIM1 gets one notification after the group, not one per frame or mesh.
    public void SetGroupCameraOpacity(System.Collections.Generic.IReadOnlyList<int> indices, float opacity)
    {
        bool changed = false;
        opacity = Mathf.Clamp(opacity, 0, 1);
        foreach (int index in indices)
        {
            if (index < 0 || index >= _meshCameraOpacity.Count) continue;
            _meshCameraOpacity[index] = opacity; changed |= ApplyMeshOpacity(index);
        }
        if (changed) AimFadeChanged?.Invoke();
    }

    // =========================================================
    private bool ApplyMeshOpacity(int index)
    {
        if (!IsInstanceValid(_meshes[index])) return false;
        float camera = FadeIndividualMeshes ? _meshCameraOpacity[index] : _cameraOpacity;
        float opacity = _buildingOpacity * _presentationOpacity * (_cameraFadeEnabled && !_wholeBuildingMode ? camera : 1);
        var mesh = _meshes[index];
        mesh.Transparency = 1 - (1 - _originalTransparency[index]) * opacity;
        // AIM1: use composed camera+floor+whole-building fade, not authored glass transparency.
        bool faded = opacity < 0.999f;
        bool wasFaded = mesh.HasMeta(FadeAwareAim.FadedMeta) && mesh.GetMeta(FadeAwareAim.FadedMeta).AsBool();
        if (faded != wasFaded) mesh.SetMeta(FadeAwareAim.FadedMeta, faded);
        return faded != wasFaded;
    }

    // =========================================================
    // Static objects have no polling; actual transform changes explicitly wake a new camera query.
    public override void _Notification(int what)
    { if (_allowed && what == NotificationTransformChanged) RegistryChanged?.Invoke(); }

    // =========================================================
    public override void _ExitTree()
    { if (_allowed) { _buildingOpacity = 1; _presentationOpacity = 1; _cameraFadeEnabled = false; SetCameraOpacity(1); _allowed = false; RegistryChanged?.Invoke(); } }
}
