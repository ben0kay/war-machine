# Equipped melee

`Shared/MeleeWeaponController` owns the equipped primary attack and cosmetic sweep; `COMBAT/Melee/MeleeAttack` remains the common query/timing owner. `BaseballBat_001` is the first consumer. No firearm inheritance/fake ammo or duplicate damage maths. Inputs remain in the top-down and FPS adapters; shared WeaponSlots lazily selects two guns and one melee scene. A bat selection sends null through the existing firearm-selection event so AmmoHud hides.

No independent physics/frame callback on equipped melee. View adapters advance while a swing is active and sleep after recovery/cancellation. See [bat setup/acceptance](BaseballBat_001/README.md).
