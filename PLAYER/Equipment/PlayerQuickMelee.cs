// Shared Q quick-shove adapter for the player in top-down and FPS.
// COMBAT/Melee owns detection; the existing CM0 dispatcher owns damage and impact.
using Godot;

public sealed class PlayerQuickMelee
{
    private readonly PlayerController _owner;
    private readonly MeleeAttack _attack;
    private readonly MeleeSettings _settings;
    private PlayerShovePose? _pose;
    private float _impactMomentumMultiplier = 1f;
    public bool IsActive => _attack.IsActive;

    // =========================================================
    // One physical key binding for both view adapters. Do not repeat this in either view.
    public static bool Requested(InputEvent input)
    { return input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.Q; }

    // =========================================================
    // Reuse a single detector and a per-player settings copy; nothing runs while idle.
    public PlayerQuickMelee(PlayerController owner)
    {
        _owner = owner;
        _attack = new MeleeAttack(owner);
        _settings = (MeleeSettings)GD.Load<MeleeSettings>("res://COMBAT/Configuration/QuickShoveMaster.tres").Duplicate(true);
    }

    // =========================================================
    // Capture real horizontal momentum when Q is pressed. The player can shove while idle,
    // walking or sprinting; later braking during windup cannot silently reduce its force.
    public bool TryStart(FirearmController weapon, bool firstPerson = false)
    {
        if (!_attack.TryStart(_settings)) return false;
        weapon.ResetRecoil(); // Q shove takes control of the weapon pose; discard stored gun recoil.
        _pose ??= new PlayerShovePose(_owner);
        _pose.Start(weapon, firstPerson);
        Vector3 velocity = _owner.Velocity;
        float speed = Mathf.Sqrt(velocity.X * velocity.X + velocity.Z * velocity.Z);
        float fullSpeed = Safe(_settings.FullMomentumSpeed, 0.1f, 20f);
        float idle = Safe(_settings.StationaryStrengthMultiplier, 0f, 5f);
        float running = Safe(_settings.RunningStrengthMultiplier, 0f, 5f);
        _impactMomentumMultiplier = Mathf.Lerp(idle, running, Mathf.Clamp(speed / fullSpeed, 0f, 1f));
        return true;
    }

    // =========================================================
    // Avoid propagating non-finite inspector values into the combat impact payload.
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }

    // =========================================================
    // Cancel the active window while preserving its existing cooldown.
    public void Cancel() { _attack.Cancel(); _pose?.Cancel(); }

    // =========================================================
    // Convert a confirmed contact into shared CM0 damage/impact once per attack.
    // The authored quick-shove master defaults to zero damage; no ammo or firearm timer.
    public void Advance(float delta, Vector3 origin, Vector3 aim, MeleeAimMode mode)
    {
        bool hit = _attack.Advance(delta, origin, aim, mode, out MeleeContact contact);
        if (_attack.IsActive) _pose?.Apply(_attack.ElapsedSeconds, _settings);
        else _pose?.Cancel();
        if (!hit) return;
        DamagePacket? damage = _settings.Damage > 0
            ? new DamagePacket(_settings.Damage, _settings.DamageKind, _owner, contact.Position, contact.Direction)
            : null;
        // Only the player's shove scales with momentum; damage and stagger duration stay fixed.
        float strength = _settings.ImpactKind == CombatImpactKind.Shove
            ? Safe(_settings.ImpactStrength, 0f, 10f) * _impactMomentumMultiplier
            : _settings.ImpactStrength;
        CombatImpact impact = new(_owner, contact.Position, contact.Direction,
            _settings.ImpactKind, strength, _settings.ImpactDuration);
        HitTargetResolver.Dispatch(contact.Collider, _owner, damage, impact);
    }
}
