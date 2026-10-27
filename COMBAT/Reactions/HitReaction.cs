// Small value state advanced by an actor's existing physics callback; no nodes, timers or idle work.
using Godot;

public struct HitReaction
{
    public float Remaining { get; private set; }
    public float Duration { get; private set; }
    public float Strength { get; private set; }
    public Vector3 Direction { get; private set; }
    private ulong _nextAllowed, _minimumRecoveryMsec;
    public bool IsActive => Remaining > 0;
    public float Progress => Duration > 0 ? Mathf.Clamp(1 - Remaining / Duration, 0, 1) : 1;

    // =========================================================
    // Never extend an active reaction. Every accepted flinch guarantees a configurable recovery window.
    public bool TryStart(in CombatImpact impact, float resistance, float threshold, float maxDuration,
        float minInterval, float minRecovery, ulong now)
    {
        if (IsActive || now < _nextAllowed) return false;
        float resist = Safe(resistance, 0, 1), strength = impact.Strength * (1 - resist);
        float duration = Mathf.Min(impact.Duration * (1 - resist), Safe(maxDuration, 0, 1));
        if (strength < Safe(threshold, 0, 10) || strength <= 0 || duration <= 0) return false;
        Remaining = Duration = duration; Strength = strength; Direction = impact.Direction;
        float interval = Mathf.Max(Safe(minInterval, 0, 10), duration + Safe(minRecovery, 0.05f, 10));
        _minimumRecoveryMsec = (ulong)Mathf.CeilToInt(Safe(minRecovery, 0.05f, 10) * 1000);
        _nextAllowed = now + (ulong)Mathf.CeilToInt(interval * 1000);
        return true;
    }

    // =========================================================
    public void Advance(float delta, ulong now)
    {
        if (!IsActive) return;
        Remaining = Mathf.Max(0, Remaining - Mathf.Max(0, delta));
        // Reserve recovery from actual expiry too, so pauses/slow frames cannot consume the whole gap.
        if (!IsActive) _nextAllowed = System.Math.Max(_nextAllowed, now + _minimumRecoveryMsec);
    }

    // =========================================================
    public void Reset() { this = default; }

    // =========================================================
    private static float Safe(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;
}
