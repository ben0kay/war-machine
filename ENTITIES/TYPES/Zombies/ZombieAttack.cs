// CM5: actor-owned telegraphed melee using the shared executor, without new callbacks or scans.
// ELD3 capability selection remains separate; this first standing strike is disabled for low bodies.
using Godot;

public partial class ZombieController
{
    private MeleeAttack? _attack;
    private MeleeSettings _attackProfile = null!;
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
        => _config.AttacksEnabled && IsChasing && !_reaction.IsActive && !LowBodyActive
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
        { _stuck = 0; SetPhysicsProcess(true); }
    }

    // =========================================================
    // Existing active physics drives contact. Dodging, cover, reactions and death can prevent the strike.
    private void AdvanceAttack(float delta)
    {
        if (!Attacking) return;
        if (!CanAttackTarget() || !InAttackReach()) { _attack!.Cancel(); return; }
        if (_attack!.Advance(delta, AttackOrigin, AttackAim, MeleeAimMode.Horizontal,
            out MeleeContact contact, _target))
        {
            var hit = new DamagePacket(_attackProfile.Damage, _attackProfile.DamageKind,
                this, contact.Position, contact.Direction);
            HitTargetResolver.Dispatch(contact.Collider, this, hit, null);
        }
    }

    // =========================================================
    // Species gait resets the arms first; windup draws both arms back, then sweeps forward and recovers.
    private void ApplyAttackPose()
    {
        if (!Attacking) return;
        float elapsed = _attack!.ElapsedSeconds;
        float windup = Mathf.Clamp(_attackProfile.WindupSeconds, 0.01f, 1);
        float active = Mathf.Clamp(_attackProfile.ActiveSeconds, 0.01f, 1);
        float recovery = Mathf.Clamp(_attackProfile.RecoverySeconds, 0.01f, 1);
        float pose = elapsed < windup ? Mathf.Lerp(0, -0.65f, elapsed / windup)
            : elapsed < windup + active ? Mathf.Lerp(-0.65f, 0.45f, (elapsed - windup) / active)
            : Mathf.Lerp(0.45f, 0, Mathf.Clamp((elapsed - windup - active) / recovery, 0, 1));
        _leftArm.Rotation += new Vector3(pose, 0, -pose * 0.3f);
        _rightArm.Rotation += new Vector3(pose, 0, pose * 0.3f);
    }
}
