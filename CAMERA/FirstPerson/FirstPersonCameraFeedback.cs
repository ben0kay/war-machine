// FPS-only transient camera movement. One plain helper, no independent node or idle callback.
using Godot;

public enum CameraFeedbackKind { MeleeSweepHit, MeleeOverheadHit, PlayerDamaged }

public sealed class FirstPersonCameraFeedback
{
    private const int Capacity = 4;
    private struct Impulse
    { public float Amplitude, RollDegrees, Duration, Elapsed, Phase; }

    private readonly Impulse[] _impulses = new Impulse[Capacity];
    private int _count, _sequence;
    public Vector2 Offset { get; private set; }
    public float RollDegrees { get; private set; }
    public bool IsActive => _count > 0;

    // =========================================================
    // Accept a small, bounded camera-space displacement on a confirmed gameplay event.
    // A fixed-size impulse array allows overlapping events without allocations or unbounded stacking.
    public bool Trigger(CameraFeedbackKind kind, FirstPersonSettings settings)
    {
        if (!settings.CameraFeedbackEnabled) return false;
        float amplitude, duration, roll;
        switch (kind)
        {
            case CameraFeedbackKind.MeleeSweepHit:
                amplitude = settings.CameraFeedbackMeleeSweepStrength;
                duration = settings.CameraFeedbackMeleeSweepSeconds;
                roll = settings.CameraFeedbackMeleeSweepRollDegrees;
                break;
            case CameraFeedbackKind.MeleeOverheadHit:
                amplitude = settings.CameraFeedbackMeleeOverheadStrength;
                duration = settings.CameraFeedbackMeleeOverheadSeconds;
                roll = settings.CameraFeedbackMeleeOverheadRollDegrees;
                break;
            case CameraFeedbackKind.PlayerDamaged:
                amplitude = settings.CameraFeedbackPlayerDamagedStrength;
                duration = settings.CameraFeedbackPlayerDamagedSeconds;
                roll = settings.CameraFeedbackPlayerDamagedRollDegrees;
                break;
            default: return false;
        }
        float multiplier = Safe(settings.CameraFeedbackGlobalStrength, 0f, 3f);
        amplitude = Safe(amplitude, 0f, 0.05f) * multiplier;
        roll = Safe(roll, 0f, 3f) * multiplier;
        duration = Safe(duration, 0f, 0.5f);
        if ((amplitude <= 0f && roll <= 0f) || duration <= 0f) return false;

        // Prefer an unused slot; if saturated, replace the effect with least time left.
        int slot = -1; float leastRemaining = float.MaxValue;
        for (int i = 0; i < Capacity; i++)
        {
            if (_impulses[i].Duration <= 0f) { slot = i; break; }
            float remaining = _impulses[i].Duration - _impulses[i].Elapsed;
            if (remaining < leastRemaining) { leastRemaining = remaining; slot = i; }
        }
        if (_impulses[slot].Duration <= 0f) _count++;
        _sequence++;
        _impulses[slot] = new Impulse { Amplitude = amplitude, RollDegrees = roll,
            Duration = duration, Phase = (_sequence % 5) * 0.43f };
        return true;
    }

    // =========================================================
    // Called only by the existing camera render update; decays exactly to zero and sleeps.
    public bool Advance(float delta, FirstPersonSettings settings)
    {
        if (_count == 0) { Offset = Vector2.Zero; RollDegrees = 0f; return false; }
        delta = Safe(delta, 0f, 0.1f);
        float hz = Safe(settings.CameraFeedbackFrequencyHz, 2f, 30f);
        Vector2 combined = Vector2.Zero;
        float combinedRoll = 0f;
        for (int i = 0; i < Capacity; i++)
        {
            Impulse impulse = _impulses[i];
            if (impulse.Duration <= 0f) continue;
            impulse.Elapsed += delta;
            float progress = Mathf.Min(1f, impulse.Elapsed / impulse.Duration);
            if (progress >= 1f)
            { _impulses[i] = default; _count--; continue; }
            float fade = (1f - progress) * (1f - progress);
            float cycle = impulse.Elapsed * Mathf.Tau * hz;
            combined += new Vector2(Mathf.Sin(cycle + impulse.Phase),
                Mathf.Sin(cycle * 0.77f + impulse.Phase + 0.9f) * 0.7f)
                * (impulse.Amplitude * fade);
            // A small damped roll gives distant scenery a visible response without changing
            // persistent mouse pitch/yaw (and therefore no extra bullet spread or recoil).
            combinedRoll += Mathf.Sin(cycle * 0.62f + impulse.Phase + 0.7f)
                * impulse.RollDegrees * fade;
            _impulses[i] = impulse;
        }
        float maxOffset = Safe(settings.CameraFeedbackMaximumOffset, 0f, 0.08f);
        Offset = combined.LimitLength(maxOffset);
        RollDegrees = Mathf.Clamp(combinedRoll,
            -Safe(settings.CameraFeedbackMaximumRollDegrees, 0f, 5f),
            Safe(settings.CameraFeedbackMaximumRollDegrees, 0f, 5f));
        return _count > 0;
    }

    // =========================================================
    public void Reset()
    {
        for (int i = 0; i < Capacity; i++) _impulses[i] = default;
        _count = 0; Offset = Vector2.Zero; RollDegrees = 0f;
    }

    // =========================================================
    private static float Safe(float value, float min, float max)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;
}
