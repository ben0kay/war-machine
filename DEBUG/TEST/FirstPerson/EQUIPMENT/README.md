# EQUIPMENT

`FirstPersonWeapon.cs` supplies centre-screen aim and trigger requests to the configured shared firearm. Queries run only for shots; shared EQUIPMENT owns damage, cooldown, muzzle obstruction and effects.

## Two test weapon slots

Shared EQUIPMENT/Shared/WeaponSlots owns two explicit scenes from EquipmentMaster: DefaultWeaponScene (1, Rifle_001) and SecondaryWeaponScene (2, MachineGun_001). Both overhead PlayerWeaponController and FPS FirstPersonWeapon use the same slots, with their own hand/camera mounts and existing aim adapters. Press top-row **1** or **2** after GUI refusal; FPS requires mouse capture. Rifle starts active. Weapon stats/automatic fire/spread/pooled tracers stay in the shared firearm implementation.

Switching cancels the trigger/pending shot, unsubscribes the old cooldown callback, stops old VFX/audio via Unequip, hides and disables the old gun, equips the new one and updates the adapter's hit mask. Click again to fire. Scenes instantiate lazily once per slot (maximum two); returning reuses the instance. No frame loop, inventory, reload/ammo, pickups or new weapon-specific firing code. Current test switching resets the unequipped gun's cooldown through existing Unequip semantics. Unload explicitly frees camera/hand-mounted weapons and releases subscriptions. Missing/invalid replacement leaves the old gun selected.

## Per-weapon FPS framing

The FPS mount starts at `FirstPersonMaster.WeaponOffset`, then adds the equipped
`FirearmSettings.FirstPersonOffset` and applies `FirstPersonScale` to that firearm
root. These adjustments are applied **only when selecting a weapon in the FPS
adapter**, not in `WeaponSlots` or the top-down player. The weapon root includes
its `Muzzle` marker, so the shot ray and cover obstruction check stay aligned
with the resized visual. Default values (zero offset, scale 1) preserve rifle
framing. The machine gun is positioned lower, slightly farther forward/right and
scaled to 78% to keep its stock/handle clear of the centre of the screen.
Adjust `MachineGunMaster.tres` if further tuning is desired.

Local validation pending: Godot/.NET unavailable here. Build, F6 World/FPS, 1/2 repeatedly, shoot/hold then switch, release/focus loss, GUI slider clicks and scene unload. Confirm only selected mesh draws, only selected gun fires, MG tracers/spread work, and both mounts/muzzles look right.
