> **CM5 source implemented / awaiting local validation:** Zombie standing strikes now use the shared melee executor and existing player-health route. Damage/timing and checks: [CM5 handoff](../../COMBAT/CM5_README.md). ELD3 attack capability rules remain deferred.

# Player vitality and anatomy

**PH0 implemented; awaiting local compilation/runtime validation.** `PlayerVitals` coordinates one hidden `HealthComponent` and one `BodyCondition` holding six fixed entries. `PlayerVitalsSettings` supplies deep-copied defaults in PlayerMaster: vitality 100, each region 100, zero armour/resistances. [Design](BODY_CONDITION_DESIGN.md) and [phase plan](../IMPLEMENTATION_PLAN.md).

`PlayerController.ReceiveHit(packet)` is the shared IDamageReceiver route; capsule hits fall back to Torso. `player.Vitals.ReceiveHit(packet, BodyRegion.LeftLeg)` is the explicit development/future hurtbox route. No weapon packet or NPC contract was changed. Shared DamageResolver runs once; resolved region damage is committed before critical HealthComponent events. Only Head/Torso reduce vitality. Zero limb condition does not kill; vitality death latches once and blocks controls without removing the player or pausing the world.

`Vitals.HealVitality(amount)` changes only hidden vitality. `Vitals.HealRegion(region, amount)` or `Vitals.Body.HealRegion` changes only that valid named region; invalid healing regions are rejected. Invalid/nonpositive/nonfinite inputs and dead/unconfigured targets are ignored; heals clamp and emit only real changes. Region callbacks receive value snapshots; existing HealthChanged supplies vitality. Mutations during damage/change notification are rejected to avoid inconsistent reentrant snapshots. The coordinator changes anatomy then critical vitality before publishing region changes, so subscribers read final consistent state.

No vitality regeneration, limb penalties, bleeding, medicine, respawn, save or accurate anatomical hitboxes. PH1 displays the read-only six-region silhouette. No stats timers or frame callbacks. Events are connected once and disconnected at exit. [Standalone checks and local handoff](../../DEBUG/TEST/PlayerVitals/README.md).


## PH2 foundation — source implemented, awaiting validation

PlayerVitals creates one PlayerStamina child, with deep-copied/sanitized PlayerStaminaSettings embedded in PlayerMaster. Defaults: 100 reserve, sprint 10/s, jump 15, recovery 15/s after 1.5s, sprint unlock at 20%. Per-weapon StaminaCostPerShot defaults to 0.5 and is authored for both current guns; existing view adapters charge actual ShotAccepted only. Zero reserve never blocks firing; no handling penalty yet.

Walking/stances are free. Movement reports actual grounded sprint progress and uses CanSprint; eligible jumps require TryJump. RecordSprint drains via existing movement physics; only depleted non-sprinting reserve wakes the stamina node's recovery physics callback. Full/disabled/dead reserve has no callback. Recovery continues while movement sleeps, uses gameplay delta rather than sky/wall time, and stops at tree pause. Death stops spending and recovery. No hunger, satiation, thirst, fatigue, healing or melee cost is implemented.

UI/GameplayHUD/Stamina is a read-only event-driven bottom-right card. Full reserve remains visible for now; disabled stamina hides it. See [PH2 contract and local checks](../IMPLEMENTATION_PLAN.md). Existing PlayerVitals smoke includes deterministic actual-node stamina assertions; compilation and running the smoke remain local because dotnet/Godot are unavailable here.
