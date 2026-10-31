# Equipped melee

`Shared/MeleeWeaponController` owns the equipped primary attack and cosmetic sweep; `COMBAT/Melee/MeleeAttack` remains the common query/timing owner. `BaseballBat_001` is the first consumer. No firearm inheritance/fake ammo or duplicate damage maths. Inputs remain in the top-down and FPS adapters; shared WeaponSlots lazily selects two guns and one melee scene. A bat selection sends null through the existing firearm-selection event so AmmoHud hides.

No independent physics/frame callback on equipped melee. View adapters advance while a swing is active and sleep after recovery/cancellation. See [bat setup/acceptance](BaseballBat_001/README.md).

## CM4 dual-action bat (2026-10-31)

The same equipped bat now exposes LMB multi-actor horizontal swipe and MMB one-target overhead heavy blow (20 base physical damage). Both invoke the existing active MeleeAttack and HitTargetResolver; no additional combat singleton, collision shape or idle loop. Per-bat alternate settings and distinct FPS/top-down poses are documented in [BaseballBat_001](BaseballBat_001/README.md#cm4--mmb-overhead-heavy-strike-2026-10-31). MMB on FPS firearms remains ADS, not melee. Source implemented; gameplay validation pending.
