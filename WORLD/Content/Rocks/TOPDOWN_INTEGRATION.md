# Active World — Rocks integration for Codex

> **Status: integration NOT IMPLEMENTED.** The existing procedural rocks mesh generator was refined to supply **nine known, visually distinct silhouettes**. This note describes the next Codex placement task. Do not mistake the legacy `WORLD/testworld.tscn` rock scatter for active `WORLD/World.tscn` support.

## Current code to reuse

- `WORLD/Content/Rocks/C/RockGenerator.cs`: generates one low-poly closed mesh per variation index (0..8). Do **not** copy rock geometry into `WORLD/WorldController.cs`; `C/` is the procedural art generator.
- `WORLD/Content/Rocks/Configuration/RockMaster.tres`: category material and placement defaults. `Configuration/StoneMaterial.tres` and `Shaders/Stone.gdshader` provide CC0 stone triplanar textures, dampness/roughness and moss.
- `WORLD/Content/Shared/C/ContentScatter.cs`: legacy category-specific `MultiMeshInstance3D` distribution with nine shared rock meshes, 32m chunk batches, terrain height/slope sampling and event-driven updates. It currently accepts `ExplorationController` and `WorldConfig` and assumes a 0..Size world origin. This legacy dependence is why active `WORLD/World.tscn` does not yet have rocks.
- Active world code: `WORLD/WorldController.cs`, `WORLD/WorldSettings.cs`, `WORLD/World.tscn`. The current active player is `PlayerController`; FPS test inherits the active world scene. The current scene already has organized containers such as `WorldActorsRoot/PersistentPropsRoot/Objects` and `SystemsRoot`.

## Silhouettes and scale targets

The shared generator deliberately has these styles (RockMaster.Size=1.2 is the base):
1. Small loose stone (wide and low)
2. Medium fieldstone (rounded silhouette made of flat polygon faces)
3. Angular block (readable vertical facets)
4. Elongated ground boulder
5. Broken upright stone
6. Broad low slab
7. Taller jagged stone
8. Large chunky boulder
9. Wide irregular outcrop

Random yaw, deterministic seed and independent scale make more visual permutations without creating one procedural mesh per object. For a natural sparse countryside, mix these types with a bias to small/medium forms; reserve especially large outcrops for less frequent occurrences and slope edges. Terrain contact may embed their skirts a few centimetres to conceal seams.

## Codex work: add to active top-down world (also inherited FPS)

1. Read root `README.md`, `CodePreferences.md`, `WORLD/README.md`, this file and actual referenced sources at branch HEAD. **Don't rebake or modify saved Terrain3D data.**
2. Implement a small **active-world placement adapter** for rocks only that reuses `RockGenerator`, shared rock materials and category-batched `MultiMeshInstance3D`. Prefer reusing/refactoring common safe scatter routines, but **preserve `ContentScatter.Initialize(Node3D, ExplorationController, WorldConfig)` for legacy TestWorld**. Avoid a giant new world-content architecture for one prop.
3. Parent the runtime rock batch owner under `WorldActorsRoot/PersistentPropsRoot/Objects/Rocks` or a clearly documented chunk-owned `Rocks` container. Group by small terrain cells and update on player position/chunk crossings rather than every frame. Reuse rock seed/size/spacing settings in `RockMaster.tres`, or expose an explicitly named active-world override without mutating the authored master.
4. Use Terrain3D's loaded data `get_height` for placement with finite-value/slope checks. Respect active world bounds, player spawn area, zombie initial spawn points, robot patrol, building envelopes/entrances, fuel forecourt, open car door and pine exclusion clearings. Prefer some open ground and some interesting small rock clusters, not uniform grid spam.
5. Initially aim for a **sparse** landscape: a few isolated rocks, occasional groups of two or three, and **rare larger silhouettes**. Enable modest configurable density/spacing, render distance and seed in a master resource; keep world performance as the main priority. No trees, terrain edits or heavy physics decoration required.
6. **Collision is a separate decision:** existing multimesh-only scatter has **no collisions**. For truly large rocks that should physically block player, zombies and bullets, add a **capped, nearby-only set** of simple static box/convex colliders or standalone placed static boulder scenes. Smaller scenic rocks can remain decorative. Do not create hundreds of full-resolution triangle collision meshes. Use collision layer/mask compatible with player/zombie/rifle tests.
7. Don't change `Camera3D` projection, player movement or FPS movement. Because FPS test inherits `WORLD/World.tscn`, the exact same rocks should automatically appear in both modes. Avoid duplicated placement.
8. Verify briefly in Godot .NET: terrain grounding; variation at both camera angles; no rocks appearing inside buildings, blocking entryways or overlapping robot/spawn positions; active scene and inherited FPS load; FPS isn't noticeably worse at proposed density. Leave extensive profiling to the owner.

## Scope

Mesh generator and its materials already exist. This handoff is **for rock placement/collision adaptation only**, not new weapons, harvesting, destruction, mining, navigation meshes or interactive stone pickup. Update the owning README files, make one focused commit and push to `implement-td0-td1`.
