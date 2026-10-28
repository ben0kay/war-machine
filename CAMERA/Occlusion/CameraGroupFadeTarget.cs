// OV-W1: a single reusable camera fade target for multiple meshes in one authored wall assembly.
// Multiple independent camera proxies point here; no scene polling or permanent callbacks.
using Godot;
using System.Collections.Generic;

public partial class CameraGroupFadeTarget : RefCounted, ICameraFadeTarget
{
    public CameraOccluder Owner { get; set; } = null!;
    public readonly List<int> MeshIndices = new();
    public bool IsValid => GodotObject.IsInstanceValid(Owner) && Owner.IsValid;

    // =========================================================
    // Keep one opacity across every piece, doorframe and decoration belonging to this wall group.
    public void SetCameraOpacity(float opacity) => Owner.SetGroupCameraOpacity(MeshIndices, opacity);
}
