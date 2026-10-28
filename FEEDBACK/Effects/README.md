# FEEDBACK/Effects — presentation-only impact cues

**Planned FB4 directory scaffold.** No visual effect has been authored or wired here.

- [DamageNumbers/](DamageNumbers/README.md) — optional temporary floating damage values.
- [HitIndicators/](HitIndicators/README.md) — player damage/attacker-direction indicators.
- [StatusEffects/](StatusEffects/README.md) — icons/cues for implemented gameplay conditions.

Effects react to **confirmed** hit, health or status events; they do not calculate/apply damage. Cap active instances, use existing actor/camera context, never per-frame scan the world, and stop work completely after completion/despawn. [FB plan](../IMPLEMENTATION_PLAN.md).
