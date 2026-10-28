# Hearing, sound stimuli and movable investigation anchors

> **Status: HE0 implemented in source, awaiting local validation. HE1–HE3 remain proposed.**
> **Related roadmap:** [PERCEPTION_IMPLEMENTATION_PLAN.md](PERCEPTION_IMPLEMENTATION_PLAN.md) → PER2 (now specified as per-zombie randomized last-seen search) and PER5 (hearing). PER0/PER1 visual detection already exist; PER2 is implemented; HE0 now supplies gameplay noise events, but AI listening/reaction is not implemented.
> **Branch:** `implement-td0-td1`. **Primary first consumer:** zombies in `WORLD/World.tscn`. Long-term consumers could include animals, hostile humans and robots, with species-specific responses.

## Desired player-facing behaviour

- Zombies initially **wander around their spawn point** using the existing configurable `WanderRadius`.
- A sufficiently audible action (gunshot, melee strike, looting, footsteps, etc.) creates **one spatial sound stimulus at the position where the action happened**. Each listener evaluates the sound's intensity/range from *that immutable position*, not from the player's continuously updated location.
- A zombie that hears but **does not see** the player walks toward an **approximate sound location** (with a small per-listener positional error / search radius, rather than perfect precision). During this travel, normal visual detection can acquire the player and take priority immediately.
- If it reaches the investigation area **without finding the player**, its **wander anchor moves to the recorded sound position**. It then resumes random wandering within `WanderRadius` around that **new** anchor, not the original spawn. This is persistent for that zombie during the current runtime unless later changed by a new investigation or future explicit reset rule.
- Weak nearby sounds may trigger a shorter local search, with optional **minimum intensity for relocating the long-term anchor**. This allows footsteps to attract a nearby zombie without necessarily relocating every wander home; make the rule configurable.
- Several shots do not cause constantly oscillating targets. Define a small event-refresh/priority rule to accept stronger/newer relevant stimuli while investigating.
- **Hearing is not eyesight.** A zombie may react to a shot behind itself or behind a building without gaining LOS or learning the player's live position.
- Current zombies have **no player attack**. If they see the player en route, use **existing visual chase** now; eventual melee/attack logic remains a *separate* future phase.

### State and priority

Illustrative state ownership in `ZombieController` (keep it simple; do not mandate a state-machine framework):

```text
WANDER near wanderAnchor (initially spawn)
   + qualifying sound -> INVESTIGATE recordedPosition + small deterministic offset
INVESTIGATE
   + direct sight -> existing CHASE (visible target position only)
   + reached investigation radius, no sight -> wanderAnchor = recordedPosition -> WANDER
   + unreachable/stuck or max investigate time -> abandon exact approach safely;
     configurable choice to keep the last confirmed anchor or switch to a nearby reachable
     point; never teleport or track an unseen moving source
CHASE
   + sight lost -> follow the separately planned PER2 last-seen logic when implemented;
     each zombie may independently abandon its visual search early, part-way or at the last-seen point
     hearing does NOT secretly preserve a live target position
```

**Important distinction:** `spawnPosition` can remain a permanent historical reference, but a mutable `wanderAnchor` becomes the **actual origin** for subsequent random goals. Do not overwrite `spawnPosition` merely to imitate anchor changes; use explicit names so a future return-home or reset system remains possible. On a new sound, clear the old random wander goal and select one investigation destination; don't use `_target.GlobalPosition` unless `IsChasing` is true and sight was verified.

## Confirmed current integration points (reviewed 2026-10-27)

