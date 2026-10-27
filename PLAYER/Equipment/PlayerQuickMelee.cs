// Shared Q quick-shove adapter for the player in top-down and FPS.
// COMBAT/Melee owns detection; the existing CM0 dispatcher owns damage and impact.
using Godot;

public sealed class PlayerQuickMelee
{
    private readonly PlayerController _owner;
    private readonly MeleeAttack _attack;
    private readonly MeleeSettings _settings;
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
    public bool TryStart() => _attack.TryStart(_settings);

    // =========================================================
    // Cancel the active window while preserving its existing cooldown.
    public void Cancel() => _attack.Cancel();

    // =========================================================
    // Convert a confirmed contact into shared CM0 damage/impact once per attack.
    // The authored quick-shove master defaults to zero damage; no ammo or firearm timer.
    public void Advance(float delta, Vector3 origin, Vector3 aim, MeleeAimMode mode)
    {
        if (!_attack.Advance(delta, origin, aim, mode, out MeleeContact contact)) return;
        DamagePacket? damage = _settings.Damage > 0
            ? new DamagePacket(_settings.Damage, _settings.DamageKind, _owner, contact.Position, contact.Direction)
            : null;
        CombatImpact impact = new(_owner, contact.Position, contact.Direction,
            _settings.ImpactKind, _settings.ImpactStrength, _settings.ImpactDuration);
        HitTargetResolver.Dispatch(contact.Collider, _owner, damage, impact);
    }
}
