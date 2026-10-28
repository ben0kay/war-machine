// Flat optional biped injury state; no nodes, queries, animation or life/death authority.
using Godot;
using System;

public enum LimbState { Functional, Damaged, Disabled, Severed }

public readonly struct AnatomySnapshot
{
    private readonly uint _states;
    // =========================================================
    public AnatomySnapshot(uint states) { _states = states; }
    // =========================================================
    public LimbState State(BodyRegion region)
        => AnatomyComponent.IsRegion(region) ? (LimbState)((_states >> ((int)region * 2)) & 3u) : LimbState.Functional;
    // =========================================================
    public bool IsSevered(BodyRegion region) => State(region) == LimbState.Severed;
}

public sealed class AnatomyComponent
{
    public event Action<BodyRegion, LimbState>? StateChanged;
    private readonly float[] _condition = new float[6], _stress = new float[6];
    private readonly LimbState[] _states = new LimbState[6];
    private readonly AnatomySettings _settings;
    private bool _changing;
    public float Maximum { get; }

    // =========================================================
    public AnatomyComponent(AnatomySettings settings)
    {
        _settings = (AnatomySettings)settings.Duplicate(true);
        Maximum = Safe(_settings.RegionCondition, 1, 10000, 80);
        for (int i = 0; i < 6; i++) _condition[i] = Maximum;
    }

    // =========================================================
    public static bool IsRegion(BodyRegion region) => (int)region >= 0 && (int)region < 6;
    // =========================================================
    public static bool IsLimb(BodyRegion region) => (int)region >= (int)BodyRegion.LeftArm && (int)region <= (int)BodyRegion.RightLeg;
    // =========================================================
    public float Condition(BodyRegion region) => IsRegion(region) ? _condition[(int)region] : 0;
    // =========================================================
    public LimbState State(BodyRegion region) => IsRegion(region) ? _states[(int)region] : LimbState.Functional;

    // =========================================================
    // Caller supplies already-mitigated damage. Commit and notify before the coordinator can deliver lethal vitality loss.
    public void ApplyResolved(in DamageResult damage)
    {
        BodyRegion region = damage.Hit.Region;
        if (_changing || !IsRegion(region) || !float.IsFinite(damage.Amount) || damage.Amount <= 0) return;
        int index = (int)region;
        if (_states[index] == LimbState.Severed) return;
        _changing = true;
        try
        {
            _condition[index] = Mathf.Max(0, _condition[index] - damage.Amount);
            float power = Safe(damage.Hit.SeverPower, 0, 100, 0);
            // Explicit sever power is required. Legacy hits can disable but never sever a limb.
            _stress[index] = (float)Math.Min(float.MaxValue, _stress[index] + (double)damage.Amount * power);
            float disabled = Safe(_settings.DisabledFraction, 0, 1, 0.25f);
            float damaged = Mathf.Max(disabled, Safe(_settings.DamagedFraction, 0, 1, 0.75f));
            float threshold = Safe(_settings.SeverThreshold, 1, 100000, 65)
                * Safe(_settings.SeverResistance, 0.01f, 100, 1);
            LimbState next = IsLimb(region) && power > 0 && _condition[index] <= 0 && _stress[index] >= threshold
                ? LimbState.Severed
                : IsLimb(region) && _condition[index] <= Maximum * disabled ? LimbState.Disabled
                : _condition[index] <= Maximum * damaged ? LimbState.Damaged : LimbState.Functional;
            // Injury progression is monotonic; a later weak/non-severing hit cannot repair a disabled limb.
            if ((int)next < (int)_states[index]) next = _states[index];
            if (next == _states[index]) return;
            _states[index] = next;
            StateChanged?.Invoke(region, next);
        }
        finally { _changing = false; }
    }

    // =========================================================
    public AnatomySnapshot Capture()
    {
        uint states = 0;
        for (int i = 0; i < 6; i++) states |= (uint)_states[i] << (i * 2);
        return new AnatomySnapshot(states);
    }

    // =========================================================
    private static float Safe(float value, float min, float max, float fallback)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
