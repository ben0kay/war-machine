# Player body-condition silhouette — PH1

**Implemented, awaiting local build/visual validation.** `BodyConditionWidget.tscn`/`.cs` draws a compact front-facing six-part silhouette and severity labels: OK, ! Hurt, !! Severe, × Disabled. The actor's own left arm/leg appear on the silhouette's right. Labels and indicators complement colour; no fracture/bleeding state is invented. Hidden vitality is never drawn as an HP bar.

One instance is composed under `GameplayHUD/Screen` by WorldController, alongside the existing NPC overlay and before Backpack. The FPS world inherits this composition. At bottom-left, it sits above the hotbar to avoid overlap on narrow windows. It reads all six initial regions, listens to Body.ConditionChanged and Vitals.Died, and unsubscribes on rebind/exit. The footer indicates injuries or death; no medical inventory tab or input interception. Shape drawing is cached by Godot and QueueRedraw runs on events; there are no idle frame callbacks/timers/tweens.

`FEEDBACK/Configuration/FeedbackMaster.tres` owns healthy/severe fraction thresholds (default .75/.40), four region colours and margin. Gameplay condition maxima and vitality stay under PlayerMaster. NPC HealthBarOverlay explicitly skips PlayerController ownership and keeps its existing injured-only NPC bars.

Local checks: build and run [PH0 smoke tests](../../../DEBUG/TEST/PlayerVitals/README.md), then F6 World/FPS and inspect healthy, each damaged region, 0-condition limb, independent region healing, death, left/right anatomy, small/16:9 viewports, backpack/hotbar input and injured NPC bars. Optional visual pulses, precision hitboxes and injury effects remain future work. No Godot visual acceptance is claimed.

## AM1 ammo card placement (2026-10-28)

The body silhouette remains anchored at x=margin…margin+244 near the bottom-left. The **AM1 ammo readout is a sibling**, positioned to its right and immediately to the left of the bottom-centred hotbar at desktop widths; at narrow widths it is moved above the hotbar. The body widget's pose, labels, health subscriptions and layout are unchanged; no numeric generic player health bar was introduced. Source only, visual layout QA pending.