- `ENTITIES/TYPES/Zombies/ZombieController.cs`: one staggered `_brain` physics timer (~0.4s); `Think()` first updates LOS/chase, otherwise wanders around `_home` with `_config.WanderRadius` and random 1–3s rests. It already has `_goal`, `_hasGoal`, `_stuck`, and `IsChasing`; currently **no sound reaction or investigation memory**.
- `ENTITIES/TYPES/Zombies/ZombieSettings.cs` / `Configuration/ZombieMaster.tres`: Inspector-editable detection ranges, wander radius, movement speeds and sight interval. Add hearing/investigation settings here (resource duplicated per actor).
- `ENTITIES/Shared/Sensing/VisibilityResolver.cs`: sight geometry only. Keep it separate from hearing; sound need not require visual LOS.
- `EQUIPMENT/Weapons/Shared/FirearmController.cs`: `TryFire()` returns `true` **only after a successful shot**, resolves hits, plays short visual effects and optionally calls `_audio.Play()`. Publish a **gameplay sound event from this successful-shot path**, even if `ShotSound` is null or muted.
- `EQUIPMENT/Weapons/Shared/FirearmSettings.cs` and `EQUIPMENT/Weapons/Rifles/Rifle_001/Configuration/RifleMaster.tres`: already have `ShotSound` and `ShotVolumeDb` for audible playback. **Gameplay hearing radius/strength is separate**; do not infer AI awareness directly from volume in dB.
- `PLAYER/PlayerController.cs`: knows movement/crouch/running and emits `PositionChanged`. It currently has **no crawling, looting or melee action**. Future player-action systems should publish sound events when those mechanics actually exist.
- `WORLD/WorldController.cs`: knows placed zombie instances and player/weapon composition; appropriate to wire one scene-local sound dispatcher once, not to contain noise attenuation rules.
- The robot's `ScanSensor` and cyan scanner visuals are unrelated to this initial zombie hearing implementation. Do not attach hearing or change robot targeting without explicit authorization.

## Suggested shared design

```text
ENTITIES/Shared/Sensing/
  PERCEPTION_IMPLEMENTATION_PLAN.md        # existing general roadmap, PER5
  HEARING_INVESTIGATION_PLAN.md            # this document
  SoundStimulus.cs                         # future lightweight readonly event/value
  SoundDispatcher.cs                       # future world-local event dispatcher (not global singleton)
  HearingSensor.cs                         # optional reusable listener filtering, only if worthwhile
ENTITIES/TYPES/Zombies/
  ZombieController.cs                      # owns audible investigation / mutable wander anchor
  ZombieSettings.cs                        # owns hearing response and search tuning
  Configuration/ZombieMaster.tres          # authored defaults
EQUIPMENT/Weapons/Shared/
  FirearmController.cs                     # publish only on confirmed successful shot
  FirearmSettings.cs                       # per-weapon gameplay hearing radius/strength
PLAYER/
  PlayerController.cs + future actions     # publish stride, loot, crawl, melee, etc. when available
WORLD/
  WorldController.cs                       # register/unregister active listeners and composer
```

- **Stimulus contract:** immutable `WorldPosition`, `Kind` (e.g. `Gunshot`, `Footstep`, `Looting`, `Melee`, `Impact`), gameplay `Radius` and/or `Intensity`, event time/sequence ID if needed for arbitration, optional originator reference/affiliation **for filtering only**. Event consumers retain coordinates; they **must never follow the originator's live transform** based on sound alone. A sound event should not retain actor nodes indefinitely.
- **Dispatch:** a single scene/world-local notification route to explicitly registered active listeners. For current ~12 zombies, immediate squared-distance filtering on discrete stimuli is inexpensive. Skip inactive/dead actors and suppress zero-radius sounds. As active populations scale, only consider a world-local spatial grid / nearby listener index after profiling. Avoid `GetNodesInGroup()` per event, engine-wide singleton broadcasts and zombie-per-frame searches.
- **Hearing threshold:** each species gets `HearingSensitivity` / `MinimumAudibleIntensity` or equivalent; use a simple, predictable falloff for distance. No cone requirement (ears hear behind); advanced wall/terrain acoustic occlusion/reflection is optional later. Never confuse a 3D audio falloff curve with AI hearing logic.
- **Reaction:** the *listener*, not the emitter/dispatcher, chooses whether to investigate, flee, ignore or change its wander anchor. Zombie `HearSound(stimulus)` stores the snapshot and wakes the existing decision/movement mechanism; no separate always-running zombie hearing loop.
- **Approximation:** pick one seeded XZ offset within `InvestigationAccuracyRadius` at sound acceptance, and store it. Don't reroll that offset each brain tick or the goal will jitter. Distinguish `InvestigationArrivalRadius` (when to stop searching) from `WanderRadius` (future roam circle).
- **Priority:** `confirmed visual chase > deliberate nonvisual investigation > ordinary wander`. PER2 last-seen investigation and HE1 sound investigation represent **separate evidence and completion policies**: early visual give-up returns to ordinary wandering, while a sufficiently strong heard stimulus may relocate the wander anchor only after the audible investigation succeeds. Decide which new nonvisual clue may supersede a visual memory using a strength/recency rule; never read the source actor's live position. When a stronger/newer sound supersedes the current one, use a cooldown/minimum priority difference so repeated automatic shots don't retarget every 0.65s. Do not have a remembered noise override a currently visible player.
- **Completion:** if no visual contact by arrival, use **the original recorded sound coordinate as new wander anchor**, not the inaccurate investigation offset. Thereafter random patrol destinations are sampled around that anchor until another qualifying stimulus moves it.
- **Pathfinding limit:** current zombies use direct `MoveAndSlide()`, no navigation. Use existing stuck protection, bounded timeout and safe fallback. Do **not** attempt to cross buildings/steep cliffs with teleportation; proper navigation belongs to a future task.
- **Sound playback is optional:** an AI noise event is not an `AudioStreamPlayer3D` and should work in silent/headless tests. Keep the real audio and gameplay hearing independently configurable.

