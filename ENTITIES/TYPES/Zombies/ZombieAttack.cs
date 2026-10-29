// CM5: actor-owned telegraphed melee using the shared executor, without new callbacks or scans.
// ELD3 capability selection remains separate; this first standing strike is disabled for low bodies.
using Godot;

public partial class ZombieController
{
    private MeleeAttack? _attack;
    private MeleeSettings _attackProfile = null!;
    private Vector3 _committedAttackAim;
    private float _attackSide = 1f; // Alternates per committed swipe, initially spawn-seeded.
    private bool Attacking => _attack?.IsActive == true;

    // =========================================================
    // Cache the isolated runtime profile once, following EntityCore configuration.
    private void InitializeAttack()
    {
        _attack = new MeleeAttack(this);
        _attackProfile = _config.AttackProfile ?? new MeleeSettings {
            Range = 1.65f, ArcDegrees = 80, VerticalTolerance = 0.8f, HitMask = 23,
            WindupSeconds = 0.45f, ActiveSeconds = 0.12f, RecoverySeconds = 0.55f,
            CooldownSeconds = 1.5f, Damage = 10, ImpactStrength = 0
        };
    }

    // =========================================================
    // Current target only; other actors still block cover rays. No player/camera/input dependency.
    private bool CanAttackTarget()
        => IsChasing && CanContinueAttack();

    // =========================================================
    // Losing sight/reach does not undo a committed swipe; lifecycle and accepted reactions still interrupt.
    private bool CanContinueAttack()
        => _config.AttacksEnabled && !_reaction.IsActive && !LowBodyActive
            && _locomotion.CanDrive && IsInstanceValid(_target) && _target!.IsInsideTree()
            && !_target.IsQueuedForDeletion() && _target is IDamageReceiver
            && !(_target is ICombatLifeState life && life.IsDead);

    // =========================================================
    // Shared eye/torso samples support standing/crouched targets without guessing capsule height.
    private Vector3 AttackOrigin => GlobalPosition + Vector3.Up * 1.05f;
    private Vector3 AttackAim => Vector3.Forward.Rotated(Vector3.Up, VisualRoot.Rotation.Y);
    private bool InAttackReach()
    {
        if (_sightTarget == null) return false;
        Vector3 offset = _sightTarget.PerceptionTorso - AttackOrigin;
        if (Mathf.Abs(offset.Y) > Mathf.Clamp(_attackProfile.VerticalTolerance, 0.25f, 3)) return false;
        offset.Y = 0;
        float distance = offset.Length();
        return distance > 0.001f && distance <= Mathf.Clamp(_attackProfile.Range, 0.25f, 5)
            && AttackAim.Dot(offset / distance) >= Mathf.Cos(Mathf.DegToRad(_attackProfile.ArcDegrees * 0.5f));
    }

    // =========================================================
    // Called by the existing staggered brain; only near, pursuing actors start a bounded action.
    private void TryStartAttack()
    {
        if (CanAttackTarget() && InAttackReach() && _attack!.TryStart(_attackProfile))
        {
            _committedAttackAim = AttackAim;
            _attackSide = -_attackSide;
            Velocity = new Vector3(0, Velocity.Y, 0);
            _stuck = 0; SetPhysicsProcess(true);
        }
    }

    // =========================================================
    // Complete misses through recovery. Only the shared active-window query checks present reach/arc/cover.
    private void AdvanceAttack(float delta)
    {
        if (!Attacking) return;
        if (!CanContinueAttack()) { _attack!.Cancel(); return; }
        if (_attack!.Advance(delta, AttackOrigin, _committedAttackAim, MeleeAimMode.Horizontal,
            out MeleeContact contact, _target))
        {
            var hit = new DamagePacket(_attackProfile.Damage, _attackProfile.DamageKind,
                this, contact.Position, contact.Direction);
            HitTargetResolver.Dispatch(contact.Collider, this, hit, null);
        }
    }

