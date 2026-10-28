# Reusable health bar — FB1 Planned

Future `HealthBar.tscn` + optional `HealthBar.cs` will display **current / maximum HP** provided by an existing `HealthComponent` or a narrow read-only actor adapter. Do not create a second health model here.

Initialize the bar on binding, listen to explicit health-change/death notifications, and unsubscribe when the owner changes or unloads. Use a shared configurable style, finite/clamped percentage, optional hit pulse only while animating, and `mouse_filter = Ignore` for in-game feedback. Reuse in player HUD and enemy overlays instead of separate health-bar implementations.

**No files other than this README yet.** Authorize FB1 before implementation. [Plan](../../IMPLEMENTATION_PLAN.md).