## Starter tuning — suggestions, NOT implemented/hardcoded

| Player or world action | Illustrative gameplay hearing radius | Notes |
| --- | ---: | --- |
| Unsuppressed rifle shot | 65–90 m | Major distraction; likely changes the long-term wander anchor |
| Melee impact | 8–16 m | Future action; meaningful nearby |
| Running footsteps | 7–12 m | Emit by stride/distance, not every physics frame |
| Normal walking footsteps | 3–7 m | Usually only nearby zombies |
| Crouched walking | 1–3 m | Quiet, not guaranteed silent |
| Crawling | 0.5–2 m | Future movement mode only |
| Looting / inventory interaction | 2–6 m | On discrete relevant actions, not each UI operation |
| Environmental impact / door slam | Source-specific | Eventually applies to non-player sources |

All values are design starting points, not physical acoustics. Tune against the current 18m visual detection and actual scene scale. Suggested zombie Inspector settings: `HearingSensitivity`, `InvestigationAccuracyRadius` (~2–4m), `InvestigationArrivalRadius` (~1.5–3m), `InvestigationTimeoutSeconds`, `SoundRetargetCooldownSeconds`, `AnchorRelocationMinIntensity`. Keep existing `WanderRadius` editable and **recenter it** instead of introducing a second duplicate wandering radius.

## Implementation stages — future approval required

| Phase | Deliverable | State |
| --- | --- | --- |
| **HE0 — shared spatial noise** | Minimal immutable stimulus and world-local dispatcher; expose one shot-noise setting on weapon resource; issue a sound on successful `TryFire()` independently of audio playback. No zombie behaviour change yet. | Implemented; awaiting local validation |
| **HE1 — zombie hearing + anchor** | Receive and filter shot events, investigate a recorded offset position, prioritize current LOS chase, recenter `wanderAnchor` when reached without visual acquisition, handle timeout/stuck/multiple shots, cleanup on death/unload. | Proposed |
| **HE2 — quiet/loud actions** | Add event-driven running/walking/crouched footstep thresholds, then melee, looting and crawl only as those systems become real. Per-action sounds use shared contract and data-driven radii. | Proposed |
| **HE3 — polish and scale** | Optional hearing debug circles/last-noise marker, obstacle muffling with bounded budget, suppression/equipment/weather modifiers, listener spatial indexing if profiling justifies. Keep these optional. | Proposed |

HE0 is implemented. The next useful playable pass is **HE1**, after separate review/authorization, without changing robot scanner, natural eyesight geometry, melee attacks or the AI navigation system. PER2 last-seen memory may be integrated deliberately if implemented earlier; HE1 must function without PER2.

## Acceptance / regression checks (brief)

