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

## RC2 — real FPS recoil (2026-10-28)

`FirstPersonWeapon.cs` now uses the existing recoil-aware `TryFire(aim, origin, _camera.GlobalBasis.Y)`, so sustained automatic shots deviate **upward in real 3D space**, not by simply overlaying a screen-space streak. Random `ShotSpread` runs independently after the current recoil angle; the hit ray and tracer share the result. Mouse/look direction and centre crosshair are not forcibly rotated; mouse-look down can compensate.

The reusable `EQUIPMENT/Weapons/Shared/FirearmVisualRecoil.cs` animates each selected weapon's **Visual only** (short backward +Z kick / muzzle-up local X pitch), borrowing the authored VisualRecoil settings from its master. The actual root/Muzzle marker remains unchanged; this is why recoil is not applied twice. On the accepted-shot event, the FPS adapter wakes only while the kick settles; no visual idle loop. Slot changes, successful Q shove, Escape, loss of mouse capture, backpack input block, lost focus and scene unload cancel and restore the authored visual transform. The third-person top-down adapter shares the same helper through its arm/shoulder wrapper.

**No camera kick was added** in RC2; the camera is the actual firing-reference view and another rotation would alter gameplay aim twice. Build with `dotnet build war-machine.csproj` and F6 the inherited FirstPersonTest scene. Verify a stationary crosshair with upward-climbing MG hit rays, short burst control, rifle shot kick, model return, near-wall muzzle obstruction, swap/Q/Escape/backpack/ALT-TAB and FPS mouse look. Run one top-down F6 regression for the shared visual helper. **Source implemented; local Godot/.NET verification outstanding.**
