// Value-only hit description shared by bullets, melee, beams and future explosions; no per-hit object allocation.
using Godot;

public enum DamageType { Physical, Energy, Explosive, Fire }

public readonly struct DamagePacket
{
    public readonly float Amount;
    public readonly DamageType Type;
    public readonly Node3D? Source;
    public readonly Vector3 Position, Direction;

    // =========================================================
    // Detection supplies hit context; the resolver owns damage rules, never raycasts or target searches.
    public DamagePacket(float amount, DamageType type, Node3D? source, Vector3 position, Vector3 direction)
    { Amount = amount; Type = type; Source = source; Position = position; Direction = direction; }
}

public readonly struct DamageResult
{
    public readonly DamagePacket Hit;
    public readonly float Amount;
    public readonly float Mitigated;

    // =========================================================
    // Amount is resolved incoming damage, not necessarily actual HP lost (overkill is possible).
    public DamageResult(DamagePacket hit, float amount, float mitigated)
    { Hit = hit; Amount = amount; Mitigated = mitigated; }
}
