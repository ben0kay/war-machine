# Shared melee equipment adapter

MeleeWeaponController duplicates its MeleeSettings once, retains a reusable MeleeAttack, and dispatches confirmed contacts through HitTargetResolver. It exposes Equip, TryStart, Advance and Cancel. Current cosmetic pose is the baseball-bat coil/side sweep/follow-through; future thrust/knife variants require separately authorized presentation work, not another damage system. View-specific offsets are authored in the melee master. No idle callbacks.

## CM3 bat presentation refinement — 2026-10-28

`FirstPersonMeleeSwing.cs` is a small FPS-only camera-local procedural arc, invoked from `MeleeWeaponController.ApplyPose` when first-person. It animates the actual model/grip position from left to right as well as yaw/pitch/roll, rather than using the shared overhead world-oriented bat pose. Gameplay contact remains in the same `MeleeAttack.AdvanceSweep` event window. No per-frame work outside active attacks. `MeleeSettings` adds two FPS-only swing extent fields, configured in the bat master; the top-down animation path remains untouched. Zombie backsteps are owned by their actor reaction, not by this model renderer.

## Camera-space sideways tilt — 2026-10-28

The FPS-only `FirstPersonMeleeSwing` now applies a configurable camera-forward screen rotation to the composed swing orientation, tilting the long bat barrel sideways instead of merely twisting around its own length. The tilt interpolates up during wind-up and down during recovery and costs nothing at idle. `MeleeSettings.FirstPersonSideTiltDegrees` (default 90° for the bat) owns the tuning. Shared melee contacts, zombie impact rules and overhead visuals remain untouched. Local visual acceptance pending.


## Top-down right-arm pose coupling — 2026-10-28

Overhead `MeleeWeaponController.ApplyPose` now rotates the bat locally under the already-aimed player WeaponMount, sends an attack-phase cue into `PlayerController.SetBatSwingPose`, and restores the cue at completion/cancellation. `PLAYER/Visuals/PlayerBatPose` owns the visual shoulder/arm/torso response and maintains an IK right-hand grip on the existing bat `GripRight` marker; Q shove remains authoritative when active. FPS skips this cue and continues using `FirstPersonMeleeSwing`. No new hit/physics loops or changes to contact, cooldown, zombie displacement or equipment shared ownership. Local Godot build/visual tests pending.
