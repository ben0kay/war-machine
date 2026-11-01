# PAU — Player footsteps & ground-material audio (Codex implementation plan)

**Repository / branch:** `ben0kay/war-machine` / **`implement-td0-td1`** (never default to `main`).  
**Owner:** `PLAYER/Audio/` for footfall scheduling, audio selection, asset/configuration; `WORLD/` for resolving the world surface; `AUDIO/` for **existing shared services only**.  
**Status: PLAN ONLY — 2026-11-01.** No PAU phase is implemented or authorized. This document is a handoff for a future explicitly assigned Codex task. Do not add audio assets or runtime code on this documentation pass.

## Objective and player-facing result

While the player walks through the same playable world, hear appropriately timed, varied footsteps matching the actual physical ground: **grass, forest dirt, rock/gravel, wood, and concrete/asphalt** in the first usable pass; extendable to metal, water/mud and wetness later. Running is louder/faster; crouching is softer/slower. **Footsteps stop immediately on idle, jumping/airborne movement, death, blocked input, or unload.** The design is shared by current FPS and secondary top-down player, so neither view creates its own footstep simulation.

This is **not** a request for terrain painting, terrain rebaking, model changes, animation rewrites, new gun sounds, a new global audio manager, or a new zombie AI feature.

## Verified source facts (inspect again at implementation HEAD)

- `PLAYER/Movement/PlayerMovement.cs`: **one** player physics driver computes collision-resolved horizontal travel after `MoveAndSlide()`, `actualSpeed`, grounded state and stance; calls `_player._animation.Advance(travel, dt, actualSpeed)`. It already sleeps physics when settled. Preserve sequence and sleep.
- `PLAYER/Visuals/PlayerAnimation.cs`: a shared `_phase` accumulates from actual speed and `GaitRate`; left/right foot lifts are derived from `cos(_phase)`. There are **no footstep events today**. Potential contact crossings are at successive opposing gait phase boundaries, but validate visually and don't assume sinusoid zeros alone mean foot-ground contact. Prone has a separate crawling pose.
- `PLAYER/PlayerController.cs` and `WORLD/WorldController.cs`: player is a shared `CharacterBody3D` and world already owns `SystemsRoot/Audio`; the world currently binds that service to weapons/listener. There is **no player footstep service binding**.
- `AUDIO/Shared/WorldAudio.cs`, `AudioCue.cs`, `WorldSoundEvents.cs`: reusable bounded positional one-shot playback, `SoundCategory.Footsteps` and per-category volume exist. `SoundKind.Footstep` already exists in `ENTITIES/Shared/Sensing/SoundStimulus.cs`; `SoundEvents.Emit` can dispatch gameplay hearing independent of playback/mute/caps. The hearing consumer must be checked for **footstep acceptance** before enabling AI reactions.
- `WORLD/Terrain/TerrainAssets.tres` has **texture ID 0 Rock/Cliff, ID 1 Grass/Ground, ID 2 Forest Dirt**. `TerrainMaterial.tres` uses **autoshader base 0 and overlay 1**, so slope/automatic blending is not identical to simply reading one manually painted ID. `WORLD/Terrain/DirtPatchAuthoring.cs` writes control maps into separately staged gameplay-preview data; when selected, footsteps must read the **active loaded terrain data**, not the original folder.
- Existing root `CodePreferences.md` establishes per-feature audio ownership. Rifle WAV/cue now live inside `EQUIPMENT/Weapons/Rifles/Rifle_001/Audio/`. Keep `AUDIO/` shared and keep new player sounds in `PLAYER/Audio/`.

## Proposed phases and permission boundaries

| Phase | Goal / main files | Prerequisites | Status |
| --- | --- | --- | --- |
| **PAU0 — audit & contract** | Inspect current gait, native Terrain3D control/texture APIs, physics floor-collision identification, authored building floor colliders, and HE1 listener filters. Record confirmed surface-ID decision logic and validation cases in this plan. No runtime changes. | None | **Planned — implement first** |
| **PAU1 — gait footfall events** | Small player-owned `PlayerFootsteps.cs` collaborator under `PLAYER/Audio/`; minimal integration in `PlayerMovement.cs`/`PlayerAnimation.cs`/`PlayerController.cs` as needed. One event per actual alternating planted foot; no audio yet. | PAU0 | Planned |
| **PAU2 — classify walked surface** | Focused `WORLD/Surface/` (or nearest existing suitable world owner) **plain resolver**, plus lightweight optional floor `SurfaceSoundTag` / profile for authored static colliders. Terrain3D's active loaded data maps to semantic surface IDs; no global scan. | PAU0–1 | Planned |
| **PAU3 — audio assets, cues & playback** | `PLAYER/Audio/Configuration/FootstepSettings.cs`, `FootstepMaster.tres`, player-owned `Sounds/<Surface>/` WAV/OGG variants and `AudioCue` resources; bind existing world `WorldAudio` once. | PAU1–2 plus authorized audio source assets | Planned |
| **PAU4 — noise, nuance & polish** | Optional `SoundKind.Footstep` stimulus via existing `SoundEvents.Emit`; stance/speed/surface loudness; later wet/mud/metal, landing/crawl sounds after separate approval. | PAU3, confirm hearing consumer semantics | Planned |

