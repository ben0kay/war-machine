# EQUIPMENT — reusable weapons and handheld tools

The first usable firearm is implemented and integrated into the active top-down scene.
**Awaiting local compilation and visual/physics validation.** Read root CodePreferences.md.

- `Configuration/EquipmentMaster.tres`: enable equipment and choose the default weapon scene.
- `Shared/C/EquipmentConfig.cs`: small composition resource, independent of the player.
- `Weapons/Shared/C/FirearmController.cs`: shared firing, cadence, hitscan, obstruction and damage dispatch.
- `Weapons/Shared/C/FirearmSettings.cs`: typed, reusable firearm stats resource.
- `Weapons/Shared/C/FirearmShotEffects.cs`: reused shot feedback, dormant after expiry.
- `Weapons/Rifles/Rifle_001/`: the first scene, editable stats and temporary C# visual.
- `PLAYER/Equipment/`: mouse/camera input adapter; actor pose remains in TopDownPlayer.
- `ENTITIES/Shared/Combat/IDamageReceiver.cs`: shared hit contract, currently supported by zombies.
- `Tools/`: reserved for later tools; no inventory, reload or ammo economy yet; simple 1/2 test switching now exists.

`C/` contains procedural C# mesh generation and scripts. `Artwork/` is for later
imported art. Item `Configuration/` holds `.tres` definitions. Keep firing code
shared from the first weapon onward: do not create a rifle-specific controller or
copy stat classes for a pistol. Future projectile/melee/tool behaviour should be
added deliberately when requested; hitscan is not a universal projectile simulator.

Rifle starts equipped while the master is enabled; 1/2 select rifle/machine gun. Mouse aims; left click or
hold fires slowly. Arrows, Shift, right Ctrl crouch and camera controls remain.
UI clicks are excluded; focus loss cancels held fire. Robot and parked FPS world
are unchanged. See the rifle README for controls, stats and local check steps.

Performance: input, actor/camera changes wake one physics aim update; no idle aim
loop. Held fire sleeps on a one-shot physics cooldown timer, then requests its next
shot. Queries exclude the holder. Feedback reuses two mesh instances and sleeps
after its brief lifetime. No projectile nodes, target scans or per-zombie hit polls.
Audio is optional via the stats resource; no sound asset is included by default.

## Two test weapon slots

Shared EQUIPMENT/Shared/WeaponSlots owns two explicit scenes from EquipmentMaster: DefaultWeaponScene (1, Rifle_001) and SecondaryWeaponScene (2, MachineGun_001). Both overhead PlayerWeaponController and FPS FirstPersonWeapon use the same slots, with their own hand/camera mounts and existing aim adapters. Press top-row **1** or **2** after GUI refusal; FPS requires mouse capture. Rifle starts active. Weapon stats/automatic fire/spread/pooled tracers stay in the shared firearm implementation.

Switching cancels the trigger/pending shot, unsubscribes the old cooldown callback, stops old VFX/audio via Unequip, hides and disables the old gun, equips the new one and updates the adapter's hit mask. Click again to fire. Scenes instantiate lazily once per slot (maximum two); returning reuses the instance. No frame loop, inventory, reload/ammo, pickups or new weapon-specific firing code. Current test switching resets the unequipped gun's cooldown through existing Unequip semantics. Unload explicitly frees camera/hand-mounted weapons and releases subscriptions. Missing/invalid replacement leaves the old gun selected.

Local validation pending: Godot/.NET unavailable here. Build, F6 World/FPS, 1/2 repeatedly, shoot/hold then switch, release/focus loss, GUI slider clicks and scene unload. Confirm only selected mesh draws, only selected gun fires, MG tracers/spread work, and both mounts/muzzles look right.

## Planned melee equipment and hit reactions

The [CM implementation plan](../COMBAT/IMPLEMENTATION_PLAN.md) documents shared bullet flinch, firearm-butt shove, a bat that can strike up to three zombies per swing, a one-target knife, and later zombie attacks. None are implemented yet. Current `WeaponSlots` remain two firearm-only test slots; an item capability extension is **proposed only for CM3**, not part of the current hotbar. Cross-actor impact and stagger ownership is planned under `COMBAT/`; the working damage pipeline stays under `ENTITIES/Shared/Combat/` until any separately authorized move.
