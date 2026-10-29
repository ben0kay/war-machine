> **PH2 foundation:** Shared movement now gates run speed via PlayerVitals.Stamina, charges eligible accepted jumps and reports actual grounded sprint travel after MoveAndSlide. Free walking/stance motion permits recovery; exhaustion preserves walk speed/acceleration. No extra movement callback or collision query. [Rules/settings/checks](../IMPLEMENTATION_PLAN.md). Awaiting local validation.

> **ZD1:** Shared movement optionally applies cached corpse-area slowdown; PlayerMaster defaults CorpseSlowdownEnabled=false. No extra per-player callback/query. Both views use this shared hook.

# Player movement and stance — PSR0

| Helper | Owns |
| --- | --- |
| PlayerMovement.cs | Cached camera-relative axes, event subscription/cleanup, horizontal movement, sprint speed selection, gravity/jump, MoveAndSlide, slide-contact door pushing, F10 and rest-state physics sleep |
| PlayerStance.cs | Standing/crouch/prone state, previous stance, cached capsule/clearance queries, one-shot transition requests and animated capsule height |

Both are private plain C# collaborators declared inside the existing partial PlayerController. They introduce no scene nodes, independent callbacks or generic state framework. Stance/rig/weapon state is held by its owning helper; settings, public identity/events, death/menu gating and existing virtual FPS hooks stay on the coordinator.

The single controller physics callback dispatches PlayerMovement.Advance: stance requests/clearance/capsule interpolation → jump consumption/input → virtual direction and acceleration hooks → gravity/MoveAndSlide → existing door-contact push/F10 → collision-resolved gait → stance/socket and position events → original grounded idle sleep. Preserve this order. No extra ground query, door query, timer, stance polling or physics loop is introduced. Camera rotation axes refresh on the same settings signal; reinitialize replaces the subscription and exit removes it.

Floor settings, movement equations, capsule sizes/speeds, jump rules and numerical thresholds were extracted unchanged. FirstPersonPlayer still overrides the same controller methods and records its eye pose after base physics completes, including the final sleeping tick. PlayerSettings/PlayerMaster remain the authoring owners; legacy player and scenes are untouched. Local build, collision/door/headroom, focus/menu/death and idle checks are required before acceptance.
