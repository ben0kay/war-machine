# PER2A — Alerted-target tracking after initial visual acquisition

**Status: Proposed only; not implemented or authorized for code changes.**
**Date:** 2026-10-28
**Working branch:** `implement-td0-td1`
**Scope:** Zombie eyesight retention in shared World (top-down/FPS), including normal Zombie 001 and Runner 002. No navigation, hearing, attacks, weapon or robot changes.

## Problem / confirmed code

`ENTITIES/TYPES/Zombies/ZombieController.cs` runs `Think()` through the existing staggered ~0.4s brain timer. The code always requires `forward.Dot(directionToPlayer) >= cos(VisionAngleDegrees/2)`, even when `IsChasing=true` or `IsInvestigating=true`. `VisualRoot` turns gradually as it actually moves. A player who runs behind the zombie crosses outside the default **160-degree** front cone and loses **confirmed sight** at the next decision, even with a clear sight ray. PER2 then investigates the last confirmed position rather than following the player. This is correct for never-alerted zombies but frustrating after the player has already been acquired.

`VisibilityResolver.CanSee()` already provides head/torso raycasts with the current `SightBlockerMask=17` (world solids layer 1 and tree trunks layer 5); **either** clear body sample means visible. It excludes the observer and target bodies. It does **not** see through solid cover, but wall-attached glass may currently block sight (PER3 problem). Existing `ZombieInvestigation.cs` records only the last **verified** target position, not a continuously updated hidden transform.

## Proposed minimal behaviour

| State | Gate for acquiring / retaining visible pursuit | On failure |
| --- | --- | --- |
| Unaware / wandering | Current **160-degree** front cone, new 22.5m detection range, clear head-or-torso LOS | Continue wandering |
| Already visually chasing | **No front-cone requirement**; retain range 28.75m + clear head-or-torso LOS required every normal sight tick | Freeze last confirmed position, start existing PER2 investigation |
| PER2 investigating last seen position | **No front-cone requirement** inside 28.75m + real LOS; if seen, cancel search and pursue | Continue bounded remembered-position search; never follow hidden player |
| Gave up and returned to wandering | Revert to normal **160-degree** acquire cone at 22.5m | Continue wandering |

The requested rule is an **alerted/locked-on visual awareness**, not a 360-degree idle detector and not wallhack memory. Only the ongoing chase or an active investigation authorizes the wider *retention* check. On reacquisition through clear LOS, update the target goal normally. When LOS is blocked by a wall, cover rock, building, vehicle or hill, `IsChasing` becomes false and PER2 handles the last verified point. A hard range exit has the same consequence. A target behind the zombie but visible to its ray no longer causes the instant loss.

**Recommended implementation:** in `ZombieController.Think()`, compute `bool alerted = IsChasing || IsInvestigating;` once; continue to range gate using the existing `VisionRange`, but skip the 160-degree forward-dot gate when `alerted` is true. Always require `VisibilityResolver.CanSee()` to set `chasing=true`. Keep `UpdateInvestigation(wasChasing, now, targetValid)`, `_lastSeen`, `IsChasing`, stop distances and the current brain scheduling unchanged. Avoid a persistent 'detected forever' flag. Do not refresh last seen while only investigating.

**Optional future polish (defer):** a small, configurable 0.4–0.8s visual contact grace or head-turn animation to smooth momentary collision/animation occlusion. If added later, an expiry can continue movement only to the *last verified* position without reading hidden player coordinates; visual confirmation still requires clear raycasts. Not part of PER2A minimal pass.

## Phase / assistant / acceptance

| Phase | Work | Suggested assistant | Status |
| --- | --- | --- | --- |
| PER2A | Edit one alerted-target sight gate in `ZombieController.Think()`; optionally label F7 overlay as *acquisition cone only*; document impact and validate in world | Regular Chat (small isolated change); Codex for optional broader debug/AI integration | **Planned — awaits explicit approval** |

**Performance:** same **0.4s staggered brain timer**, same cached ray query, max two rays per tick and no extra nodes or new frame callbacks. Additional bounded ray checks only for already-alerted zombies outside the original cone. Inactive actors still stop their timer; after investigation ends, the forward cone resumes. This preserves FPS/top-down parity and keeps zombie-specific behaviour outside the generic shared `VisibilityResolver` (future wildlife/robots may choose different rules).

**Debug:** Current F7 red sector describes the 160-degree *initial acquisition cone*, not alerted 360-degree tracking; either clarify its label or update debug presentation in a **separate** approval if misleading. No physics/debug draw changes required for the gameplay fix.

**Minimal in-world tests (no extra test scenes):** Run normal and runner in `WORLD/World.tscn` and FPS; approach from behind unaware (should remain unseen), get acquired at front then circle behind within retention range (should keep pursuing with clear LOS), duck entirely behind a collidable rock/building (should lose direct sight and investigate), reappear behind during investigation (should reacquire), sprint past 28.75m (should drop to search), wait for search to finish and check ordinary front-only detection resumes. Compare F7 cone and test no hidden live-position tracking. Compile Godot C# locally; no runtime acceptance performed remotely.

## Separate balance change already implemented

Before implementing PER2A, the user authorized and received these **settings-only** adjustments:

- Normal Zombie 001: WanderSpeed **0.65→0.78m/s**, ChaseSpeed **1.15→1.38m/s** (+20%).
- Normal and Runner 002: DetectionRange **18→22.5m**, LoseRange **23→28.75m** (+25%) to preserve the original range hysteresis.
- Runner 002 wander **0.85m/s**, chase **8.3m/s** unchanged.
- No `ZombieController`, `ZombieInvestigation`, `VisibilityResolver`, F7 visualization, attack or hearing runtime changes.

After user approves PER2A, update this document and the owning READMEs with implemented status and local validation results.
