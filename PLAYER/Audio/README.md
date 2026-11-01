# Player audio ownership

**Status: planning only (2026-11-01). No footstep scripts, resources or samples have been created.**

Player-specific movement and body sounds belong in `PLAYER/Audio/`. Shared pooling, spatial playback, cue types and gameplay hearing events remain in `AUDIO/Shared/` and are **not** to be duplicated. Weapon clips remain with their weapons (for example `EQUIPMENT/Weapons/Rifles/Rifle_001/Audio/`).

The authoritative proposed implementation is [FOOTSTEPS_IMPLEMENTATION_PLAN.md](FOOTSTEPS_IMPLEMENTATION_PLAN.md). It covers gait-triggered left/right foot contacts, Terrain3D and authored-floor surface classification, stance and pace variations, optional zombie hearing and performance/validation boundaries. **No plan phase is authorized merely by this document.**

When approved, keep `PlayerFootsteps` and its grouped `FootstepMaster.tres` configuration here, and sound variants inside `PLAYER/Audio/Sounds/<Surface>/`. Do not create independent audio managers, always-running footstep timers, or a second playback service.
