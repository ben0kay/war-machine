// RC0: per-weapon cumulative recoil state. Shot spread remains separate and randomized per shot.
using Godot;

public struct FirearmRecoil
{
    private float _pitch, _yaw, _delay;
    private bool _alternateRight;
    public Vector2 OffsetDegrees => new(_yaw, _pitch);
    public bool IsActive => _pitch > 0.0001f || Mathf.Abs(_yaw) > 0.0001f || _delay > 0f;

    // =========================================================
    // Read PRE-shot recoil in the caller's screen-up basis; zero axis preserves legacy aim until RC1/RC2.
    public Vector3 Apply(Vector3 aim, Vector3 viewUp)
    {
        if (!Finite(aim) || aim.LengthSquared() < 0.000001f) return aim;
        Vector3 forward = aim.Normalized();
        if ((_pitch <= 0 && _yaw == 0) || !Finite(viewUp) || viewUp.LengthSquared() < 0.000001f) return forward;
        Vector3 up = viewUp - forward * viewUp.Dot(forward);
        if (up.LengthSquared() < 0.000001f)
        {
            Vector3 reference = Mathf.Abs(forward.Y) < 0.98f ? Vector3.Up : Vector3.Right;
            up = reference - forward * reference.Dot(forward);
        }
        up = up.Normalized();
        Vector3 right = forward.Cross(up).Normalized();
        return (forward + up * Mathf.Tan(Mathf.DegToRad(_pitch))
            + right * Mathf.Tan(Mathf.DegToRad(_yaw))).Normalized();
    }

    // =========================================================
    // Only the firearm's accepted shot path may call this, once, AFTER resolving the current shot.
    public void OnAcceptedShot(FirearmSettings settings)
    {
        float pitchStep = Safe(settings.RecoilPitchPerShotDegrees, 0, 15);
        float yawStep = Safe(settings.RecoilYawStepDegrees, 0, 5);
        _pitch = Mathf.Clamp(_pitch + pitchStep, 0, Safe(settings.RecoilMaxPitchDegrees, 0, 30));
        _yaw = Mathf.Clamp(_yaw + (_alternateRight ? -yawStep : yawStep),
            -Safe(settings.RecoilMaxYawDegrees, 0, 15), Safe(settings.RecoilMaxYawDegrees, 0, 15));
        _alternateRight = !_alternateRight;
        _delay = _pitch > 0.0001f || Mathf.Abs(_yaw) > 0.0001f
            ? Safe(settings.RecoilRecoveryDelaySeconds, 0, 2) : 0;
    }

    // =========================================================
    // Recover only while the firearm wakes processing; finish without a permanent idle callback.
    public void Advance(float delta, FirearmSettings settings)
    {
        if (!IsActive || !float.IsFinite(delta) || delta <= 0) return;
        float dt = Mathf.Min(delta, 0.25f);
        if (_delay > 0) { float waiting = Mathf.Min(_delay, dt); _delay -= waiting; dt -= waiting; }
        if (dt <= 0) return;
        float speed = Safe(settings.RecoilRecoveryDegreesPerSecond, 0, 180);
        if (speed <= 0) { Reset(); return; }
        _pitch = Mathf.MoveToward(_pitch, 0, speed * dt);
        _yaw = Mathf.MoveToward(_yaw, 0, speed * dt);
        if (_pitch <= 0.0001f && Mathf.Abs(_yaw) <= 0.0001f) Reset();
    }

    // =========================================================
    // Swaps, focus loss, shove and unload discard any old stored direction.
    public void Reset()
    { _pitch = 0; _yaw = 0; _delay = 0; _alternateRight = false; }

    // =========================================================
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }

    // =========================================================
    private static bool Finite(Vector3 value)
    { return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z); }
}
