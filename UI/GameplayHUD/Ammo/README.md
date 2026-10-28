# Ammo HUD (AM1)

AM1 owns only screen-space **presentation** of AM0's real firearm magazine, reserve ammunition and reload state. `AmmoHud.tscn` mounts `AmmoHud.cs` once in shared `GameplayHUD/Screen` through `WORLD/WorldController.InitializeAmmoHud`. The inherited FPS scene uses the same widget; the FPS world only resolves its different `FirstPersonEquipment/WeaponSlots` path.

## Layout

A compact **160 × 66px** charcoal card with "AMMO", large `30 / ∞` style number, and `RELOADING`, `R RELOAD` or `EMPTY` status. Anchored with the hotbar's 18px bottom margin, immediately **left of the bottom-centred 472px hotbar** and **to the right of the player body-condition silhouette** on normal desktop widths. On narrower viewports (little room to the left of the hotbar), it shifts above the bar and to the right of the body widget. The only sizing update is on viewport resize. All nodes ignore mouse input.

## Data and performance

`AmmoHud.Initialize(WeaponSlots)` reads the current firearm once, then subscribes to `WeaponSlots.SelectionChanged`. For the selected gun alone, it observes AM0 `AmmoChanged`, `ReloadStarted`, `ReloadCompleted`, `ReloadCancelled`; on slot change it unsubscribes the prior gun. No ammo state is stored in the UI, no UI input routing, no frame/physics loops or new timers. It disconnects all subscriptions on exit. **The gun itself remains the authoritative ammunition source.** AM2 may replace reserve data later without requiring a new widget.

## Local checks

`dotnet build war-machine.csproj`; F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. Check 1920x1080 and a narrower 1280x720 viewport, health/hotbar gaps, 1/2 selection, shot-by-shot ammo decrement, empty/auto reload, manual R, UI open/focus and no mouse interception. Source integration complete; Godot visual/build validation pending. Full [AM0–AM2 plan](../../../EQUIPMENT/Weapons/AMMO_IMPLEMENTATION_PLAN.md).
