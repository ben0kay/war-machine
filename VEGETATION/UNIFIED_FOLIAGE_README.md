# Unified foliage controller — FG0 first pass (source implemented, Godot validation pending)

**Single authoritative authoring entrypoint:** `VEGETATION/Configuration/FoliageMaster.tres`.

This resource contains the existing **editable child recipe resources**, rather than duplicating every density/seed in multiple new settings. Open FoliageMaster in Godot to edit which families/species are enabled, density, placement zones, spacing, patch sizes, tint and shared fog visibility. The referenced recipes retain their original material/mesh/shader assets and deterministic seeds. The legacy resource files remain as reusable nested recipes and a fallback, not extra runtime spawners.

**One active-world chunk lifecycle:** `VEGETATION/FoliageManager.cs` listens to the player and shared fog policy, owns one aligned 32m X/Z key grid, prioritizes nearby chunk work and releases distant content. Up to 2 family/chunk operations run per frame with a **soft 3 ms budget**. A single costly grass creation can still exceed 3 ms; this first pass does **not** promise zero spikes. Existing F2 diagnostics still report `Grass.CreateChunk` etc., plus `Foliage.ProcessQueue`.

**What does not independently stream anymore when FoliageMaster.Enabled=true:** `StylizedGrassPlacement`, `PinePlacement`, `ForestPlacement` and `ContentScatter` enter passive/externally-managed mode. They use their current deterministic template loaders, exclusion masks, Terrain3D sampling, MultiMesh/LOD, wind, canopy and collision creation **only when the single manager asks for a chunk**. No separate player PositionChanged subscriptions exist for these four families. F1 recipe edits invalidate that family's chunks and requeue through the same manager. Fog changes adjust all native vegetation LODs and rock range and requeue only changed residency.

**What stays separate:** the existing disabled-by-default `TerrainGrass` GPU particle tile pool is a legacy rendering implementation, configured by the FoliageMaster's `GpuGrass` child recipe; it is not created by the CPU MultiMesh chunk queue. World WS0 128m logical sections, WS1 GPU culling and WS2 static building streaming are independent and unchanged. The actual terrain and building layout are not rebaked, and world objects/zombies are not foliage chunks.

**Fallback:** switch `FoliageMaster.tres → Enabled=false` to use the pre-existing autonomous per-species streaming paths for comparison; no artwork or terrain files are deleted in FG0. Do not remove the old implementations before local acceptance.

**Validation in Godot 4 .NET:** build C#, F6 FirstPersonWorld.tscn, observe near/far gradual foliage arrival, collision on rocks/trunks, forest zones and exclusions, grass001/002/ferns/shrubs, F1 live fog/foliage toggles and F2 timings. Compare same player location with fallback Enabled=false; layout and materials should match, but streaming order/timing differs. Verify chunk release/re-entry and that F2 sees no multi-family synchronized 181ms reconcile. This commit is **not a runtime benchmark**.

If the 2–4ms target still misses due to `Grass.CreateChunk` running atomically, the next measured PERF1 step is to split its dense patch work; don't add another scheduler.

**Resource compatibility:** FoliageSettings is a runtime [GlobalClass] resource, not an editor [Tool] script. Its child authored recipes include non-[Tool] custom resources; keeping the parent non-tool avoids the editor-time typed-resource cast failure recorded in LessonsLearned.md LL001.

## PERF1 — incremental grass chunk work (2026-11-01, source implemented; testing pending)

In managed mode, `FoliageManager` now resumes a partially generated grass chunk on later frames instead of requiring a full `Grass.CreateChunk` call. Existing seeds, candidate ordering, terrain sampling, grass001/002, optional billboard, shrubs/ferns and MultiMesh LOD recipes remain the same.

- Sampling yields every **8 placement attempts**; MultiMesh transform upload yields every **64 instances**. The manager's existing `FoliageMaster.WorkBudgetMs=3` is a **soft CPU deadline**. No new controller, threads, mesh assets or external dependencies.
- Completed batches remain **hidden** until all chunk families' grass/plant variants for that chunk are ready. Only then does the chunk become resident. Unfinished batches are discarded on chunk-centre changes, fog changes or F1 setting rebuilds; deterministic seeds recreate them identically.
- Managed F2 scope is now `Grass.BuildStep`, nested inside `Foliage.ProcessQueue`. The original `Grass.CreateChunk` scope remains available through `FoliageMaster.Enabled=false` fallback for comparison.
- A single Godot API call or deferred rendering/physics action may still cause a hitch; this is not a measured or guaranteed zero-stutter result. Local check: build Godot .NET, run FPS scene with F6, walk multiple 32m chunk edges with F2, verify visual density, grass/fern/shrub placement, fog changes, F1 disable/re-enable, unload/re-entry. Compare worst frame and operations with the prior 42ms screenshot.

## PERF1.1 — remaining foliage spikes (2026-11-01, source implemented, validation pending)

In managed mode, DF2 forest trees now yield to the **existing shared FoliageManager queue**, instead of constructing each forest chunk atomically. Each 4 tree/collider setup steps, each near/far MultiMesh batch and each 4 canopy fade targets yield; unfinished chunks remain invisible and their collision layer stays disabled until committed. Partial work is disposed on F1 rebuilds, terrain refresh, fog changes, chunk relocation and scene exit. The unmanaged fallback keeps its original synchronous behavior and final geometry/collisions.

Grass now yields every **4** terrain-placement attempts, every **16** transform uploads, checks the deadline before resuming and performs at most 8 steps per queue call (instead of 16). `Grass.Commit` separates final batch visibility from `Grass.BuildStep` in F2. Density, art, RNG sequence and Terrain3D are unchanged. Expect `Forest.BuildStep` (rather than `Forest.CreateChunk`) in F2 when unified foliage is enabled.

The work budget remains **soft**: one Godot native call, scene attachment, or rendering/physics synchronization may still exceed 3 ms. Build/test in Godot .NET; run F6 FPS world, walk and cross 32m boundaries with F2, verify trees/trunks/grass visuals, foliage fade and collision, turn fog/forest/grass off/on, release and return to a chunk, compare max step times and actual frame-time hitches with previous screenshots. Do not claim FPS gains before owner profiling.

## PERF1.2 rollback — 2026-11-01

The attempted bulk MultiMesh buffer upload and terrain-sample reuse in commit `ea7b91f` were reverted at the owner's request after an unsatisfactory laptop runtime test (slower FPS and foliage arriving visibly late). The prior PERF1.1 incremental grass/forest generation remains active. The existing foliage queue has a **soft 3 ms budget** and keeps incomplete grass chunks hidden until fully prepared; this can independently produce delayed pop-in on slower hardware. Do not interpret this rollback as proof that the bulk upload alone caused the delay. No model, seed, density, master setting, tree, rock or building-streaming changes were made in this rollback.
