# Temporary PH2 stamina HUD

StaminaHud.tscn is composed once by WorldController into GameplayHUD/Screen and inherited by FirstPersonWorld. The 190×62 card is bottom-right with a 20px right / 24px bottom margin; narrow windows lift it above the hotbar/ammo row. CornerMargin is exported for easy later relocation. It remains visible at full stamina for this prototype; disabled stamina hides it.

The card reads PlayerVitals.Stamina, subscribes to Changed and viewport resize, and never changes gameplay values. It shows a numeric reserve, thin bar and READY / SPRINTING / RECOVERY / EXHAUSTED / INACTIVE text. RECOVERY includes the post-exertion wait. All controls ignore mouse input. No polling, tween, idle frame callback or duplicate per-view HUD. Exit/rebind disconnect subscriptions.

Source implemented, awaiting local .NET build and layout/visual checks. Check normal/narrow resolutions, inventory/mouse capture and death in FPS and secondary top-down. Stamina policy/settings: PLAYER/Stats/README.md and PLAYER/Configuration/PlayerMaster.tres; this GUI does not show hunger/thirst/fatigue or a normal player HP bar.

## UI4 — alignment with Body Condition (2026-10-31)

`StaminaHud.EffectiveBottomMargin(viewportWidth, CornerMargin)` extracts the original narrow-window vertical lift into a static lightweight calculation. Stamina stays in its original bottom-right location and uses this same function as the relocated BodyConditionWidget, which stacks directly above it with a 12 px gap. This only runs at initialize/viewport resize, not each frame. It changes no stamina rendering, stats or HUD behaviour. Test at 720p, 900p, 1080p and narrow viewports. Source implemented; local testing pending.