1. Zombie spawns -> wander anchor is spawn; random destinations remain within configured `WanderRadius`.
2. Player fires out of view/behind zombie -> nearby zombie goes to **recorded shot position**, not the running player's new location. Distant zombies beyond range stay unaffected.
3. Zombie reaches search area with no LOS -> settles and resumes wandering **around shot coordinate** even after the player moves away; only an accepted newer event/explicit reset changes that anchor.
4. While investigating, the zombie sees the player -> normal sight chase takes priority. After LOS loss, do not use an unverified live position.
5. Walking/running/crouch sounds (once implemented) have increasing audibility; noise dispatch is silent when the actor stands still. Muted/missing real SFX does not disable gameplay hearing.
6. Rapid automatic shots are handled predictably without target jitter, duplicates or growing unbounded memory. Multiple zombies respond independently with stable per-listener offsets.
7. Stuck/no-path investigation terminates gracefully rather than cycling forever; no teleport, no additional idle timers, no leaks on actor death/deactivation/scene exit.
8. Godot C# build, F6 `WORLD/World.tscn`, confirm current sight/cover, F7 debug cones, gunshot effects and hit damage are unchanged. User does visual gameplay/performance acceptance.

**HE1–HE3 remain design references.** The HE0 handoff below supersedes old statements that the dispatcher does not exist. When authorized, follow root `CodePreferences.md`: one commit per job, brief validation, update the sensing and zombie READMEs, report outstanding local checks.

## PER2 integration note

PER2 visual investigation is now implemented and, by explicit owner direction, already relocates the wander anchor to the physically settled give-up position. Future HE1 must preserve this visual behavior and reuse the actor-owned anchor policy; anchor relocation is no longer exclusive to hearing. HE0 supplies shared gameplay events; HE1–HE3 remain unimplemented. Playback alone still does not supply AI clues.

## HE0 implementation handoff — 2026-10-28

Owner authorized a shared emission route through AUDIO plus independent gameplay hearing, with common action loudness and no sound assets required. `AUDIO/Shared/WorldSoundEvents.cs` is composed once by the existing WorldAudio. `SoundEmissionSettings` is reusable by future footsteps/melee/interactions. `SoundStimulus` and `SoundDispatcher` live here; no actor reference, global singleton, event queue or scene scan. Both current weapon masters opt in (rifle 80m, MG 70m at loudness 1); old custom firearms without ShotEmission preserve their earlier playback-only behavior. Active world/FPS already inject the same AUDIO owner through WeaponSlots. Other consumers must explicitly use their world's service.

Each successful TryFire closes its cooldown gate, then emits one snapshot at the actual muzzle coordinate (not the cover-adjusted ray origin), before optional pooled playback. Failed cooldown/unequipped/invalid-aim calls emit nothing. Numeric source actor ID is separate from emitter weapon ID. Per-world sequence supports later HE1 arbitration; it is not a save identifier. A service without audio can also be constructed for silent gameplay/tests. Shutdown clears subscribers and rejects future emissions.

Shared loudness is linear amplitude. Effective amplitude is profile loudness times action multiplier (individual and result caps 0–4); zero suppresses both. Playback adds 20log10(amplitude) dB to the existing gain, still subject to AudioCue distance and pool limits. Hearing radius is base radius times sqrt(amplitude), capped 1000m, with peak intensity equal to amplitude and linear distance falloff. Personal master/category volumes never alter hearing. Radius zero means playback only. These rules are simple gameplay tuning, not physical propagation.

**No HE1 behavior:** zombies/deer/robots do not subscribe, investigate or change anchors from sounds yet. No footsteps, additional sounds or audio files were added. HE1 must range-filter registered active listeners, decide thresholds/priorities, unsubscribe and preserve PER2. See [shared API](../../../AUDIO/Shared/README.md).

**Checks/handoff:** source and resource paths/load counts plus finite/zero/multiplier/falloff arithmetic reviewed. No Godot/.NET executable is available here, so compilation and runtime event/playback acceptance remain pending. Locally run `dotnet build war-machine.csproj`; F6 both World and FPS. Temporarily subscribe to `WorldAudio.SoundEvents.Hearing.Emitted` and count snapshots: one per accepted rifle/MG shot, none for cooldown/unequipped/UI-blocked attempts; positions stay fixed as the player moves and cover-adjusted shots retain their muzzle origin. Confirm radius-zero vs loudness-zero, shutdown/reload and independent world instances. With Stream unassigned/audio OFF/full voice pool, noise counts must remain unchanged. When a real rifle sound is assigned, verify one pooled sound per shot, existing nested caps, louder/quieter gain and no legacy double-play. Remove the temporary subscriber after checking. No runtime acceptance is claimed.
