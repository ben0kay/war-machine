// Equipped melee primary action: shared timing, query dispatch and bat swing presentation; view adapters advance only active swings.
using Godot;

// The selected move changes the one existing attack profile, not the shared contact/damage system.
public enum MeleeStrikeKind { Sweep, Overhead }

public partial class MeleeWeaponController : Node3D
{
    [Export] public MeleeSettings Settings { get; set; } = null!;
    public MeleeSettings RuntimeSettings { get; private set; } = null!;
    private MeleeSettings _overheadSettings = null!, _activeSettings = null!;
    private MeleeStrikeKind _activeStrike;
    private CollisionObject3D _holder = null!;
    private MeleeAttack? _attack;
    private PlayerController? _player;
    private Transform3D _rest;
    private Vector3 _direction;
    private bool _firstPerson;
    public bool IsAttacking => _attack?.IsActive == true;

    // =========================================================
    public override void _Ready()
    {
        RuntimeSettings = (MeleeSettings)Settings.Duplicate(true);
        // Build the alternate profile once; same cached MeleeAttack, but capped at exactly one actor.
        _overheadSettings = (MeleeSettings)RuntimeSettings.Duplicate(true);
        _overheadSettings.Damage = RuntimeSettings.AlternateDamage;
        _overheadSettings.ArcDegrees = RuntimeSettings.AlternateArcDegrees;
        _overheadSettings.VerticalTolerance = RuntimeSettings.AlternateVerticalTolerance;
        _overheadSettings.SweepThickness = RuntimeSettings.AlternateSweepThickness;
        _overheadSettings.MaximumTargets = 1;
        _overheadSettings.WindupSeconds = RuntimeSettings.AlternateWindupSeconds;
        _overheadSettings.ActiveSeconds = RuntimeSettings.AlternateActiveSeconds;
        _overheadSettings.RecoverySeconds = RuntimeSettings.AlternateRecoverySeconds;
        _overheadSettings.CooldownSeconds = RuntimeSettings.AlternateCooldownSeconds;
        _activeSettings = RuntimeSettings;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    public void Equip(CollisionObject3D holder)
    { Cancel(); if (_holder != holder) _attack = new MeleeAttack(holder); _holder = holder; }

    // =========================================================
    public void SetView(bool firstPerson)
    { var hands = GetNodeOrNull<Node3D>("Visual/Hands"); if (hands != null) hands.Visible = firstPerson; }

    // =========================================================
    // One click starts one move. Both strikes share the same cooldown and active damage pipeline.
    // Facing locks at acceptance; movement still carries the existing contact origin.
    public bool TryStart(PlayerController player, Vector3 aim, bool firstPerson,
        MeleeStrikeKind strike = MeleeStrikeKind.Sweep)
    {
        aim.Y = 0;
        if (!aim.IsFinite() || aim.LengthSquared() < 0.001f || _attack == null) return false;
        if (strike == MeleeStrikeKind.Overhead && !RuntimeSettings.AlternateAttackEnabled) return false;
        MeleeSettings profile = strike == MeleeStrikeKind.Overhead ? _overheadSettings : RuntimeSettings;
        if (!_attack.TryStart(profile)) return false;
        _activeSettings = profile; _activeStrike = strike;
        _player = player; _direction = aim.Normalized(); _firstPerson = firstPerson; _rest = Transform;
        ApplyPose(0); return true;
    }

    // =========================================================
    // Query only in the active window; dispatch once per deduplicated contact through shared anatomy/damage/impact.
    public void Advance(float delta, Vector3 origin)
    {
        if (!IsAttacking) return;
        var contacts = _attack!.AdvanceSweep(delta, origin, _direction);
        foreach (MeleeContact contact in contacts)
        {
            if (!IsInstanceValid(contact.Collider) || contact.Collider.IsQueuedForDeletion()) continue;
            DamagePacket damage = new(_activeSettings.Damage, _activeSettings.DamageKind, _holder,
                contact.Position, contact.Direction, severPower: _activeSettings.SeverPower);
            CombatImpact impact = new(_holder, contact.Position, contact.Direction,
                _activeSettings.ImpactKind, _activeSettings.ImpactStrength, _activeSettings.ImpactDuration);
            HitTargetResolver.Dispatch(contact.Collider, _holder, damage, impact, contact.ShapeIndex);
        }
        if (IsAttacking) ApplyPose(_attack.ElapsedSeconds); else { _attack.Cancel(); RestorePose(); }
    }

    // =========================================================
    // Coil beside the shoulder, sweep flat across the body, follow through, then recover to the authored hold.
    private void ApplyPose(float elapsed)
    {
        if (_firstPerson)
        {
            if (_activeStrike == MeleeStrikeKind.Overhead)
                FirstPersonMeleeSwing.ApplyOverhead(this, _rest, _activeSettings, elapsed);
            else FirstPersonMeleeSwing.Apply(this, _rest, _activeSettings, elapsed);
            return;
        }
        float windup = Safe(_activeSettings.WindupSeconds, 0.001f, 1);
        float active = Safe(_activeSettings.ActiveSeconds, 0.001f, 1);
        float recovery = Safe(_activeSettings.RecoverySeconds, 0.001f, 1);
        if (_activeStrike == MeleeStrikeKind.Overhead)
        {
            ApplyOverheadPose(elapsed, windup, active, recovery);
            return;
        }
        float half = Mathf.DegToRad(Safe(_activeSettings.ArcDegrees, 10, 180)) * 0.5f;
        float yaw, pitch;
        float swing;
        if (elapsed < windup)
        {
            float t = Smooth(elapsed / windup);
            yaw = Mathf.Lerp(0f, half, t); pitch = Mathf.Lerp(0f, -0.28f, t); swing = -t;
        }
        else if (elapsed < windup + active)
        {
            float t = Mathf.Clamp((elapsed - windup) / active, 0f, 1f);
            yaw = Mathf.Lerp(half, -half, t); pitch = -0.28f * (1f - t); swing = Mathf.Lerp(-1f, 1f, t);
        }
        else
        {
            float t = Smooth((elapsed - windup - active) / recovery);
            yaw = Mathf.Lerp(-half, 0f, t); pitch = 0f; swing = 1f - t;
        }
        // Rotate around the held handle in the player's local socket space.
        // Existing aimed WeaponMount sets forward; hand IK tracks this grip every active tick.
        Transform = new Transform3D(_rest.Basis * new Basis(Vector3.Up, yaw)
            * new Basis(Vector3.Right, pitch), _rest.Origin);
        _player!.SetBatSwingPose(swing);
    }

    // =========================================================
    // A distinct overhead bat arc driven by the same accepted attack clock and top-down hand IK.
    private void ApplyOverheadPose(float elapsed, float windup, float active, float recovery)
    {
        float pitch, swing;
        if (elapsed < windup)
        {
            float t = Smooth(elapsed / windup);
            pitch = Mathf.Lerp(0f, 1.12f, t);
            swing = -0.24f * t;
        }
        else if (elapsed < windup + active)
        {
            float t = Mathf.Clamp((elapsed - windup) / active, 0f, 1f);
            pitch = Mathf.Lerp(1.12f, -0.83f, t);
            swing = Mathf.Lerp(-0.24f, 0.24f, t);
        }
        else
        {
            float t = Smooth((elapsed - windup - active) / recovery);
            pitch = Mathf.Lerp(-0.83f, 0f, t);
            swing = 0.24f * (1f - t);
        }
        Transform = new Transform3D(_rest.Basis * new Basis(Vector3.Right, pitch), _rest.Origin);
        _player!.SetBatSwingPose(swing);
    }

    // =========================================================
    private static float Safe(float value, float min, float max)
        => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;

    // =========================================================
    private static float Smooth(float value)
    { float t = Mathf.Clamp(value, 0, 1); return t * t * (3 - 2 * t); }

    // =========================================================
    private void RestorePose()
    {
        Transform = _rest;
        if (!_firstPerson && IsInstanceValid(_player)) _player!.SetBatSwingPose(0f);
        _player = null;
    }

    // =========================================================
    // Preserve cooldown on interruption; focus, UI, switch and unload cannot leave damage windows or pose active.
    public void Cancel() { bool active = IsAttacking; _attack?.Cancel(); if (active) RestorePose(); }
}
