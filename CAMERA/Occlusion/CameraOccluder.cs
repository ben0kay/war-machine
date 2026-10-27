// Opt-in building/wall/scenery fade component. One bounds proxy, no update loop, never attached to entities.
using Godot;
using System.Collections.Generic;

public partial class CameraOccluder : Node3D, ICameraFadeTarget
{
    public const uint ObstructionLayer = 1u << 7; // Physics layer 8; independent of solid/weapon layers.
    public static event System.Action? RegistryChanged;
    [Export] public Node3D ObjectRoot { get; set; } = null!;
    private readonly List<MeshInstance3D> _meshes = new();
    private readonly List<float> _originalTransparency = new();
    private bool _allowed;
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
        for (Node? node = ObjectRoot; node != null; node = node.GetParent())
            if (node is CharacterBody3D) { GD.PushWarning("CameraOccluder ignores entities/characters."); return; }
        CollectMeshes(ObjectRoot);
        if (_meshes.Count == 0) return;
        bool first = true; Aabb bounds = default;
        foreach (var mesh in _meshes)
        {
            var local = CameraOcclusionMath.TransformBounds(mesh.GetAabb(), GlobalTransform.AffineInverse() * mesh.GlobalTransform);
            bounds = first ? local : bounds.Merge(local); first = false;
            _originalTransparency.Add(mesh.Transparency);
        }
        var proxy = new Area3D { Name = "CameraObstructionProxy", CollisionLayer = ObstructionLayer,
            CollisionMask = 0, Monitoring = false, Monitorable = true };
        proxy.SetMeta("camera_occluder", this);
        proxy.AddChild(new CollisionShape3D { Position = bounds.GetCenter(),
            Shape = new BoxShape3D { Size = bounds.Size.Max(Vector3.One * 0.05f) } });
        AddChild(proxy); _allowed = true;
        SetNotifyTransform(true); RegistryChanged?.Invoke();
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
    {
        for (int i = 0; i < _meshes.Count; i++)
            if (IsInstanceValid(_meshes[i]))
                _meshes[i].Transparency = 1 - (1 - _originalTransparency[i]) * Mathf.Clamp(opacity, 0, 1);
    }

    // =========================================================
    // Static objects have no polling; actual transform changes explicitly wake a new camera query.
    public override void _Notification(int what)
    { if (_allowed && what == NotificationTransformChanged) RegistryChanged?.Invoke(); }

    // =========================================================
    public override void _ExitTree()
    { if (_allowed) { SetCameraOpacity(1); _allowed = false; RegistryChanged?.Invoke(); } }
}
