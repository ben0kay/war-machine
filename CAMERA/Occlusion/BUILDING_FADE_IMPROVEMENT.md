# Building obstruction fade — future Codex task

**Status:** Proposed only. No code or config changed. Implement only when explicitly requested.

## Problem

When the player walks **outside, behind Building002 (the two-storey ranger lodge)**, the current whole-house fade still leaves the player hard to see through many overlapping translucent meshes (roof, walls, floors, props, trim). The current global camera opacity is **0.5** (50% visible per faded surface). Several overlapping surfaces can still strongly obscure the player.

## Confirmed current code

- `CAMERA/Configuration/CameraMaster.tres`: `ObstructedOpacity = 0.5` (global setting for camera obstruction), `ObstructionFadeSeconds = 0.2`. Defined in `CAMERA/CameraSettings.cs`.
- `CAMERA/Occlusion/CameraOcclusion.cs`: event-invalidated camera-to-player obstruction query; drives targets toward the global opacity and stops processing when fades finish.
- `CAMERA/Occlusion/CameraOccluder.cs`: individual mesh targets and composed opacity factors (building, floor presentation, camera); uses per-instance transparency.
- `WORLD/Content/Structures/Shared/BuildingFloorVisibility.cs`: Building002 outside-player, whole-house bounding-box provider; fades all house sections using `SetBuildingOpacity` when the player is outside; indoors retains floor- and wall-specific presentation.
- `WORLD/Content/Structures/Building002/Configuration/Building002Settings.cs` + `Building002Master.tres`: separate indoor `HiddenUpperOpacity` and `IndoorRoofOpacity`, but **no dedicated outside-house opacity setting**.

## Desired small improvement

1. **Add a dedicated `OutsideBuildingOpacity`** setting (suggested default **0.15–0.20**) to Building002's existing settings/master; 0 = invisible, 1 = opaque. Keep `CameraMaster.ObstructedOpacity` as the global default for trees, other scenery, and ordinary sectional obstruction.
2. When the player is **outside** and the whole-house obstruction test hits, fade the *entire lodge* to that much lower opacity. When no longer obstructing, restore the whole house smoothly.
3. During outside whole-house fading, avoid unnecessary **additional individual wall camera fades** acting on the same meshes. Treat the house as one coherent fade target outside. **Indoors**, preserve existing upper-floor, roof, stair, and individual-wall fade logic.
4. Review transparency **layer stacking** (several roof/wall/prop layers viewed through each other): a single per-mesh opacity value does **not** guarantee comparable final visibility through an entire multi-layer house. Test actual visibility of the player at typical angled camera views. If 0.15–0.20 alone is insufficient, consider a small capped **whole-house layer/coverage adjustment** rather than counting every mesh each frame. Prioritize a simple predictable visual result.
5. Preserve all collision, building geometry, stair gameplay, camera controls, renderer compatibility and existing non-lodge fades. No special combat or actor visibility logic.

## Performance / implementation constraints

- Reuse the existing `ICameraOcclusionProvider` / `ICameraFadeTarget` lifecycle and cached whole-building bounds; **no new per-frame raycasts, mesh scans, per-wall timers, global scene queries or idle polling**.
- Transition updates only while opacity changes, then sleep. Rebuild/re-register bounds only when needed; avoid allocating per obstruction check.
- Keep the new value Inspector-editable in the owning `Building002Master.tres` and honour deep-copied runtime settings; don't hardcode a second, competing opacity constant.
- Do not change Building001 or Building003 behaviour without a specific reason. The fuel station currently fades roof/canopy and selected walls, not its entire contents.

## Brief acceptance checks

- Outside, with the entire lodge between camera and player, the soldier remains **clearly visible**, including through overlapping walls and roof; changing `OutsideBuildingOpacity` noticeably changes that result.
- Moving clear restores house opacity cleanly; pausing while faded has **no ongoing per-frame fade work**.
- Entering the lodge retains the existing inside roof/upper-floor fading; stairs, upstairs/downstairs transitions and section-specific walls continue to work without pops.
- Trees and the petrol station retain their existing global occlusion behaviour.
- Build `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, and inspect the lodge from a few directions. Keep validation brief; report any visual testing deferred to the owner.

**Suggested implementation owner:** Codex / ChatGPT Work, after explicit authorization. Update this note and `CAMERA/Occlusion/README.md` when completed. One commit per authorized job.
