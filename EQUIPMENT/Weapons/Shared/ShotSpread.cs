// Shared per-shot angular dispersion for any firearm; zero spread leaves aim precisely unchanged.
using Godot;

public static class ShotSpread
{
    // =========================================================
    // Draw one random direction in the aim cone; maximum offset grows naturally with distance.
    // Radius exponent 0.5 is uniform disk area; lower values favour the outer cone.
    // Called only for accepted shots, with no extra raycasts, nodes or update loops.
    public static Vector3 Apply(Vector3 aimDirection, float maxDegrees, RandomNumberGenerator random,
        float radialExponent = 0.5f)
    {
        Vector3 forward = aimDirection.Normalized();
        if (forward.LengthSquared() < 0.000001f) return Vector3.Zero;

        float degrees = Mathf.Clamp(maxDegrees, 0f, 10f);
        if (degrees <= 0f) return forward;

        // Build a stable perpendicular basis without degenerating near vertical shots.
        Vector3 reference = Mathf.Abs(forward.Y) > 0.98f ? Vector3.Right : Vector3.Up;
        Vector3 right = forward.Cross(reference).Normalized();
        Vector3 up = right.Cross(forward).Normalized();
        float phi = random.RandfRange(0f, Mathf.Tau);
        float exponent = float.IsFinite(radialExponent) ? Mathf.Clamp(radialExponent, 0.2f, 0.5f) : 0.5f;
        float radius = Mathf.Pow(random.Randf(), exponent) * Mathf.Tan(Mathf.DegToRad(degrees));
        Vector3 offset = right * Mathf.Cos(phi) + up * Mathf.Sin(phi);
        return (forward + offset * radius).Normalized();
    }
}
