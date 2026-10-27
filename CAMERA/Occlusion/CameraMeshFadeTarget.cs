// Lightweight per-wall fade handle; no Node, timer or callbacks. CameraOccluder owns cached mesh state.
using Godot;

public partial class CameraMeshFadeTarget : RefCounted, ICameraFadeTarget
{
    public CameraOccluder Owner { get; set; } = null!;
    public int MeshIndex { get; set; }
    public bool IsValid => GodotObject.IsInstanceValid(Owner) && Owner.IsValid;

    // =========================================================
    public void SetCameraOpacity(float opacity) => Owner.SetMeshCameraOpacity(MeshIndex, opacity);
}
