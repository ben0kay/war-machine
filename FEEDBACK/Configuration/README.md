# FEEDBACK/Configuration — planned master tuning

> **Now implemented:** `FeedbackSettings.cs` and `FeedbackMaster.tres` are consumed by HealthBarOverlay. Tune world/player bar sizes, world anchor elevation, player margin, 48-control maximum and 30Hz wounded-only projection. HP/damage state is never stored here. The scaffold-only text below is historical.

Reserved for `FeedbackSettings.cs` (a valid exported Godot resource class) and `FeedbackMaster.tres` **when FB1/FB2 introduces real widgets and settings consumers**. Use a single primary Feedback master with grouped Inspector values for health, temporary enemy bars, optional defence styling and future effect presentation. Do not put current/max HP, damage amounts, player state or zombie values in this configuration.

**FB0 does not create an inert/invalid .tres** or exported C# class without a consumer. This folder exists and is documented for Codex's next authorized UI phase. [Plan](../IMPLEMENTATION_PLAN.md).
