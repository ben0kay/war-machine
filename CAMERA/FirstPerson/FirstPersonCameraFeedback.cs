// FPS-only transient camera movement. One plain helper, no independent node or idle callback.
using Godot;

public enum CameraFeedbackKind { MeleeSweepHit, MeleeOverheadHit, PlayerDamaged }

public sealed class FirstPersonCameraFeedback
{
    private const int Capacity = 4;
    private struct Impulse
    { public float Amplitude, Duration, Elapsed, Phase; }

    private readonly Impulse[] _impulses = new Impulse[Capacity];
    private int _count, _sequence;
    public Vector2 Offset { get; private set; }
    public bool IsActive => _count > 0;

    // =========================================================
    // Accept a small, bounded camera-space displacement on a confirmed gameplay event.
    // A fixed-size impulse array allows overlapping events without allocations or unbounded stacking.
    public bool Trigger(CameraFeedbackKind kind, FirstPersonSettings settings)
    {
        if (!settings.CameraFeedbackEnabled) return false;
        float amplitude, duration;
        switch (kind)
        {
            case CameraFeedbackKind.MeleeSweepHit:
                amplitude = settings.CameraFeedbackMeleeSweepStrength;
                duration = settings.CameraFeedbackMeleeSweepSeconds;
                break;
            case CameraFeedbackKind.MeleeOverheadHit:
                amplitude = settings.CameraFeedbackMeleeOverheadStrength;
                duration = settings.CameraFeedbackMeleeOverheadSeconds;
                break;
            case CameraFeedbackKind.PlayerDamaged:
                amplitude = settings.CameraFeedbackPlayerDamagedStrength;
                duration = settings.CameraFeedbackPlayerDamagedSeconds;
                break;
            default: return false;
        }
        float multiplier = Safe(settings.CameraFeedbackGlobalStrength, 0f, 3f);
        amplitude = Safe(amplitude, 0f, 0.05f) * multiplier;
        duration = Safe(duration, 0f, 0.5f);
        if (amplitude <= 0f || duration <= 0f) return false;

        // Prefer an unused slot; if saturated, replace the effect with least time left.
        int slot = -1; float leastRemaining = float.MaxValue;
        for (int i = 0; i < Capacity; i++)
        {
            if (_impulses[i].Amplitude <= 0f) { slot = i; break; }
            float remaining = _impulses[i].Duration - _impulses[i].Elapsed;
            if (remaining < leastRemaining) { leastRemaining = remaining; slot = i; }
        }
        if (_impulses[slot].Amplitude <= 0f) _count++;
        _sequence++;
        _impulses[slot] = new Impulse { Amplitude = amplitude, Duration = duration,
            Phase = (_sequence % 5) * 0.43f };
        return true;
    }

    // =========================================================
    // Called only by the existing camera render update; decays exactly to zero and sleeps.
    public bool Advance(float delta, FirstPersonSettings settings)
    {
        if (_count == 0) { Offset = Vector2.Zero; return false; }
        delta = Safe(delta, 0f, 0.1f);
        float hz = Safe(settings.CameraFeedbackFrequencyHz, 2f, 30f);
        Vector2 combined = Vector2.Zero;
        for (int i = 0; i < Capacity; i++)
        {
            Impulse impulse = _impulses[i];
            if (impulse.Amplitude <= 0f) continue;
            impulse.Elapsed += delta;
            float progress = Mathf.Min(1f, impulse.Elapsed / impulse.Duration);
            if (progress >= 1f)
            { _impulses[i] = default; _count--; continue; }
            float fade = (1f - progress) * (1f - progress);
            float cycle = impulse.Elapsed * Mathf.Tau * hz;
            combined += new Vector2(Mathf.Sin(cycle + impulse.Phase),
                Mathf.Sin(cycle * 0.77f + impulse.Phase + 0.9f) * 0.7f)
                * (impulse.Amplitude * fade);
            _impulses[i] = impulse;
        }
        float maxOffset = Safe(settings.CameraFeedbackMaximumOffset, 0f, 0.08f);
        Offset = combined.LimitLength(maxOffset);
        return _count > 0;
    }

    // =========================================================
    public void Reset()
    {
        for (int i = 0; i < Capacity; i++) _impulses[i] = default;
        _count = 0; Offset = Vector2.Zero;
    }

    // =========================================================
    private static float Safe(float value, float min, float max)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;
}