    // =========================================================
    // CM5-A1: one raised claw swipes horizontally across the chest while the waist/head twist.
    // Visual-only overlay on the existing windup/hit/recovery clock; neither reach nor collision changes.
    private void ApplyAttackPose()
    {
        if (!Attacking) return;
        float elapsed = _attack!.ElapsedSeconds;
        float windup = Mathf.Clamp(_attackProfile.WindupSeconds, 0.01f, 1f);
        float active = Mathf.Clamp(_attackProfile.ActiveSeconds, 0.01f, 1f);
        float recovery = Mathf.Clamp(_attackProfile.RecoverySeconds, 0.01f, 1f);
        float roll, twist, forward, shift, reach, sweep;
        if (elapsed < windup)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / windup);
            roll = -0.65f * t; twist = -0.70f * t; forward = -0.20f * t;
            shift = -0.40f * t; reach = t; sweep = -1f;
        }
        else if (elapsed < windup + active)
        {
            float t = Mathf.SmoothStep(0f, 1f, (elapsed - windup) / active);
            roll = Mathf.Lerp(-0.65f, 1f, t); twist = Mathf.Lerp(-0.70f, 1f, t);
            forward = Mathf.Lerp(-0.20f, 1f, t); shift = Mathf.Lerp(-0.40f, 1f, t);
            reach = 1f; sweep = Mathf.Lerp(-1f, 1f, t);
        }
        else
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((elapsed - windup - active) / recovery, 0f, 1f));
            roll = 1f - t; twist = 1f - t; forward = 1f - t;
            shift = 1f - t; reach = 1f - t; sweep = 1f;
        }

        float side = _attackSide; // -1: left claw leads; +1: right claw leads.
        float chestRoll = Mathf.DegToRad(Mathf.Clamp(_config.AttackTorsoRollDegrees, 0f, 30f)) * roll * side;
        float chestTwist = Mathf.DegToRad(Mathf.Clamp(_config.AttackTorsoTwistDegrees, 0f, 25f)) * twist * side;
        float chestPitch = Mathf.DegToRad(Mathf.Clamp(_config.AttackForwardLungeDegrees, 0f, 35f)) * forward;
        _attackUpperBody.Rotation = new Vector3(-chestPitch, chestTwist, chestRoll);
        _attackUpperBody.Position = _upperBodyRest + new Vector3(
            side * Mathf.Clamp(_config.AttackSideShift, 0f, 0.18f) * shift, 0f, -0.03f * Mathf.Max(0f, forward));
        float headSway = Mathf.DegToRad(Mathf.Clamp(_config.AttackHeadSwayDegrees, 0f, 20f));
        _anatomyHead.Rotation += new Vector3(-chestPitch * 0.25f, chestTwist * 0.35f, side * headSway * roll);

        // Lift the striking arm once in windup, then keep its elevation almost fixed.
        // Sweep around its local Y axis: outside shoulder -> across the player's chest -> recover.
        // The other arm stays slightly lower and counterbalances; the original gait is restored after attack.
        float clawPitch = Mathf.DegToRad(Mathf.Clamp(_config.AttackSwipeArmPitchDegrees, 40f, 100f));
        float yaw = side * Mathf.DegToRad(Mathf.Clamp(_config.AttackHorizontalSwipeDegrees, 30f, 140f)) * 0.5f * sweep * reach;
        bool leftLead = side < 0f;
        Vector3 leading = leftLead ? _leftArm.Rotation : _rightArm.Rotation;
        Vector3 following = leftLead ? _rightArm.Rotation : _leftArm.Rotation;
        leading.X = Mathf.Lerp(leading.X, clawPitch, reach);
        following.X = Mathf.Lerp(following.X, clawPitch * 0.72f, reach);
        leading.Y += yaw;
        following.Y -= yaw * 0.18f;
        leading.Z += (leftLead ? -0.12f : 0.12f) * reach;
        if (leftLead) { _leftArm.Rotation = leading; _rightArm.Rotation = following; }
        else { _rightArm.Rotation = leading; _leftArm.Rotation = following; }
    }
}
