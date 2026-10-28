> **Anatomy/combat/corpse direction (2026-10-28):** [Overall master plan](../../ENTITIES/TYPES/Zombies/ANATOMY_COMBAT_CORPSE_PLAN.md). Reuse the existing BodyRegion enum and compatible condition/damage foundations; preserve player critical-versus-limb vitality rules, healing and HUD. Future PH-HIT consumes shared region detection when authorized; ELD does not automatically implement it.

# BodyCondition and Vitality — design specification for PH0/PH1

**PH0/PH1 implemented in source, awaiting local validation (2026-10-28).** Authoritative implementation gates are in [PLAYER implementation plan](../IMPLEMENTATION_PLAN.md). All work occurs on `implement-td0-td1`.

## Player anatomy at launch

Maintain **one** player state component with exactly six named regions. A fixed six-entry array (or equivalent small flat structure) is cheaper and easier to serialize than six independent HealthComponents or ticking scenes.

| BodyRegion | Initial condition | Meaning in the data model |
| --- | ---: | --- |
| Head | 100 | Head condition; critical/life-threatening hit path |
| Torso | 100 | Chest/abdomen combined; critical/life-threatening hit path |
| LeftArm | 100 | Support-hand/handling condition, future aim/handling modifiers |
| RightArm | 100 | Dominant-hand/handling condition, future aim/handling modifiers |
| LeftLeg | 100 | Mobility condition, future limp/sprint restrictions |
| RightLeg | 100 | Mobility condition, future limp/sprint restrictions |

Use `BodyRegion.Unknown` as the optional unspecified-hit sentinel; resolve it consistently as **Torso** on receiving player damage. Left/right refer to **the player's own anatomy**, not screen-left, so the HUD must display a front-facing or clearly labelled mirrored image correctly.

This is **condition**, not six independently mortal HP pools. The initial 0–100 scaling is an internal developer-friendly measure; visible gameplay should show severity rather than numbers. Never automatically kill on a limb reaching 0. The first pass does not add status effects, anatomy-based movement changes or limb animations.

## Separate hidden Vitality

`Vitals/Health : HealthComponent` stays authoritative for internal overall player vitality and one-time death. Start with `MaxHealth=100`. **Do not draw it as the primary player HP bar.** NPCs still use their existing conventional health and FEEDBACK bars.

- A **head/torso** hit reduces the corresponding region condition and the hidden vitality by the appropriate **single mitigated damage amount**. Initial multipliers can be 1.0 for critical regions; author profile values if actual policy needs tuning, not two damage formulas.
- A **limb** hit reduces that region's condition but does **not directly** reduce vitality in PH0. Later injuries can cause vitality harm through explicit, approved bleeding/shock/critical conditions.
- A non-anatomical hazard (starvation, severe temperature, etc.) would later be able to damage vitality explicitly without claiming a bullet hit a body region. These are PH3/PH5 concerns, not implemented now.
- **Death** occurs at vitality 0 and latches once; zero head/torso condition does not introduce a competing independent death rule in the first pass. Lethal hit presentation and policies can expand later.

**Mitigation happens once.** Desired path:

~~~text
HitTargetResolver -> PlayerController.ReceiveHit
  -> PlayerVitals.ResolveIncomingHit(packet, region or Unknown)
    -> DamageResolver.Resolve(packet, shared profile) [once]
    -> BodyCondition.ApplyResolvedDamage(region, resolvedAmount)
    -> if region is Head or Torso:
         HealthComponent.ApplyResolvedDamage(resolvedResult) [one vitality loss]
    -> BodyCondition/HealthChanged/Death events -> future HUD
~~~

To support this, a narrow `HealthComponent.ApplyResolvedDamage` path is acceptable in PH0 if it preserves all old `ApplyDamage`, `Damaged`, `HealthChanged`, `Died` behaviour. The generic `ApplyDamage` may delegate to an internal common method. **Do not pass already mitigated damage to `ApplyDamage`, since that would resolve/mitigate twice.** Match event order for lethal hits so the body snapshot and vitality/death snapshot are consistent, and guard against reentrant death callbacks.

