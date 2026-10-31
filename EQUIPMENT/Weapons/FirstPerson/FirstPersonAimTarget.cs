// FPS camera-centre target resolver shared by actual firing and the optional unspread debug line.
using Godot;

public static class FirstPersonAimTarget
{
    // =========================================================
    // The camera finds the intended point; the firearm then fires FROM its muzzle toward that point.
    // Caller owns the cached query. Spread and recoil are not calculated here.
    public static Vector3 Resolve(PlayerController player, PhysicsRayQueryParameters3D query,
        Vector3 cameraOrigin, Vector3 cameraDirection, float range)
    {
        query.From = cameraOrigin;
        query.To = cameraOrigin + cameraDirection * range;
        var hit = player.GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 ? hit["position"].AsVector3() : query.To;
    }
}
