# NPC health overlay — existing source implementation, awaiting local validation

`HealthBarOverlay.cs` already registers shared `HealthComponent` actors and displays a pooled red bar for injured living actors only. At full health no bar is shown. It updates from `HealthChanged` events and projects wounded actors at a capped rate, with no per-entity permanent UI callbacks. Known limitation: no world-geometry LOS occlusion; an injured NPC behind walls may still expose its bar.

**PH0 future player integration change:** The current overlay contains a branch that would show the player's generic health as a bottom-left numeric bar. The new player design **must exclude PlayerController-owned HealthComponent from this overlay**. Do not delete or regress zombie/deer bars. Player condition is PH1's six-region silhouette under `FEEDBACK/Bars/PlayerStatus`. Do not show a standard numeric player HP bar.

This folder remains a shared NPC presenter, not a damage owner, and the FPS test inherits the same shared HUD. PH0/PH1 are planned only. See [FB plan](../../IMPLEMENTATION_PLAN.md) and [player design](../../../PLAYER/Stats/BODY_CONDITION_DESIGN.md).
