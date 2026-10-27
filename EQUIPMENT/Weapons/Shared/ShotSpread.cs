// Shared per-shot angular dispersion for any firearm; zero spread leaves aim precisely unchanged.
using Godot;

public static class ShotSpread
{
    // =========================================================
    // Sample uniformly over a small disk in the aim plane; angular spread scales naturally with range.
    // Called once for an accepted shot. Does not create nodes, physics queries or persistent timers.
    public static Vector3 Apply(Vector3 aimDirection, float maxDegrees, RandomNumberGenerator random)
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
        float radius = Mathf.Sqrt(random.Randf()) * Mathf.Tan(Mathf.DegToRad(degrees));
        Vector3 offset = right * Mathf.Cos(phi) + up * Mathf.Sin(phi);
        return (forward + offset * radius).Normalized();
    }
}
