// RC1: top-down arm/shoulder reaction wrapped around shared, cosmetic-only firearm visual recoil.
using Godot;

public sealed class TopDownWeaponRecoil
{
    private readonly PlayerController _player;
    private readonly FirearmVisualRecoil _gun;
    public bool IsActive => _gun.IsActive;

    // =========================================================
    // Reuse the identical rifle/machine-gun visual kick used in FPS, plus top-down arm response.
    public TopDownWeaponRecoil(PlayerController player, FirearmController firearm)
    { _player = player; _gun = new FirearmVisualRecoil(firearm); }

    // =========================================================
    public void Kick() { _gun.Kick(); UpdateArms(); }

    // =========================================================
    public void Advance(float delta) { _gun.Advance(delta); UpdateArms(); }

    // =========================================================
    public void Cancel() { _gun.Cancel(); UpdateArms(); }

    // =========================================================
    // Player rig alone owns the arm joints; shared visual kick only moves the gun model.
    private void UpdateArms()
    { if (GodotObject.IsInstanceValid(_player)) _player.SetWeaponRecoilPose(_gun.Amount); }
}
