# Gameplay HUD

> **Authoritative planned player-status revision (2026-10-28):** PH0 will add hidden Vitality plus individual conditions for Head/Torso/LeftArm/RightArm/LeftLeg/RightLeg. PH1 adds a **compact six-section body silhouette** near the hotbar, not a player 100 HP bar. The current generic `HealthBarOverlay` is already mounted once by WorldController and supports injured NPCs; it currently contains a prospective player numeric bar path which **PH0 must exclude**. No actual player vitals or body HUD have yet been implemented. See [player plan](../../PLAYER/IMPLEMENTATION_PLAN.md) and [body design](../../PLAYER/Stats/BODY_CONDITION_DESIGN.md).

The existing nine-slot [Hotbar](Hotbar/README.md) and [Backpack Inventory](Inventory/README.md) share this screen-space HUD. PH1's new visual-only `FEEDBACK/Bars/PlayerStatus` widget should be a sibling of the hotbar/backpack in the single `GameplayHUD/Screen`, shared by overhead/FPS, with `MouseFilter.Ignore` and region event subscriptions only. No duplicate overlays, new player-health arithmetic, interference with shooting or constant HUD polling. Display only severity information that actually exists; detailed wounds/treatments inside a future Inventory Health tab are not authorized now.

This document change creates no scenes, scripts or layout adjustments.
