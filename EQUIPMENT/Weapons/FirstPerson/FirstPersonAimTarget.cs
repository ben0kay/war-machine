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

    // =========================================================
    // Smooth deterministic hand drift, sampled only while physical sway is active.
    // Angles: X = screen-right yaw, Y = screen-up pitch, in degrees.
    public static Vector2 SamplePhysicalSway(FirearmSettings settings, float adsBlend, double timeSeconds)
    {
        if (!settings.FpsPhysicalSwayEnabled) return Vector2.Zero;
        float amplitude = Mathf.Clamp(settings.FpsSwayAmplitudeDegrees, 0f, 1f);
        if (amplitude <= 0f) return Vector2.Zero;
        float frequency = Mathf.Clamp(settings.FpsSwayFrequencyHz, 0.1f, 3f);
        float adsMultiplier = Mathf.Clamp(settings.FpsAdsSwayMultiplier, 0.1f, 1f);
        float strength = amplitude * Mathf.Lerp(1f, adsMultiplier, Mathf.Clamp(adsBlend, 0f, 1f));
        float phase = (float)(timeSeconds * Mathf.Tau * frequency);
        float yaw = 0.75f * Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 0.51f + 0.8f);
        float pitch = 0.60f * Mathf.Sin(phase * 0.73f + 1.2f)
            + 0.40f * Mathf.Sin(phase * 1.31f);
        return new Vector2(yaw, pitch) * strength;
    }

    // =========================================================
    // Translate small angular sway into a different muzzle-to-target shot direction.
    // Both the real shot and its cyan debug ray call this exact function before random spread.
    public static Vector3 ApplyPhysicalSway(Vector3 aimTarget, Vector3 muzzle,
        Basis cameraBasis, Vector2 anglesDegrees)
    {
        if (anglesDegrees == Vector2.Zero) return aimTarget;
        float distance = Mathf.Max(0.5f, muzzle.DistanceTo(aimTarget));
        float right = Mathf.Tan(Mathf.DegToRad(anglesDegrees.X)) * distance;
        float up = Mathf.Tan(Mathf.DegToRad(anglesDegrees.Y)) * distance;
        return aimTarget + cameraBasis.X * right + cameraBasis.Y * up;
    }
}

