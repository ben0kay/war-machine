# FEEDBACK/Bars — reusable visual meters

**Folder foundation only; no bar scenes/scripts currently implemented.** The bar system presents actor-owned values and never owns damage, health, healing or shield simulation.

- [Health/](Health/README.md) — generic current/max health visual (FB1).
- [PlayerStatus/](PlayerStatus/README.md) — screen-space health/status widgets assembled under existing `UI/GameplayHUD` (FB1).
- [EnemyBars/](EnemyBars/README.md) — temporary world-positioned bars for relevant damaged actors (FB2).
- [Defence/](Defence/README.md) — reserved depletable shield/guard meter (FB3), **not** static armour or resistances.

The primary top-down view drives presentation decisions; inherited FPS may reuse the same widget without duplicating HUD state. Read [FB plan](../IMPLEMENTATION_PLAN.md) before adding files.
