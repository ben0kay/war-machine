# Player status bar presentation — FB1 Planned

A screen-space component for **player health and eventual optional vitals**, owned by FEEDBACK as a reusable widget and assembled/positioned by `UI/GameplayHUD` next to (not inside) the existing hotbar.

**PH0** in `PLAYER/IMPLEMENTATION_PLAN.md` creates real HP, healing and death; **FB1** turns those events into a styled health meter. Never introduce player HP state or modify damage from this folder. Start with only health; later stamina/hunger or genuine shield meters require their own authorized phases.

Bind once after player initialization, receive event-driven updates, release subscriptions on scene unload; no idle polling or mouse-input interception. This directory currently contains no UI scenes/code. [Plan](../../IMPLEMENTATION_PLAN.md).