Optional region metadata on `DamagePacket` must be backward-compatible with all existing packet constructors and zombie/deer/robot consumers. If simpler, use a thin player-specific region-aware routing method rather than rewriting the shared packet. The current player movement capsule supplies **Unknown -> Torso**; PH0 should provide controlled test injections for each limb, not mislabel capsule rays as anatomically precise.

## Healing and state change API

- `PlayerVitals.HealVitality(float)` operates only on the hidden life reserve.
- `BodyCondition.HealRegion(BodyRegion, float)` operates only on the specified limb/region.
- Both clamp to each max and ignore NaN/infinite/nonpositive inputs, dead/unconfigured targets, and unchanged amounts. No resurrection from normal healing. No automatic healing of all regions.
- `BodyCondition.ConditionChanged(region, current, maximum)` or equivalent strongly typed snapshot event fires only when state truly changes. Initial PH1 widget binding reads **all six** values immediately; no need for initial fake damage events.
- Reuse existing `HealthComponent.HealthChanged(HealthComponent)`. No new duplicate HP changed-event API or UI-layer arithmetic.
- All authored starting values live in the player master `.tres`, deep duplicated at runtime. Current condition belongs to runtime/possible later SAVE, never in shared resources.

## Planned PH1 condition visualization

A **compact six-section human silhouette** in the lower-left of the shared GameplayHUD. Not six literal HP bars, not a conventional 100/100 meter. UI is read-only; gameplay does not depend on colours.

| Condition fraction (suggested) | Appearance | Interpretation |
| --- | --- | --- |
| 75–100% | muted neutral/green | healthy or minor wear |
| 40–74% | amber + small indicator | injured |
| 1–39% | red + high-priority indicator | badly injured |
| 0% | deep red/disabled + icon | incapacitated region |

These are **presentation thresholds only**, tunable in `FeedbackMaster.tres` when PH1 starts. No 'broken bone' icon before an actual fracture condition exists. Include text/icon/pattern signals for colour-blind clarity. Fully healthy UI should be unobtrusive; affected regions become prominent and may briefly pulse. MouseFilter.Ignore; update only on events; one widget shared between overhead/FPS; don't overlay the hotbar.

Later **Inventory > Health** can display exact values, wounds, treatments, bandages and fractures; that tab is not PH0/PH1.

## Future precise hitboxes — PH-HIT (separate authorization)

The existing movement capsule protects navigation/physics only. It **cannot** distinguish head vs limb shots. Plan six small nonblocking hurtbox shapes or region tags attached to the player art/pose, on a dedicated appropriate hit-detection layer, without confusing CharacterBody3D movement collision, AI sight, cover or zombie melee:

- Head, Torso, LeftArm, RightArm, LeftLeg, RightLeg.
- Trace/ray routing must avoid the main body capsule masking anatomical hits and prevent both capsule+hurtbox applying the *same hit* twice.
- Preserve weapon muzzle rays, impact/cover priority, crouch/stance and both camera modes; use the collider→actor parent walk already present in `HitTargetResolver`.
- Start with representative coarse hitboxes, then optionally animate/follow the pose. No six separate health components or rigidbodies.
- If hitbox path fails or is absent, default to Torso rather than invent a body region.

A precise PH-HIT implementation **is not included in PH0 or PH1** unless separately authorized.

## Realism later — explicit consequences, not yet implemented

- Left/right leg injury -> limp, reduced speed, potential sprint lock; **PH2 stamina integration** could consume a read-only movement modifier later.
- Left/right arm injury -> weapon sway, handling/reload, melee force.
- Head injury -> concussion, vision and consciousness changes.
- Torso injury -> bleeding, stamina/breathing effects and vital risk.
- Treat bleeding, fractures, infection, shock, medicine, bandages and harvesting individually as content warrants (PH5 follow-ups). Event-driven changes with at most one active low-rate survival tick for actual ongoing effects, never idle per-limb polling.

## Future Codex acceptance

PH0: six regions independently injectable, critical vs limb damage distinction, single mitigation, correct vitality/death events and NPC regressions, no player HP overlay. PH1: per-region silhouette/colours update and heal separately, works in top-down + FPS without duplicate HUD or hotbar interference, no idle process. PH-HIT later: confirmed true per-limb weapon hit detection and no double damage.

**Implementation now follows this specification.** See the PLAYER plan handoff and DEBUG/TEST/PlayerVitals checks. PH-HIT and later injury consequences remain future work.
