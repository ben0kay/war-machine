> **CM5 source implemented / awaiting local validation:** Zombie standing strikes now use the shared melee executor and existing player-health route. Damage/timing and checks: [CM5 handoff](../../COMBAT/CM5_README.md). ELD3 attack capability rules remain deferred.

# Player vitality and anatomy

**PH0 implemented; awaiting local compilation/runtime validation.** `PlayerVitals` coordinates one hidden `HealthComponent` and one `BodyCondition` holding six fixed entries. `PlayerVitalsSettings` supplies deep-copied defaults in PlayerMaster: vitality 100, each region 100, zero armour/resistances. [Design](BODY_CONDITION_DESIGN.md) and [phase plan](../IMPLEMENTATION_PLAN.md).

`PlayerController.ReceiveHit(packet)` is the shared IDamageReceiver route; capsule hits fall back to Torso. `player.Vitals.ReceiveHit(packet, BodyRegion.LeftLeg)` is the explicit development/future hurtbox route. No weapon packet or NPC contract was changed. Shared DamageResolver runs once; resolved region damage is committed before critical HealthComponent events. Only Head/Torso reduce vitality. Zero limb condition does not kill; vitality death latches once and blocks controls without removing the player or pausing the world.

`Vitals.HealVitality(amount)` changes only hidden vitality. `Vitals.HealRegion(region, amount)` or `Vitals.Body.HealRegion` changes only that valid named region; invalid healing regions are rejected. Invalid/nonpositive/nonfinite inputs and dead/unconfigured targets are ignored; heals clamp and emit only real changes. Region callbacks receive value snapshots; existing HealthChanged supplies vitality. Mutations during damage/change notification are rejected to avoid inconsistent reentrant snapshots. The coordinator changes anatomy then critical vitality before publishing region changes, so subscribers read final consistent state.

No stamina, regeneration, limb penalties, bleeding, medicine, respawn, save or accurate anatomical hitboxes. PH1 displays the read-only six-region silhouette. No stats timers or frame callbacks. Events are connected once and disconnected at exit. [Standalone checks and local handoff](../../DEBUG/TEST/PlayerVitals/README.md).
