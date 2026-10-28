# Enemy health bars — FB2 Planned

> **Implemented in source, awaiting local validation:** `HealthBarOverlay.cs` registers all `HealthComponent` nodes, existing or spawned later. It displays the same pooled bar above all injured health-bearing actors (not hard-coded zombies), hides it when fully healed/dead/unloaded/offscreen, and stays inactive when no actor is injured. It can show bars through obstacles in this initial version; no wall ray is performed. The earlier proposed 2.5-second timeout below is superseded by **visible while HP is below max**.

Future temporary overhead health bars for zombies/runners and any later health-bearing actor. Show on genuine damage or an explicitly approved visibility event; hide after a short interval, on death, off-screen, occlusion, unload or deactivation.

Design one bounded, shared presenter/pooled set of widgets that projects actor head/marker positions into the current camera. Default to hidden while undamaged. Avoid permanent per-zombie nodes or scene-tree searches in hot paths. Use current/max health snapshots after `Damaged`, not `DamageResult.Amount` alone (which can include overkill).

Primary visual tuning is top-down; FPS inherits the world and must not receive a duplicate HUD manager. FB2 is not implemented yet. [Plan](../../IMPLEMENTATION_PLAN.md).
