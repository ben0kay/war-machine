# Player body-condition presentation — PH1 / FB1 (Planned)

**Player status is not a numeric HP bar.** PH1 adds a compact **six-region human silhouette** to `FEEDBACK/Bars/PlayerStatus/`, composed under the existing `UI/GameplayHUD/Screen` sibling of the hotbar/backpack. The player still has hidden survival Vitality internally via PH0, but its normal on-screen state is Head, Torso, LeftArm, RightArm, LeftLeg and RightLeg condition. Each can independently show muted healthy, amber injured, red severe or dark incapacitated state; use icons/labels or patterns as well as colour. Only display injury icons for injuries the gameplay actually implements.

**Data:** Subscribe once to `PLAYER/Stats/BodyCondition.ConditionChanged` and vitality/death events when PH0 has actually created them. Read initial six-value snapshot, update only changed segments, optionally pulse briefly on hit, and unsubscribe on rebind/exit. HUD is read-only and uses `MouseFilter.Ignore`; no per-frame polling/duplicate player HP calculations.

**Coexistence:** The existing generic `FEEDBACK/Bars/Health/HealthBar.tscn` and `Bars/EnemyBars/HealthBarOverlay.cs` serve **NPCs**. That overlay currently has a player bottom-left special case, so PH0 is required to **exclude player HealthComponent** from it. PH1 must not turn that numeric bar back on or instantiate a second overlay. Both the overhead and inherited FPS camera use one silhouette. Leave the inventory Health tab, medical treatment, region-specific raycast hitboxes and limb gameplay effects for separately approved work.

See [PH0–PH1 plan](../../../PLAYER/IMPLEMENTATION_PLAN.md) and [body condition spec](../../../PLAYER/Stats/BODY_CONDITION_DESIGN.md). **Status: design only; no widget exists yet.**
