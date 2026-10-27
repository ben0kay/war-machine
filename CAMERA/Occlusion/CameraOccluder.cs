// Opt-in scenery section fade owner: optional per-wall proxies, composed floor opacity, no update loop.
using Godot;
using System.Collections.Generic;

public partial class CameraOccluder : Node3D, ICameraFadeTarget
{
    public const uint ObstructionLayer = 1u << 7; // Physics layer 8; independent of solid/weapon layers.
    public static event System.Action? RegistryChanged;
    [Export] public Node3D ObjectRoot { get; set; } = null!;
    private readonly List<MeshInstance3D> _meshes = new();
    private readonly List<float> _originalTransparency = new();
    private readonly List<float> _meshCameraOpacity = new();
    private bool _allowed;
    private float _cameraOpacity = 1, _presentationOpacity = 1, _buildingOpacity = 1;
    private bool _cameraFadeEnabled = true;
    [Export] public NodePath VisualRootPath { get; set; } = new NodePath("");
    [Export] public bool FadeIndividualMeshes { get; set; }
    [Export] public bool CreateObstructionProxy { get; set; } = true;
    public bool IsValid => _allowed && IsInsideTree() && !IsQueuedForDeletion();

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
                for (int i = 0; i < _meshes.Count; i++)
                {
                    var meshBounds = CameraOcclusionMath.TransformBounds(_meshes[i].GetAabb(),
                        GlobalTransform.AffineInverse() * _meshes[i].GlobalTransform);
                    AddProxy(meshBounds, new CameraMeshFadeTarget { Owner = this, MeshIndex = i });
                }
            }
            else AddProxy(bounds, this);
        }
        _allowed = true; ApplyOpacity();
        SetNotifyTransform(true); RegistryChanged?.Invoke();
    }

    // =========================================================
    // Fine-grained wall targets share this owner and presentation factor; static Areas do not poll or process.
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
        if (root is MeshInstance3D mesh) _meshes.Add(mesh);
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
    private void ApplyOpacity()
    { for (int i = 0; i < _meshes.Count; i++) ApplyMeshOpacity(i); }

    // =========================================================
    // Camera interpolation touches only the obstructing wall mesh, preserving other walls and floor presentation.
    public void SetMeshCameraOpacity(int index, float opacity)
    { _meshCameraOpacity[index] = Mathf.Clamp(opacity, 0, 1); ApplyMeshOpacity(index); }

    // =========================================================
    private void ApplyMeshOpacity(int index)
    {
        if (!IsInstanceValid(_meshes[index])) return;
        float camera = FadeIndividualMeshes ? _meshCameraOpacity[index] : _cameraOpacity;
        float opacity = _buildingOpacity * _presentationOpacity * (_cameraFadeEnabled ? camera : 1);
        _meshes[index].Transparency = 1 - (1 - _originalTransparency[index]) * opacity;
    }

    // =========================================================
    // Static objects have no polling; actual transform changes explicitly wake a new camera query.
    public override void _Notification(int what)
    { if (_allowed && what == NotificationTransformChanged) RegistryChanged?.Invoke(); }

    // =========================================================
    public override void _ExitTree()
    { if (_allowed) { _buildingOpacity = 1; _presentationOpacity = 1; _cameraFadeEnabled = false; SetCameraOpacity(1); _allowed = false; RegistryChanged?.Invoke(); } }
}
