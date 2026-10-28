# FEEDBACK/Configuration — Inspector presentation master

`FeedbackSettings.cs` and `FeedbackMaster.tres` are already consumed by the pooled numeric health overlay for NPCs. Existing player bar width/height/margin fields are **legacy prospective numeric UI controls**; PH0 must exclude the player's HealthComponent from that overlay. Do not delete these existing settings as part of a documentation task or alter NPC bar defaults.

When PH1 starts, add only settings actually used by the **six-region silhouette**: position/size, healthy/amber/severe/incapacitated colours and brief change-pulse duration. Those are presentation settings, never authoritative body condition or Vitality values; game state belongs in `PLAYER/Stats/` and the player master.

[FB plan](../IMPLEMENTATION_PLAN.md) · [BodyCondition design](../../PLAYER/Stats/BODY_CONDITION_DESIGN.md). No PH0/PH1 code has yet been added.
