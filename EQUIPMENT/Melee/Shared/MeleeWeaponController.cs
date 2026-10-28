// Equipped melee primary action: shared timing, query dispatch and bat swing presentation; view adapters advance only active swings.
using Godot;

public partial class MeleeWeaponController : Node3D
{
    [Export] public MeleeSettings Settings { get; set; } = null!;
    public MeleeSettings RuntimeSettings { get; private set; } = null!;
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
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    public void Equip(CollisionObject3D holder)
    { Cancel(); if (_holder != holder) _attack = new MeleeAttack(holder); _holder = holder; }

    // =========================================================
    public void SetView(bool firstPerson)
    { var hands = GetNodeOrNull<Node3D>("Visual/Hands"); if (hands != null) hands.Visible = firstPerson; }

    // =========================================================
    // One click starts one swing. Facing locks at acceptance; movement can still carry the swing origin.
    public bool TryStart(PlayerController player, Vector3 aim, bool firstPerson)
    {
        aim.Y = 0;
        if (!aim.IsFinite() || aim.LengthSquared() < 0.001f || _attack == null || !_attack.TryStart(RuntimeSettings)) return false;
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
            DamagePacket damage = new(RuntimeSettings.Damage, RuntimeSettings.DamageKind, _holder,
                contact.Position, contact.Direction, severPower: RuntimeSettings.SeverPower);
            CombatImpact impact = new(_holder, contact.Position, contact.Direction,
                RuntimeSettings.ImpactKind, RuntimeSettings.ImpactStrength, RuntimeSettings.ImpactDuration);
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
            FirstPersonMeleeSwing.Apply(this, _rest, RuntimeSettings, elapsed);
            return;
        }
        float windup = Safe(RuntimeSettings.WindupSeconds, 0.001f, 1);
        float active = Safe(RuntimeSettings.ActiveSeconds, 0.001f, 1);
        float recovery = Safe(RuntimeSettings.RecoverySeconds, 0.001f, 1);
        float half = Mathf.DegToRad(Safe(RuntimeSettings.ArcDegrees, 10, 180)) * 0.5f;
        float holdPitch = Mathf.DegToRad(Safe(RuntimeSettings.HoldPitchDegrees, 0, 85));
        float holdYaw = Mathf.DegToRad(Safe(RuntimeSettings.HoldYawDegrees, -90, 90));
        float yaw, pitch;
        if (elapsed < windup)
        { float t = Smooth(elapsed / windup); yaw = Mathf.Lerp(holdYaw, half, t); pitch = Mathf.Lerp(holdPitch, 0, t); }
        else if (elapsed < windup + active)
        { float t = Mathf.Clamp((elapsed - windup) / active, 0, 1); yaw = Mathf.Lerp(half, -half, t); pitch = 0; }
        else
        { float t = Smooth((elapsed - windup - active) / recovery); yaw = Mathf.Lerp(-half, holdYaw, t); pitch = Mathf.Lerp(0, holdPitch, t); }
        // Yaw corresponds to the same horizontal angular sector used by AdvanceSweep.
        GlobalBasis = Basis.LookingAt(_direction, Vector3.Up) * new Basis(Vector3.Up, yaw) * new Basis(Vector3.Right, pitch);
        if (!_firstPerson) _player!.SetShoveGrip(this, 1);
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
        if (!_firstPerson && IsInstanceValid(_player)) _player!.SetShoveGrip(null, 0);
        _player = null;
    }

    // =========================================================
    // Preserve cooldown on interruption; focus, UI, switch and unload cannot leave damage windows or pose active.
    public void Cancel() { bool active = IsAttacking; _attack?.Cancel(); if (active) RestorePose(); }
}
