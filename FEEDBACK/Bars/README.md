# FEEDBACK/Bars — visual status presentation

**Implemented in source:** `Health/HealthBar.tscn` and `EnemyBars/HealthBarOverlay.cs` provide pooled numeric health bars for injured shared-health actors (Godot acceptance pending). The overlay currently contains a special bottom-left numeric player bar path, which future **PH0 must exclude** while preserving NPC bars.

**Planned PH1 / FB1:** `PlayerStatus/` will own a **six-region anatomical silhouette** for Head, Torso, LeftArm, RightArm, LeftLeg and RightLeg condition. Player vitality remains hidden, not a numeric HUD bar. The widget reads events only; it must not calculate damage, interfere with the hotbar or capture mouse input.

- [Health](Health/README.md) — numeric bar reused for NPCs
- [EnemyBars](EnemyBars/README.md) — existing pooled overlay, player exclusion required in PH0
- [PlayerStatus](PlayerStatus/README.md) — planned body-condition silhouette
- [Defence](Defence/README.md) — future consumable shield/guard, not static armour

[FEEDBACK plan](../IMPLEMENTATION_PLAN.md) · [BodyCondition design](../../PLAYER/Stats/BODY_CONDITION_DESIGN.md).
