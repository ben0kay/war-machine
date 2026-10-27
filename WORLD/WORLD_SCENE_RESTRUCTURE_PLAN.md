# War Machine — World scene root hierarchy restructuring plan

**Status: Planned only.** For Codex on branch `implement-td0-td1`. No scene/node changes in this documentation commit.

## Goal

Mirror the root-container organization from `ben0kay/isometric-fantasy-2d/SCENES/World/World.tscn`. The 3D world keeps one actual `World` root, with ordinary child container nodes. Make authored props visible in the Godot **Local** scene tree and runtime objects predictable in **Remote**, without changing their reusable prefab code. This is not an FPS optimization and does not require new managers.

## Target structure

```
World (Node3D, WorldController)
├── TerrainRoot (Node3D)
│   └── Terrain3D
├── WorldActorsRoot (Node3D)
│   ├── PersistentEntitiesRoot (Node3D)
│   │   ├── Player
│   │   ├── RobotPatrol (existing route markers, Robot_001)
│   │   └── Zombies (dynamic)
│   ├── PersistentPropsRoot (Node3D)
│   │   ├── Structures (authored Building001)
│   │   └── Objects/Cars (optional authored Car001)
│   ├── ChunkActorsRoot (future streamed content)
│   └── PreviewSceneryRoot (optional)
├── SystemsRoot (Node)
│   ├── VegetationRoot (active pines / grass)
│   └── EnvironmentRoot (current sky/weather)
├── Camera
├── DebugUIRoot
│   └── Controls/Status
└── GameplayUIRoot (optional; only if populated)
```

The exact nested categories are adjustable. Only create containers needed for current systems; don't generate managers or idle callbacks simply to organize. Preserve the camera's world transform and Terrain3D's resources.

## Work phases

| Phase | Task | Status |
| --- | --- | --- |
| SO0 | Inspect current code and scene paths; identify all direct-child assumptions, runtime `AddChild` calls, existing prop placement and foliage clearings. Write a safe reparent map. | Planned |
| SO1 | Add essential authored root containers in `WORLD/World.tscn`. Move Terrain3D, Player, RobotPatrol and UI beneath suitable containers while preserving relative marker paths and global transforms; update `WorldController` GetNode paths and other references. | Planned |
| SO2 | Direct dynamically created Zombies, sky/environment, grass/pines and scenery into stable container nodes, preserving setup sequence, camera occlusion, collision and cleanup. Avoid duplicate runtime nodes. | Planned |
| SO3 | Make **manually placed** buildings/cars selectable as actual PackedScene instances beneath PersistentPropsRoot in the **Local** editor tree. Stop duplicating them in runtime placement code; preserve terrain grounding, authored transforms and pine-exclusion areas. Do not automatically place Building002/003. | Planned |
| SO4 | Update README and do brief Godot .NET build/F6 smoke test, including robot patrol, zombie spawns, player/camera, terrain, weather, trees, placed props and scene Local/Remote inspection. | Planned |

## Critical implementation details

- `WORLD/WorldController.cs` currently creates `Structures`, `Zombies`, grass/pines, sky/weather at runtime. `World.tscn` currently authors Terrain3D, Player, Camera, Controls/Status and RobotPatrol at the root. Building and car positioning comes from `PlaceScenery` and terrain height sampling.
- The entire RobotPatrol subtree must move together. Preserve `Robot` and `RouteMarkers` NodePaths; change the WorldController lookup to the new path.
- Update `Terrain3D`, `Player`, `Camera`, `Controls/Status` references carefully. Keep Terrain3D `set_camera`, terrain native data directory and vegetation terrain/camera links intact. Never rebake.
- Existing building and car `.tscn` prefabs stay independently usable in FPS and top-down. Their procedural `C/` mesh code and static collision remain unchanged.
- CameraOccluder still belongs to scenery; don't mix occlusion camera logic into every building. No 2D YSort concepts in 3D.
- If converting code-instanced Building001/Car001 to editor-authored instances, ensure the terrain-height placement and grass/tree exclusion are still calculated correctly and objects are not spawned twice. Authored transforms should remain adjustable in inspector.
- A hierarchy container does not cause streaming or improve FPS by itself. Avoid adding new update loops; ordinary Node3D containers are lightweight.
- Only run brief testing per CodePreferences; don't do exhaustive tests unless requested. One commit per approved stage, push to `implement-td0-td1`.

## Codex instructions

Read root `README.md`, `CodePreferences.md`, `WORLD/README.md`, `WORLD/World.tscn`, `WORLD/WorldController.cs`, `WORLD/Encounters/RobotPlacement.cs`, and `SCENES/World/World.tscn` in the **isometric-fantasy-2d** repo.

Implement **SO0–SO1 first** when specifically requested; do not execute all phases simply because this plan exists. Keep current game behaviour, FPS world and all reusable props intact.
