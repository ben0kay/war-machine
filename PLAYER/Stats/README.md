# PLAYER/Stats — planned vitality and anatomy foundation

**PH0 and PH1 are PLANNED. No new stats or visual scripts exist yet.** [Player plan](../IMPLEMENTATION_PLAN.md) and [BodyCondition design](BODY_CONDITION_DESIGN.md) are the authoritative Codex contracts.

- **PH0:** one `PlayerVitals` coordinator wraps the existing generic `HealthComponent` for hidden 100-point Vitality/death and one `BodyCondition` for **six independent regions**: Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg. A body hit is mitigated once by the existing `DamageResolver`. Head/torso damage can harm hidden vitality; arms/legs lose condition without directly killing. Unknown capsule hit -> Torso. No per-region health nodes or loops.
- **PH0 source:** new `PlayerVitals.cs`, `PlayerVitalsSettings.cs`, `BodyCondition.cs` and `BodyRegion.cs`, with an exported profile in `PLAYER/Configuration/PlayerMaster.tres` and `Vitals/Health/BodyCondition` scene wiring. Use the existing `HealthComponent.HealthChanged`, never duplicate it.
- **PH1:** read-only six-part silhouette/condition widget belongs to `FEEDBACK/Bars/PlayerStatus/` and is composed once via `UI/GameplayHUD`. No numeric player HP bar; enemy generic health bars stay.
- **PH-HIT:** actual precise anatomical weapon hitboxes are separate future work. PH0 injects test region data through an explicit compatible route; current player capsule falls back to Torso. No pretending that the movement capsule detects limbs.
- **PH5 later:** limb movement/aim penalties, bleeding, fractures, pain, healing items/medicine and environmental hazards; not implied by PH0.

Keep authored initial conditions in typed `.tres` defaults, never store live HP in a shared resource. No stats polling during idle; event-driven damage, healing and subscriptions, clean exit/unbind. PH2 stamina, PH3 needs and PH4 fatigue remain separately authorized.
