# PLAYER/Stats — planned player-owned health and vitals

**PH0 Planned; no stats scripts, components or runtime player HP implemented in this folder.** This directory reserves the home for `PlayerVitals.cs` and `PlayerVitalsSettings.cs` proposed by the existing [PLAYER implementation plan](../IMPLEMENTATION_PLAN.md).

Reuse `ENTITIES/Shared/Combat/HealthComponent.cs` and `HealthSettings.cs` for actual HP, armour/resistance damage calculations and death; do not create another resolver. `PlayerController` will implement `IDamageReceiver` and forward to a small player-owned stats adapter during PH0. Stats own values/events; `FEEDBACK/` is presentation-only and `UI/GameplayHUD/` owns screen-space layout.

Add code, scene nodes and player master health subresources **only when PH0 is explicitly authorized**. Stop all stats work when idle; later stamina/survival phases have independent gates.