**Each phase requires separate owner authorization.** Don't run PAU1–PAU4 automatically after a review or after PAU0. One logical commit per authorized slice; reflect implemented vs awaiting local Godot acceptance accurately. If PAU0 finds an existing simpler surface-contact abstraction, use it instead of creating `WORLD/Surface/`.

## PAU1 — reliable foot contact without timers

1. Reuse the **collision-resolved** movement update after `MoveAndSlide()` and the current shared gait phase; do not drive sounds from raw key input, camera bob, frame count, a fixed wall-clock repeating timer, or an FPS-only animation.
2. Emit exactly one **left** or **right** step for a valid foot-down phase crossing while moving on a floor. Track previous/current unwrapped phase and guard against repeated crossings, first-frame spurious steps, footstep bursts after teleports/large delta, moving platforms producing camera-only movement, and rapid stance changes. Tune gait event positions against the actual animation: `cos(_phase)` lift sign changes near `π/2` and `3π/2`, but verify contact rather than blindly using zero crossings.
3. Require grounded **after** collision resolution, valid horizontal travel, nontrivial speed, active/alive player, and no UI/focus/control suppression. No regular walking steps in air, stationary, while sliding a wall or in a pause menu. Jump landings and prone dragging are explicitly **deferred**; no fake ordinary footsteps while prone in first pass.
4. Use the existing settled-physics sleep; do not wake it just to check for a step. No new `_Process`, independent player physics callback, repeating timer, continuous downward ray or allocations on every movement frame. Unsubscribe/clear state on reinitialize/exit.
5. Make a lightweight, optional *diagnostic* step counter with no default per-frame overlay/log spam. Verify cadence with both current player views, speeds, inclines and a large frame delta.

## PAU2 — surface selection contract

**Decision order at each accepted footfall** (rather than every frame):

1. Determine actual support surface beneath the **contacting foot** (with a small bounded center fallback). Prefer a verified collision contact/shape reference from existing movement state if it identifies support correctly. If Godot's collision state does not identify the ground reliably, use **at most one short downward physics ray per accepted footfall**, excluding the player's collider, with a dedicated collision mask and an explicit maximum distance; avoid obstacle/wall misclassification.
2. If grounded on an authored floor/structure collider, look for an explicit semantic material tag/profile on that collider or its well-defined owning static-body ancestor (e.g., **Wood, Concrete, Metal**). Don't infer the material from a mesh name, albedo screenshot, asset filename or texture-pixel colour. Preserve existing structure collision, top-down fade and static streaming contracts. Default untagged interiors to a configurable generic hard surface rather than reporting grass through the floor.
3. If the support is Terrain3D, ask a small `WORLD/` resolver for the semantic surface at the **world XZ foot point** using the **actual loaded Terrain3D control/texture data**. Consult the installed addon API/version and its shader semantics at implementation time: ID 0 Rock/Cliff, 1 Grass/Ground, 2 Forest Dirt. Painted blends and auto-slope material selection require a deliberate **dominant material / configurable threshold** policy. If a stable public addon helper is available, use it; **do not hard-code the DTP author's private control-map bit masks in PLAYER** or read .res data from disk per step. Handle out-of-region/no data by a safe fallback, never mutate terrain.
4. Priority: the **walked collider** (e.g., a wooden porch covering native terrain) beats the terrain beneath it. Preserve bounds, terrain hole behaviour, native collision and player floor slope rules. Don't confuse dense foliage visuals over grass with a new ground material.
5. Return a small semantic type (such as `Grass / Dirt / Rock / Wood / Concrete / Metal / Unknown`) and optional confidence/secondary blend if useful. The **player never knows** Terrain3D texture slot numbers or individual structure names. The resolver is read-only, stateless/cached and has **no own processing loop**.

**Test cases:** standing on grass, steep auto-rock, painted Forest Dirt and its feathered edge, over a rocky patch, inside the store and police station, on a wooden floor/staircase, on asphalt, near a Terrain3D hole, under a bridge, on a moved/streamed authored structure, and outside native data. Include a safe unknown fallback instead of a null-resource exception.

## PAU3 — playback + tunables

**Proposed eventual assets (not created by this plan):**

```text
PLAYER/Audio/
├── README.md
├── FOOTSTEPS_IMPLEMENTATION_PLAN.md
├── PlayerFootsteps.cs
├── Configuration/
│   ├── FootstepSettings.cs
│   └── FootstepMaster.tres
└── Sounds/
    ├── Grass/       (3–5 distinct short step WAV/OGG variants + cues)
    ├── Dirt/        (3–5)
    ├── Rock/        (3–5)
    ├── Wood/        (3–5)
    ├── Concrete/    (3–5)
    └── Metal/       (optional later)
```

