> **IX0:** One read-only interaction prompt below screen centre now appears while holding E. LMB invokes the reachable target action; GUI remains first priority. See [interaction HUD](Interaction/README.md). Source implemented; local input/layout checks pending.

> **PH2 temporary stamina card (2026-10-29):** One small [bottom-right stamina meter](Stamina/README.md) is now composed beside the existing body/ammo/inventory HUD, shared by both views. Event-driven, mouse-through, always visible while enabled; narrow windows lift it above existing controls. Source implemented; layout/build acceptance pending.

> **Current implementation (2026-10-28):** PH0/PH1 now implemented in source: one read-only BodyConditionWidget beside the NPC overlay and backpack, above the hotbar. Hidden vitality has no generic player HP bar. World/FPS share this composition. Local build/visual acceptance pending; older planned status below is superseded.

# Gameplay HUD

> **Authoritative planned player-status revision (2026-10-28):** PH0 will add hidden Vitality plus individual conditions for Head/Torso/LeftArm/RightArm/LeftLeg/RightLeg. PH1 adds a **compact six-section body silhouette** near the hotbar, not a player 100 HP bar. The current generic `HealthBarOverlay` is already mounted once by WorldController and supports injured NPCs; it currently contains a prospective player numeric bar path which **PH0 must exclude**. No actual player vitals or body HUD have yet been implemented. See [player plan](../../PLAYER/IMPLEMENTATION_PLAN.md) and [body design](../../PLAYER/Stats/BODY_CONDITION_DESIGN.md).

The existing nine-slot [Hotbar](Hotbar/README.md) and [Backpack Inventory](Inventory/README.md) share this screen-space HUD. PH1's new visual-only `FEEDBACK/Bars/PlayerStatus` widget should be a sibling of the hotbar/backpack in the single `GameplayHUD/Screen`, shared by overhead/FPS, with `MouseFilter.Ignore` and region event subscriptions only. No duplicate overlays, new player-health arithmetic, interference with shooting or constant HUD polling. Display only severity information that actually exists; detailed wounds/treatments inside a future Inventory Health tab are not authorized now.

This document change creates no scenes, scripts or layout adjustments.

## AM1 ammunition widget — implemented in source 2026-10-28

`Ammo/AmmoHud.tscn` + `AmmoHud.cs` introduce a **compact 160×66px magazine/reserve display** in the shared `GameplayHUD/Screen`, to the right of the player-body condition HUD and immediately left of the centred hotbar at normal desktop widths. The readout shows `30 / ∞`, `100 / ∞` etc., switching to amber `RELOADING` during AM0 reload and `R RELOAD` when a partial magazine can be reloaded. On narrow windows it relocates above the hotbar and to the right of the body HUD to avoid visible overlap. Like other gameplay feedback it has `MouseFilter.Ignore`.

`WorldController.InitializeAmmoHud` adds exactly one panel per active world; inherited FPS shares it, resolving the different equipment-slots path through a small overridden method. `WeaponSlots.SelectionChanged` and the selected `FirearmController` AM0 ammo/reload events update the UI **only when state changes**, plus one viewport-resize callback for positioning. All subscriptions are released on exit. The ammo widget never alters AM0, recoil, inventory or shooting. No HUD progress bar, sound or reload animation introduced. [AM0–AM2 implementation plan](../../EQUIPMENT/Weapons/AMMO_IMPLEMENTATION_PLAN.md). **Source implemented; Godot/.NET build and layout QA pending.**
