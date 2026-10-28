# Enemy health bars — FB2 Planned

Future temporary overhead health bars for zombies/runners and any later health-bearing actor. Show on genuine damage or an explicitly approved visibility event; hide after a short interval, on death, off-screen, occlusion, unload or deactivation.

Design one bounded, shared presenter/pooled set of widgets that projects actor head/marker positions into the current camera. Default to hidden while undamaged. Avoid permanent per-zombie nodes or scene-tree searches in hot paths. Use current/max health snapshots after `Damaged`, not `DamageResult.Amount` alone (which can include overkill).

Primary visual tuning is top-down; FPS inherits the world and must not receive a duplicate HUD manager. FB2 is not implemented yet. [Plan](../../IMPLEMENTATION_PLAN.md).
