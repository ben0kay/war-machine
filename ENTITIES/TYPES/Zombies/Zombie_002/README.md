# Zombie 002 — Runner

**Status:** Created as a reusable prefab, **not spawned into World.tscn**, local compile/visual test outstanding.

- **Scene:** `Zombie_002.tscn` inherits the existing `../Zombie.tscn` including its capsule and collision layers. The root overrides only its script (`ZombieRunnerController`) and master settings (`Configuration/ZombieRunnerMaster.tres`).
- **Behaviour:** derives from `../ZombieController.cs`; reuses health, sight/cover checks, wandering, pursuit and terrain recovery. The only subtype overrides are the procedural visuals and gait/acceleration feel. **No melee/attack system**.
- **Art:** `C/ZombieRunnerMeshBuilder.cs` builds a slimmer body, narrow shoulders, a tilted head, ripped dark clothing and lean limbs, with the same six limb pivots as Zombie 001. No per-mesh script, extra animation timer or collision change. `Artwork/` is reserved for an imported replacement.
- **Speed:** `ChaseSpeed = 8.3 m/s` once LOS confirms the player (slightly above the player's default `WalkSpeed 5 × RunMultiplier 1.6 = 8.0 m/s`). Wanders at `0.85 m/s`. Chase acceleration = 20 m/s² and the animation stride rate adapts to running velocity; idle continues to use the existing one-shot brain timer.
- **Config:** `Configuration/ZombieRunnerMaster.tres` is an independent `ZombieSettings` authoring resource with the same hearing-ready/sight-related contract as Zombie 001. Its 100 HP, 18 m detection range and 10 m wander radius match the existing zombie; its speed/gait/body are the first differences.

**When placing later:** world owner should instantiate this scene, put it on terrain at a sampled valid surface, and call inherited `InitializeTarget(player, seed, optionalGroundHeight)`; if using F7 vision-cone debug, register it with the existing world debug helper. Do not automatically add this runner to existing `ZombieSpawnOffsets` until authorized.

**Brief local validation:** Build `dotnet build war-machine.csproj`, open the standalone scene in Godot and inspect the inherited collision shape and script/settings overrides. For an optional temporary private test scene, call `InitializeTarget()`, then verify slow idle wandering; true LOS -> fast running with alternating arms/knees; blocked LOS -> slow wandering; no outgoing damage; correct death/hit handling. Compare movement with the player's Shift running (8.0 m/s). Actual Godot integration/rendering unverified here.
