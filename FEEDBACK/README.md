# FEEDBACK — visual combat and status feedback

**Status: FB0 folder/documentation foundation implemented; FB1–FB4 planned only.** No health bars, defence bars, damage numbers, indicators, effects or feedback C# are installed by FB0. Do not mistake a directory for a working feature.

This top-level folder owns **reusable presentation** of gameplay state in the active top-down world and (where appropriate) the inherited FPS debug scene. It **does not** own current HP, damage maths, health rules, player survival state or a new combat pipeline.

## Ownership and entry points

- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) — independent FB0–FB4 authorization gates for Codex/regular chats.
- [Bars/](Bars/README.md) — shared bar widgets, player HUD presentation and temporary enemy world-space/overlay bars.
- [Effects/](Effects/README.md) — optional damage numbers, hit indicators and status feedback (later).
- [Configuration/](Configuration/README.md) — reserved home for an Inspector-editable `FeedbackMaster.tres` **when working UI code defines the resource class**. No dummy .tres now.
- [PLAYER/IMPLEMENTATION_PLAN.md](../PLAYER/IMPLEMENTATION_PLAN.md) — existing PH0–PH5 plan for player HP, healing, death and survival; **PH0 is separate from FB work**.
- [UI/GameplayHUD/](../UI/GameplayHUD/README.md) — composition/layout for screen-space player UI (currently a visual hotbar), not authoritative player state.
- [ENTITIES/Shared/Combat/](../ENTITIES/Shared/Combat/README.md) — existing `HealthComponent`, `HealthSettings`, `DamageResolver`, `IDamageReceiver`; these remain the only damage/health foundation.

## Rules for future implementations

1. All bar data is read-only presentation: initialize from current/max values on bind, subscribe to HP/death/defence changes, and unsubscribe on despawn, rebind or scene exit. **Never calculate damage, mutate HP or duplicate resistances** inside feedback widgets.
2. Player bars are screen-space components arranged by GameplayHUD; enemy bars are projected from an actor-owned marker or a dedicated relevant-enemy overlay, not permanent CanvasLayers or idle processing on every zombie.
3. Default enemy bars are **hidden until damage or another explicit relevance event**, fade/hide after a short configurable duration, and sleep when hidden, off-screen, dead or unloaded. Bound the number of active bars and avoid per-enemy full-world queries.
4. Health is a depletable number. Existing `HealthSettings.Armour` and damage resistances are **mitigation values, not consumable shield points**: do not draw them as a shrinking defence bar. `Bars/Defence` is reserved for a **future real shield/guard resource** if/when one is implemented. Static armour may eventually be shown by a small icon or tooltip.
5. Reuse one configurable visual style across top-down and FPS where practical; the debug FPS scene inherits World.tscn, so avoid double-instancing overlays. Use `Control.MouseFilterEnum.Ignore` for noninteractive gameplay feedback to preserve shooting/aiming and UI focus behavior.
6. Use event-driven changes and temporary active-only animation/tween callbacks; no always-running feedback polling, singleton gameplay manager or duplicate hit detector.
7. Create scenes/scripts/configuration **only when their named FB phase is authorized**; FB0 adds directory documentation, not functionality.

Work on `implement-td0-td1`, consult root README + CodePreferences + real current source, and make one commit per explicitly authorized phase. Local build/visual acceptance will be listed in the relevant implemented phase.
