# Hotbar — nine-slot visual foundation

The reusable `Hotbar.tscn` is a **code-free CanvasLayer** instanced directly in `WORLD/World.tscn` as `GameplayHUD`. The inherited FPS debug test also displays it. The separate parked F5 first-person wilderness scene is unchanged; use **F6** with `WORLD/World.tscn` or `WORLD/FirstPerson/FirstPersonWorld.tscn`.

### Appearance and layout

Nine empty 44px slots are named `Slot1` to `Slot9` under `Screen/BarFrame/Slots`, each with a numbered Label. A lightly translucent charcoal panel and slim borders keep the bar readable without dominating the view. **Slot1's cyan border is only a visual placeholder**, not the active equipment selection. `BarFrame` anchors to the bottom-center of the viewport, 18px above the edge; HBox separation is 6px. The visual StyleBoxFlat resources, slot sizes and offsets are authored in `Hotbar.tscn`, so no extra settings/code is needed for the initial visual pass.

Every Control has `mouse_filter = Ignore`; no slots capture input, preventing interference with camera mouse aiming and firing. No `_Process`, `_PhysicsProcess`, timers, inventory callbacks, or draw loops are present. The bar is static and effectively dormant while idle.

### Future item use — not part of this pass

An inventory/quick-access model should eventually own item references and selected index. A small presentation adapter can update icons, stacks and highlight only when that model changes. Do not wire 1–9 keys, wheel actions, drag/drop, existing rifle/machine gun, save data, or equipping in this visual-only pass.

### Local validation

Only syntax/path/layout inspection was completed here; **Godot runtime display has not been verified**. F6 `WORLD/World.tscn` to confirm nine slots at the bottom center at 1920×1080 and a smaller window. Confirm UI does not block aiming or clicking and doesn't overlap debug readouts. F6 the FPS test to confirm the inherited bar, mouse capture and F9 fog controls. No terrain rebake. No code compilation needed for this static UI-only addition.

## AM1 sibling ammo display (2026-10-28)

One `UI/GameplayHUD/Ammo/AmmoHud.tscn` is now mounted by WorldController alongside the existing static Hotbar scene under `GameplayHUD/Screen`. It sits immediately to the left of the bottom-centred nine-slot bar on desktop widths, right of the player-body HUD, and moves higher at narrower widths. No `Hotbar.tscn` slot size, 1–9 numbering, panel appearance or input behavior changes. Updates occur on selected-weapon/ammo events; no per-frame polling. Local Godot visual tests pending.
