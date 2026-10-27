// Damage-independent contact metadata. A zero-HP attack can request an impact without touching health.
using Godot;

public enum CombatImpactKind { Bullet, Shove, Melee, Blast }

public readonly struct CombatImpact
{
    public readonly Node3D? Source;
    public readonly Vector3 Position, Direction;
    public readonly CombatImpactKind Kind;
    public readonly float Strength, Duration;

    // =========================================================
    // Keep push direction horizontal and finite; coincident/vertical contacts use a predictable safe fallback.
    public CombatImpact(Node3D? source, Vector3 position, Vector3 direction, CombatImpactKind kind, float strength, float duration)
    {
        Source = source; Position = position; Kind = kind;
        direction.Y = 0;
        Direction = float.IsFinite(direction.X) && float.IsFinite(direction.Z) && direction.LengthSquared() > 0.000001f
            ? direction.Normalized() : Vector3.Forward;
        Strength = float.IsFinite(strength) ? Mathf.Clamp(strength, 0, 10) : 0;
        Duration = float.IsFinite(duration) ? Mathf.Clamp(duration, 0, 2) : 0;
    }
}

public interface IImpactReceiver
{
    void ReceiveImpact(in CombatImpact impact);
}
