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

## CM2-D — side-on shove

Implemented: two-handed sideways gun push in overhead/FPS, driven by existing attack timing. Focus/menu/switch/unload cancels and restores the gun; pending fire waits for recovery. No idle pose processing. See COMBAT/README.md CM2-D handoff for tuning and deferred local build/visual checks. Prior no-animation statements are historical.

## AIM1 — Fade-aware top-down mouse aiming

**Implemented in source; awaiting local Godot validation.** The mouse aim ray alone uses `CAMERA/Occlusion/FadeAwareAim.cs`. Each procedural building collider shape is linked to its matching visual mesh once by `StructureGeometry.Box` or Building001's equivalent builder. `CameraOccluder` updates a tiny mesh metadata flag on transitions into/out of its **composed** camera, floor and exterior whole-building fade. On a changed mouse/camera/player aim, the adapter raycasts once, skips a *known faded shape* by advancing slightly beyond the hit, and repeats only as necessary with a 24-skip cap. Opaque shapes, even on the **same static body**, still stop selection. Unpaired shapes are never ignored. This is a **cursor targeting exception only**: the firearm's shot rays, ballistic obstruction, collision masks, actual damage, and movement/AI remain unchanged. FPS has its own aim adapter and is not affected.

On load/rebuild, section mesh owners discover existing prefab metadata as before; later buildings using StructureGeometry automatically gain shape pairing. Author-imported or custom collision builders must explicitly call `FadeAwareAim.Link(collisionShape, matchingMesh)` for every pair; see the structure authoring README. No whole-body collision disable, permanent input loop, additional physics tick or scene scan was added. Extra aim rays happen only when the ray intersects currently faded, linked geometry. Every aim ray stops after at most 25 queries; an extremely dense stack conservatively stops on the last checked hit.

**Local tests:** build C#; F6 WORLD/World.tscn. With a zombie behind a faded exterior wall, cursor/weapon should aim at the zombie while actual bullets still hit the wall. Repeat indoors with an individually faded wall and faded upper roof; verify opaque adjacent wall still stops the cursor and turning fading off/restoring opacity re-enables normal aiming. Check whole-building fade, same-body back wall, dynamically instanced buildings, terrain/props, movement collision and FPS firing. No Godot/.NET build or rendered hit test performed in this session.
