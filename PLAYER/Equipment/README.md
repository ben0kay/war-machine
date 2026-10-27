# Player equipment adapter

C/PlayerWeaponController.cs equips the scene selected by EquipmentMaster and
bridges mouse/camera/player changes to shared FirearmController. It owns input
and aim queries only; it has no rifle path, damage, range or cooldown constants.
The player owns WeaponMount and its holding pose. Camera ViewChanged covers
follow, zoom, pitch and projection. No settled idle physics/process callback.
Release is observed before GUI handling; presses only after GUI refusal.
Focus loss/unload cancels the trigger and releases subscriptions/effects.

## Ownership cleanup

PlayerWeaponController.cs lives directly here and consumes the active PlayerController and CAMERA/Camera.cs; gameplay weapon logic remains in EQUIPMENT.

## Two test weapon slots

Shared EQUIPMENT/Shared/WeaponSlots owns two explicit scenes from EquipmentMaster: DefaultWeaponScene (1, Rifle_001) and SecondaryWeaponScene (2, MachineGun_001). Both overhead PlayerWeaponController and FPS FirstPersonWeapon use the same slots, with their own hand/camera mounts and existing aim adapters. Press top-row **1** or **2** after GUI refusal; FPS requires mouse capture. Rifle starts active. Weapon stats/automatic fire/spread/pooled tracers stay in the shared firearm implementation.

Switching cancels the trigger/pending shot, unsubscribes the old cooldown callback, stops old VFX/audio via Unequip, hides and disables the old gun, equips the new one and updates the adapter's hit mask. Click again to fire. Scenes instantiate lazily once per slot (maximum two); returning reuses the instance. No frame loop, inventory, reload/ammo, pickups or new weapon-specific firing code. Current test switching resets the unequipped gun's cooldown through existing Unequip semantics. Unload explicitly frees camera/hand-mounted weapons and releases subscriptions. Missing/invalid replacement leaves the old gun selected.

Local validation pending: Godot/.NET unavailable here. Build, F6 World/FPS, 1/2 repeatedly, shoot/hold then switch, release/focus loss, GUI slider clicks and scene unload. Confirm only selected mesh draws, only selected gun fires, MG tracers/spread work, and both mounts/muzzles look right.
