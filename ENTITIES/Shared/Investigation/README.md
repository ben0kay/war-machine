> `Begin` also accepts an optional per-episode `durationScale` (default 1, sanitized 0.1–1). HE1 blocked sounds use it to shorten the capped deadline without changing stored PER2 settings or sight/hit callers.

# Shared fixed-point investigation

**Implemented in source, awaiting local build/World/FPS acceptance (2026-10-28).** InvestigationMemory is a passive plain C# helper: remember a point, choose a partial/full destination, expire a bounded episode, and optionally dwell/look on arrival. It has no scene nodes, actor references, species types, movement, raycasts, timers or engine callbacks. A species adapter supplies confirmed clues, its position/current speed, its seeded RNG and monotonic runtime time, and owns the existing movement/sensing lifecycle.

InvestigationSettings is reusable typed policy data (weights, travel fractions, deadline scale/allowance/cap, dwell range). Each helper duplicates its settings once. Existing zombie .tres properties remain authoritative and unchanged: ZombieSettings.CreateInvestigationSettings maps them into this runtime policy at startup. No serialized-property migration or duplicate zombie master values. Restart after editing authored policy; no new live-tuning polling. A future species can provide its own settings without changing this helper.

## Contract and priorities

- Remember copies a finite Vector3 and clue kind; no live source reference.
- Observe accepts a confirmed visual point and cancels the episode while retaining last-seen memory.
- Begin draws early/moderate/thorough once from the supplied RNG, freezes Destination, and sets the deadline. Defaults preserve PER2: 40/35/25 weights; early 40–60%, moderate 65–90%, thorough 100%; time = horizontal distance / current species speed × 1.10–1.60 +3s, capped at 60s.
- Arrive returns true only for a thorough travel episode and starts its 0.8–2s look window. False means the owner should finish immediately.
- Expired reports either deadline or look-window expiry. LookOffset supplies the original small look-around curve. The owner handles expiry/invalid target/disabled trigger/stuck movement and calls Clear.
- Clear removes phase, point, destination, deadline and profile state. Death, deactivation and scene exit clear zombie memory through existing lifecycle hooks.

Current clue kinds are LastSeen, Hit and Sound. HE1 supplies a single estimated sound point and optional per-episode commitment weights to Begin; defaults, fractions and deadline policy are unchanged. The optional override never mutates the stored settings. Listening/arbitration stays in ZombieHearing, not this passive helper.

## Zombie integration

ZombieInvestigation.cs remains a thin species adapter for sight/hit triggers, AI goals, blocked-path handling, look yaw and settled wander-home relocation. Confirmed sight has priority. After losing sight it uses the last verified point. A **surviving actual bullet contact from the registered player target** copies that shooter's position once at impact and starts/refreshes the same episode if not already visually chasing. During active chase the newest hit clue can be retained until the next perception tick; confirmed sight replaces it, sight loss can use it. Repeated hits are new clues and may restart the episode; hidden movement by itself never changes the stored goal.

HitInvestigationEnabled=true is independently editable on both zombie masters. LastSeenInvestigationEnabled controls visual-loss episodes only. Both use the same give-up tuning. The trigger runs in ReceiveImpact's Bullet branch before flinch enable/cooldown gating, after the existing alive/active/spawn-ground guards. A lethal hit cannot start investigation; an armour-blocked but real bullet contact can. Q/bat hits do not activate this trigger. Nearby gunshots use the independent HE1 Sound clue, not a Hit clue. No firearm queries, damage values or source-type pipeline changes.

Acquisition/reacquisition still uses normal timer-driven cone/LOS rules, including existing PER2A alerted tracking. Being shot from outside vision range can start travel toward the sampled position, but does not grant ongoing visual tracking. Native collision, injury locomotion and the >2s stuck fallback remain authoritative; no new pathfinding/teleport. Movement can sleep when stopped; passive episodes age on the existing ~0.4s zombie brain timer. Home anchoring remains the settled reachable position, never a forced snap to the hidden shooter.

## Acceptance in existing worlds

Build `dotnet build war-machine.csproj`, then F6 World and WORLD/FirstPerson/FirstPersonWorld.tscn. No additional test scenes.

1. Confirm ordinary detect → hide → partial/full search → give-up still works, on slow and runner zombies. Thorough search should dwell/look; blocked approaches give up safely.
2. From beyond detection range or behind the zombie, land a nonlethal shot, then hide/move sideways behind cover. It should investigate the old shooting point (or its randomly chosen fraction), not continuously follow the hidden player. A fresh hit may refresh that clue.
3. Step into real LOS during the search: confirmed pursuit takes over. Toggle HitInvestigationEnabled=false on a master/restart: shot-only search stops, existing sight-loss policy remains. Reverse the trigger toggles to check independence.
4. Disable bullet flinch or shoot during its cooldown: a surviving actual hit still supplies a clue. Kill the zombie: no search/callback/corpse movement remains. Q/bat and missed shots must not trigger this shot-investigation feature.
5. Repeat with limping/crawling/immobilized zombies and reload/unload both scenes. Immobilized actors can retain an expiring clue but cannot self-propel. Inspect idle: no new brain/physics callback or query while unused.

Source extraction, settings/resource paths and whitespace checks only here; Godot/.NET unavailable. Compilation, seeded runtime equivalence, actual shot/cover behaviour and performance remain unverified. No measured improvement claim.
