# AUDIO — reusable sound playback foundation

> **Status: structure and configuration only.** There is **no live AudioManager, pooling, sound asset or world wiring** yet. These directories and Inspector-readable AudioMaster are a handoff for Codex; current rifle/robot/weather playback is unchanged.

War Machine's fully 3D, angled top-down survival world will eventually need positional weapons, footsteps, wildlife/deer, zombies, robots, machinery, weather and ambient sound. It also has an inherited first-person test. Keep playback shared and bounded, not one always-running AudioStreamPlayer per zombie.

## Folder map

```text
AUDIO/
  README.md / IMPLEMENTATION_PLAN.md
  AudioSettings.cs
  Configuration/AudioMaster.tres
  Shared/                       future AudioManager, cue resources, routing
  OneShots/
    Weapons/                    gunfire, reload, melee and impact
    Movement/                   footsteps, slides, falls
    Creatures/                  zombies, deer, wildlife, damage
    Interactions/               doors, scavenging, items, switches
    UI/                         HUD, menu and warnings (2D)
  Loops/
    Ambience/                   forest, caves, buildings
    Weather/                    rain, wind, blizzards
    Machinery/                  robots, generators, engines
    Creatures/                  rare sustained growls/breathing
```

Each subfolder has a README so it exists in Git before audio assets are supplied. Audio files belong inside these asset folders; generic playback code belongs in `Shared/`.

## Planned global voice handling

Use one world-owned playback service with reusable pooled `AudioStreamPlayer3D` and `AudioStreamPlayer` nodes. Master defaults: **20 total voices**, up to **14 one-shots**, **6 loops**, **5 creature voices**, **2 voices per emitter**, and **3 concurrent copies of one cue**. Limits are **nested caps**, not additive. **These values are currently data only; they do not affect live sounds.**

When 50 zombies request groans at once, only the closest/most relevant few should play. Reject out-of-range optional sounds before using a slot. Rank requests by distance, category and priority; prefer immediate player feedback/nearby danger over distant groans or ambience. When all slots are full, drop/replace the least useful optional sound rather than create a new voice. Include slight pitch/gain variation later to avoid a choir of identical zombie sounds. Don't muffle important gameplay feedback merely to reach a global volume target.

**One-shots:** save the world position of an accepted rifle shot and play the short tail **there**, even if the player moves away. Recycle a pooled voice on `Finished`; no per-shot node construction. **Loops:** start/stop explicitly, optionally follow their source, use fades and coarse distance relevance updates, end on disable/death/unload; never start one unbounded loop per creature.

**3D listener:** the overhead camera is elevated, so use an intentional player-centred `AudioListener3D` (or equivalent) rather than accidentally attenuating a rifle shot as if the player's ears were 36m above them. Ensure the FPS test switches cleanly without competing active listeners. UI sounds are 2D. Later define/validate Godot buses for Weapons, Creatures, Footsteps, World, Weather, UI and optional Music; settings offsets apply at the right category or bus once implemented.

**Performance:** zero/OFF should stop voices and processing; no always-on loop scanning when nothing is playing; no per-zombie sound timers; no world searches per cue. Reuse event-driven trigger points, signal-based recycling and one bounded, active-only relevance pass for ongoing loops. Reload/scene unload must stop and release audio handles. No built-in sound effect should force a world-wide listener query.

## Keep AI hearing separate from audible playback

The planned zombie hearing/perception feature is defined in `ENTITIES/Shared/Sensing/HEARING_INVESTIGATION_PLAN.md`. Gameplay gunshot stimuli should be raised independently of the audio playback outcome. **Muted/capped/missing sound must not make a real shot inaudible to zombies.** The audio manager only presents SFX; individual AI systems decide whether to investigate recorded positions.

## Existing sources to preserve

- `EQUIPMENT/Weapons/Shared/FirearmController.cs` already plays optional `FirearmSettings.ShotSound` with its own cached `AudioStreamPlayer3D` for accepted `TryFire()`. Neither default rifle nor machine gun has a bundled stream. Future AU1 migrates **rifle only** first, avoiding duplicate playback or changing hit/trace/cooldown.
- `ENTITIES/TYPES/Robots/Robot_001/RobotAudio.cs` already synthesizes/caches spatial footstep and servo cues. Keep its behaviour until a separate authorized integration.
- `ENVIRONMENT/Weather/WeatherBridge.cs` owns current weather state; future rain loops should respond to it instead of adding one player per raindrop.

**Next:** see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md), stages AU0 (pooled manager), AU1 (rifle sound only), AU2 (creatures, robots, ambience/weather), AU3 (AI stimuli/acoustics, owned by sensing). Follow `CodePreferences.md`: minimal C#, Inspector master, dormant when off, and one commit per task.
