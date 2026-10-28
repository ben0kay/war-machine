// FPS-only blunt melee presentation: wide left wind-up, fast cross-screen sweep, then controlled return.
// No hit queries or character motion: the shared attack timer remains the source of damage and contact.
using Godot;

public static class FirstPersonMeleeSwing
{
    // =========================================================
    // Camera-local motion moves both the grip and full bat across the view, not just the barrel angle.
    public static void Apply(Node3D weapon, Transform3D rest, MeleeSettings settings, float elapsed)
    {
        float windup = SafeTime(settings.WindupSeconds), active = SafeTime(settings.ActiveSeconds);
        float recovery = SafeTime(settings.RecoverySeconds);
        float left = SafeExtent(settings.FirstPersonWindupLeft), right = SafeExtent(settings.FirstPersonFollowRight);
        Basis start = rest.Basis, wind = Orient(55f, 72f, -22f), finish = Orient(-65f, 48f, 26f);
        Basis rotation;
        Vector3 shift;
        if (elapsed < windup)
        {
            float t = Smooth(elapsed / windup);
            rotation = start.Slerp(wind, t);
            shift = new Vector3(-left * t, 0.13f * t, -0.08f * t);
        }
        else if (elapsed < windup + active)
        {
            // Straight-through, fast active stroke follows the same time range as the contact sweep.
            float t = Mathf.Clamp((elapsed - windup) / active, 0f, 1f);
            rotation = wind.Slerp(finish, t);
            shift = new Vector3(Mathf.Lerp(-left, right, t), Mathf.Lerp(0.13f, -0.08f, t),
                Mathf.Lerp(-0.08f, 0.03f, t));
        }
        else
        {
            float t = Smooth((elapsed - windup - active) / recovery);
            rotation = finish.Slerp(start, t);
            shift = new Vector3(Mathf.Lerp(right, 0f, t), Mathf.Lerp(-0.08f, 0f, t),
                Mathf.Lerp(0.03f, 0f, t));
        }
        weapon.Transform = new Transform3D(rotation, rest.Origin + shift);
    }

    // =========================================================
    // Yaw keeps the barrel on the correct attack side; pitch/roll add a forceful diagonal slash.
    private static Basis Orient(float yaw, float pitch, float roll)
        => new Basis(Vector3.Up, Mathf.DegToRad(yaw))
            * new Basis(Vector3.Right, Mathf.DegToRad(pitch))
            * new Basis(Vector3.Back, Mathf.DegToRad(roll));

    // =========================================================
    private static float Smooth(float t)
    { t = Mathf.Clamp(t, 0f, 1f); return t * t * (3f - 2f * t); }

    // =========================================================
    private static float SafeTime(float t) => float.IsFinite(t) ? Mathf.Clamp(t, 0.01f, 1f) : 0.1f;

    // =========================================================
    private static float SafeExtent(float v) => float.IsFinite(v) ? Mathf.Clamp(v, 0.1f, 1.2f) : 0.65f;
}
