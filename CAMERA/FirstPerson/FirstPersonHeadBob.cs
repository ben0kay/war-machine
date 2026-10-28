// FPS-only, distance-driven head bob. Plain state helper; no extra Node, timer or physics loop.
using Godot;

public sealed class FirstPersonHeadBob
{
    public Vector2 Offset { get; private set; }
    public bool NeedsUpdate => _target.LengthSquared() > 0.00000001f || Offset.LengthSquared() > 0.00000001f;
    private Vector2 _target;
    private float _phase;

    // =========================================================
    // Walk/run phase advances only after actual grounded post-collision horizontal travel.
    public void Record(float distance, float dt, bool grounded, bool movingInput,
        PlayerController.MovementStance stance, FirstPersonSettings settings)
    {
        _target = Vector2.Zero;
        if (!settings.HeadBobEnabled || !grounded || !movingInput
            || stance == PlayerController.MovementStance.Prone
            || !float.IsFinite(distance) || !float.IsFinite(dt) || dt <= 0f) return;
        float speed = distance / dt;
        if (!float.IsFinite(speed) || speed < 0.15f) return;
        float walkingSpeed = Mathf.Max(0.1f, settings.WalkSpeed);
        float runningSpeed = Mathf.Max(walkingSpeed + 0.1f, walkingSpeed * Mathf.Max(1f, settings.RunMultiplier));
        bool crouching = stance == PlayerController.MovementStance.Crouching;
        float running = crouching ? 0f : Mathf.Clamp((speed - walkingSpeed) / (runningSpeed - walkingSpeed), 0f, 1f);
        float intensity = Mathf.Clamp(speed / walkingSpeed, 0f, 1f) * (crouching ? 0.5f : 1f);
        float stride = Mathf.Lerp(Stride(settings.WalkStrideLength), Stride(settings.RunStrideLength), running);
        // Repeat over two vertical cycles so the lateral sway completes once per two footstep bounces.
        // This is travel-paced presentation, not an exact foot-bone/animation sync.
        _phase = Mathf.PosMod(_phase + Mathf.Min(distance, runningSpeed * dt + 0.15f) / stride * Mathf.Tau, Mathf.Tau * 2f);
        float x = Mathf.Lerp(Amplitude(settings.WalkHorizontalAmplitude), Amplitude(settings.RunHorizontalAmplitude), running) * intensity;
        float y = Mathf.Lerp(Amplitude(settings.WalkVerticalAmplitude), Amplitude(settings.RunVerticalAmplitude), running) * intensity;
        // Old Y at 2× phase gave 6.25 Hz walk / 8 Hz run with the original short cycle.
        // One vertical bounce per phase revolution is gentler, with slower alternating lateral sway.
        _target = new Vector2(Mathf.Sin(_phase * 0.5f) * x, Mathf.Sin(_phase) * y);
    }

    // =========================================================
    // Render interpolation smooths step samples and decays to exact neutral after stopping.
    public bool Advance(float dt, FirstPersonSettings settings)
    {
        float tau = _target.LengthSquared() < 0.00000001f
            ? Mathf.Clamp(settings.HeadBobReturnSeconds, 0.05f, 0.8f) : 0.06f;
        Offset = Offset.Lerp(_target, 1f - Mathf.Exp(-Mathf.Max(0f, dt) / tau));
        if (_target.LengthSquared() < 0.00000001f && Offset.LengthSquared() < 0.00000001f)
            Offset = _target = Vector2.Zero;
        return NeedsUpdate;
    }

    // =========================================================
    public void Stop() => _target = Vector2.Zero;

    // =========================================================
    public void Reset() { _phase = 0f; Offset = _target = Vector2.Zero; }

    // =========================================================
    private static float Amplitude(float value) => float.IsFinite(value) ? Mathf.Clamp(value, 0f, 0.08f) : 0f;

    // =========================================================
    private static float Stride(float value) => float.IsFinite(value) ? Mathf.Clamp(value, 0.5f, 6f) : 2.8f;
}