- **Audio source gate:** Use owner-provided, properly licensed or specifically authorized original/generated samples; don't invent references to nonexistent files or silently download copyrighted assets. If samples are unavailable, PAU1–2 can be tested with counters and PAU3 remains blocked until authorized audio is ready.
- Bind one `WorldAudio` service from the existing world composition; don't introduce a per-foot `AudioStreamPlayer3D` or new world audio singleton. Use `SoundCategory.Footsteps`, short non-looping spatial `AudioCue`s and the existing voice pool. Keep the player listener position/own-foot sound audibility sensible without bypassing spatial or voice budgets.
- Put adjustable **Enabled, StepVolumeDb, minimum travel/speed, cadence clamp, surface mapping/blend threshold, surface-specific cue arrays, pitch jitter, repeat-avoidance**, and **walk/run/crouch** gain multipliers into one typed feature-owned `FootstepMaster.tres`. Author explicit defaults, clamp invalid values and avoid a forest of per-surface managers or independent render-distance settings.
- Choose one variant on **accepted footfall only** (ideally prevent immediate exact repeats), use bounded slight pitch/volume randomization, and respect existing global/category/user volume. One step = at most one playback request; voice rejection or missing sample must never stop player motion.
- World position is the resolved foot/ground contact at the moment of the step. When audio is disabled, stop playback requests but do not alter movement or gameplay noise semantics. No per-frame resource loads, cue creation, LINQ, node tree searches or extra pooled players.

## PAU4 — AI hearing (explicit opt-in; not merely sound playback)

- `ENTITIES/Shared/Sensing/SoundStimulus.cs` already defines `SoundKind.Footstep`. Reuse the existing `WorldSoundEvents.Emit(profile, SoundKind.Footstep, contactPosition, sourceId, emitterId, playbackCue, gainDb, loudnessMultiplier)` rather than creating a second hearing dispatcher.
- **Audit HE1 zombie code first:** current gunshot investigation may filter by kind and is not proof that footsteps produce a reaction. Do not silently make all zombies omniscient, retarget every step, or add per-zombie footstep searches. A dedicated *future authorized* hearing slice may add sensitivity/threshold and retarget cooldown.
- Suggested tunable noise priority (not fixed metres): **prone drag later / crouch < walk < sprint**, with hard rock/metal generally louder than soft grass. Author radii/gains through player settings and the existing `SoundEmissionSettings` / `AudioCue` conventions; validate actual HE1 reactions when implemented.
- Muted/missing/capped playback does **not** cancel gameplay hearing. A hearing radius of zero allows playback-only footsteps; a zero action loudness suppresses both. Do not broadcast an event merely because the camera bobs.

## Performance, preservation and acceptance

**Activation:** existing player movement/animation physics callback while movement is active. **Work while active:** existing travel/phase update plus a tiny phase-crossing check. **Expensive work only on a valid step:** one support-material lookup (at most one optional short ray), one cue selection and at most one pooled sound request / independent hearing emission. **Sleep:** stationary, blocked/dead, airborne, uninitialized/unloaded/focus-lost; release subscriptions and audio emitter ownership. No new always-running scene manager or actor/terrain scan.

**Preserve:** player speed, stance/jump/stamina physics, FPS head bob/lean/ADS, shared top-down mechanics until their separate retirement, existing foot pose, weapon handling, rifle audio/AI gunshot dispatch, zombie sensing, terrain materials/collision, DTP previews, structure streaming, sound caps and editable settings. Do not rebake or silently change saved terrain.

**Local validation when a phase is implemented:** one `dotnet build war-machine.csproj` when available; F6 `WORLD/FirstPerson/FirstPersonWorld.tscn` and brief F6 `WORLD/World.tscn`. Inspect idle, walk, run, crouch, prone, jump/fall/land, stairs/wood/concrete, Terrain3D grass/rock/dirt feathered paint, nearby structure platforms, focus/menu, death, streamed props and scene unload. Confirm no double footsteps across phase wrap, no sounds in air, no wrong below-floor terrain material, no new idle frame loop, and no more than one material query per actual footfall. With real samples, listen at normal game mix level. Test AI hearing separately when PAU4 is specifically authorized. Owner handles substantive listening, visuals and FPS profiling; **no FPS gain/impact is claimed by this plan**.

## Codex kickoff — PAU0 only (copy/paste when authorized)

> In `ben0kay/war-machine` branch `implement-td0-td1`, read `AGENTS.md`, `CodePreferences.md`, root `README.md`, `LessonsLearned.md`, `PLAYER/README.md`, `PLAYER/Movement/README.md`, `PLAYER/Audio/README.md`, and `PLAYER/Audio/FOOTSTEPS_IMPLEMENTATION_PLAN.md`. Inspect current `PlayerMovement`/`PlayerAnimation`, world Terrain3D material and *active* data interface, current building colliders and `WorldAudio`/`SoundStimulus`. Perform **PAU0 audit only**: confirm reliable alternate-foot contact phase and support-collider identification; document concrete Terrain3D surface sampling and floor-tag approach, edge cases and any source/doc conflicts. Do **not** implement footsteps, create audio samples, alter world/terrain or advance PAU1–4. Update only the owner plan with verified findings and exact subsequent implementation scope. Keep checks brief; one logical commit on the named branch; report commit and outstanding in-Godot validation.
