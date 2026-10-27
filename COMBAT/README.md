# COMBAT — cross-actor attacks, impacts and reactions

> **Status: planning only.** The combat roadmap is [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md), phases **CM0–CM5**. No melee weapon, gun shove, bullet stagger, knockback or zombie outgoing attack is implemented by creating this folder. Implement one separately authorized phase at a time on the current development branch.

## Existing implementation (do not duplicate)

The live damage contracts are currently in [ENTITIES/Shared/Combat](../ENTITIES/Shared/Combat/README.md): `DamagePacket`, `IDamageReceiver`, `DamageResolver`, `HealthSettings` and `HealthComponent`. Their types are generic even though they live under ENTITIES. A firearm sends a packet from [FirearmController](../EQUIPMENT/Weapons/Shared/FirearmController.cs), and the zombie receives it, applies mitigation and dies when health reaches zero. **Nonlethal hits currently do not interrupt zombie movement.** The shared health component publishes `Damaged`/`Died` events but no generalized reaction or zero-damage impact dispatch.

The live player shooting adapter is [PLAYER/Equipment/PlayerWeaponController.cs](../PLAYER/Equipment/PlayerWeaponController.cs). [FirstPersonWeapon](../DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs) uses the same firearm controller in the FPS test. Existing `WeaponSlots` are two test-only **firearm** slots, not an inventory or generic item hotbar. [PLAYER/IMPLEMENTATION_PLAN.md](../PLAYER/IMPLEMENTATION_PLAN.md) defines **PH0 player health**, which must precede zombie attacks that damage the player.

## Intended ownership

- **`COMBAT/`** owns cross-actor melee hit detection/dispatch, impact metadata, stagger/knockback policies and common attack/reaction mechanics **when those phases are authorized**. Generic damage should eventually live here as well, but moving existing `ENTITIES/Shared/Combat` files is an optional, separately validated cleanup—not a prerequisite for melee.
- **`ENTITIES/Shared/Combat/`** remains the *single live damage implementation* until any explicit migration. Do not create duplicate `DamageResolver`, `HealthComponent`, `DamagePacket` or `IDamageReceiver` classes in COMBAT.
- **`EQUIPMENT/`** owns gun, bat, knife and other equipment scenes, their per-weapon `.tres` profiles and presentation sockets/animation bindings. A firearm can also initiate a low-damage gun-butt shove.
- **`PLAYER/`** owns attack intent/input, posture, stamina (future PH2), movement/animation coordination and later health, not generic hit/damage calculations.
- **`ENTITIES/TYPES/Zombies/`** owns zombie-specific movement interruption, recoil pose, resistance, perception and any future outgoing attack scheduling; generic reaction primitives are reusable by other creatures or players.
- **`WORLD/`** composes actors and scene references only. FPS remains an optional comparison view; shared combat must not depend on an overhead camera.
- **`COMBAT/Configuration/`** and its master `.tres` should be added **with the first implemented mechanic that genuinely needs global tuning**. Use typed `Resource` profiles, runtime copies and per-weapon/per-target defaults rather than speculative master fields. `C/` folders remain for *procedural artwork only*, never combat controllers.

## Principle

`Input / enemy decision → attack window → bounded hit detection → existing damage receiver (when damaging) + optional impact reaction → target-owned movement/pose response → stop and sleep`.

An **impact can do zero HP damage** (gun shove), and an attack can do HP damage without a stagger (immunity, resisted impact). Killing takes priority over staggering. No global combat singleton, idle target scans, per-zombie hit polling or second damage resolver.

Start with [the detailed plan](IMPLEMENTATION_PLAN.md). It contains all phase ownership, dependencies, suggested values, performance rules and handoff checks. **No code/config/scene file was changed for this documentation-only planning job.**
