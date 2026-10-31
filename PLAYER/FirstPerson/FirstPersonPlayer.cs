// Test-only arrow-key adapter. Shared player still owns gravity, capsule collision, acceleration and crouching.
using Godot;

public partial class FirstPersonPlayer : PlayerController
{
    private FirstPersonCamera _view = null!;
    private FirstPersonSettings _fps = null!;
    private float _adsMovementMultiplier = 1f, _equippedMovementMultiplier = 1f;

    // =========================================================
    // Equipment updates only when ADS starts/stops. Wake shared locomotion for immediate speed changes.
    public void SetAdsMovementMultiplier(float multiplier)
    {
        float next = float.IsFinite(multiplier) ? Mathf.Clamp(multiplier, 0.1f, 1f) : 1f;
        if (Mathf.IsEqualApprox(next, _adsMovementMultiplier)) return;
        _adsMovementMultiplier = next; SetPhysicsProcess(true);
    }

    // =========================================================
    // =========================================================
    // Separate from ADS: the active firearm's carried weight applies whenever equipped.
    public void SetEquippedWeaponMovementMultiplier(float multiplier)
    {
        float next = float.IsFinite(multiplier) ? Mathf.Clamp(multiplier, 0.1f, 1f) : 1f;
        if (Mathf.IsEqualApprox(next, _equippedMovementMultiplier)) return;
        _equippedMovementMultiplier = next; SetPhysicsProcess(true);
    }

    // =========================================================
    protected override float GetMovementSpeedMultiplier() => _equippedMovementMultiplier * _adsMovementMultiplier;

    // =========================================================
    // Keep the existing actor identity for zombies, but hide its third-person art from the eye camera.
    public override void Initialize(PlayerSettings settings, Camera3D camera)
    {
        _view = (FirstPersonCamera)camera; _fps = _view.ViewSettings;
        _adsMovementMultiplier = _equippedMovementMultiplier = 1f;
        var movement = (PlayerSettings)settings.Duplicate(true);
        movement.WalkSpeed = Mathf.Max(0.1f, _fps.WalkSpeed);
        movement.RunMultiplier = Mathf.Max(1, _fps.RunMultiplier);
        movement.CrouchSpeedMultiplier = Mathf.Clamp(_fps.CrouchSpeedMultiplier, 0.1f, 1);
        base.Initialize(movement, camera); VisualRoot.Hide();
        FloorSnapLength = Mathf.Clamp(_fps.FloorSnapDistance, 0.05f, 0.75f);
        FloorMaxAngle = Mathf.DegToRad(Mathf.Clamp(_fps.MaximumSlopeDegrees, 10, 60));
        FloorStopOnSlope = true; FloorConstantSpeed = true;
        SafeMargin = Mathf.Clamp(_fps.CollisionSafeMargin, 0.001f, 0.05f);
    }

    // =========================================================
    // Measure post-collision horizontal travel once, after the shared player physics driver completes.
    public override void _PhysicsProcess(double delta)
    {
        Vector3 before = GlobalPosition;
        base._PhysicsProcess(delta);
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        bool movingInput = ReadMovementInput().LengthSquared() > 0.01f;
        _view.SetLeanAmount(LeanAmount);
        _view.RecordPhysicsPose(CapsuleHeight, travel.Length(), IsOnFloor(),
            movingInput, CurrentStance, (float)delta);
    }

    // =========================================================
    // Fast braking and reversal avoid floatiness; shared overhead movement retains its original formula.
    protected override Vector3 UpdateHorizontalVelocity(Vector3 current, Vector3 target, float dt)
    {
        float rate = target.LengthSquared() < 0.0001f ? _fps.Deceleration
            : current.Dot(target) < 0 ? _fps.DirectionChangeAcceleration : _fps.Acceleration;
        return current.MoveToward(target, Mathf.Max(1, rate) * dt);
    }

    // =========================================================
    // The FPS debug view only jumps when the mouse is captured for gameplay.
    protected override bool CanAcceptJumpInput() => Input.MouseMode == Input.MouseModeEnum.Captured;

    // =========================================================
    protected override bool CanAcceptLeanInput() => Input.MouseMode == Input.MouseModeEnum.Captured;

    // =========================================================
    // Releases and camera capture changes wake the shared movement driver so velocity can settle.
    protected override Vector2 ReadMovementInput() => Input.MouseMode == Input.MouseModeEnum.Captured
        ? base.ReadMovementInput() : Vector2.Zero;

    // =========================================================
    // Read current yaw only while moving; looking up/down cannot change walking speed or introduce flight.
    protected override Vector3 GetMovementDirection(Vector2 keys)
    {
        Vector3 forward = -_view.GlobalBasis.Z; forward.Y = 0;
        // Roll must not rotate the strafing axis; derive it from the level look-forward vector.
        Vector3 right = forward.Cross(Vector3.Up); right.Y = 0;
        return (right.Normalized() * keys.X + forward.Normalized() * keys.Y).Normalized();
    }
}
