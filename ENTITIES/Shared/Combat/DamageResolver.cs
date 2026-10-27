// Stateless shared damage maths: physical armour first, then the matching type resistance.
using System;

public static class DamageResolver
{
    // =========================================================
    // Invalid/nonpositive hits cannot heal or poison health with NaN; no actor or weapon-specific branches.
    public static DamageResult Resolve(in DamagePacket hit, HealthSettings defence)
    {
        if (!float.IsFinite(hit.Amount) || hit.Amount <= 0) return new DamageResult(hit, 0, 0);
        float armour = float.IsFinite(defence.Armour) ? Math.Max(0, defence.Armour) : 0;
        float resistance = defence.ResistanceFor(hit.Type);
        resistance = float.IsFinite(resistance) ? Math.Clamp(resistance, -1, 1) : 0;
        float afterArmour = hit.Type == DamageType.Physical ? Math.Max(0, hit.Amount - armour) : hit.Amount;
        // Double intermediate protects finite float inputs from overflowing during vulnerability scaling.
        float damage = (float)Math.Min(float.MaxValue, (double)afterArmour * (1 - resistance));
        return new DamageResult(hit, damage, Math.Max(0, hit.Amount - damage));
    }
}
