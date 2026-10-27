# Active-world ownership cleanup

| Phase | Goal | Assistant | Status |
| --- | --- | --- | --- |
| OR0 | Relocate prototype into owning systems; split settings and mesh code; preserve legacy scene | Codex / ChatGPT Work | Implemented, Awaiting Local Validation |

Authorized structure: CAMERA/Camera.cs directly at the root camera folder; C/ only for procedural mesh construction, Artwork/ for future imported models. Active player/controller/settings/scene, standalone camera scene/config, world composition/placement/bake, debug controls and environment settings now have owning folders. Non-mesh shared entity, zombie, robot, player-equipment and firearm scripts were moved out of C/. Player and zombie mesh builders were extracted without changing their rig transforms or animation formulas. Older parked content/cave folder conventions are retained for a later dedicated cleanup.

Separate active resources replace the mixed TopDownTestSettings: PlayerSettings, CameraSettings, WorldSettings and GameplayEnvironmentMaster. Legacy player assets move together under PLAYER/LegacyFirstPerson; old WorldMaster becomes LegacyWorldMaster. Existing resource values, rifle equip/aim/damage, zombie HP/behaviour, robot route, sky/weather, arrows/crouch and projection controls are preserved. F5/default scene and native addon sources are unchanged.

Native prototype data is user-local rather than tracked. GameplayTerrainStorage copies the old bake to WORLD/Terrain/Gameplay only if the destination is empty, keeps the old files, and the world assigns its new data directory after copying. No normal-world terrain edits or automatic generation. Historical TD docs live in Archive/; the former test folder contains no active tracked code.

Performance: file/ownership changes introduce no new frame callbacks. Existing event-driven wake/sleep, active motion and low-rate enemy decisions remain. Migration checks/copies happen only at initialization or explicit bake. More general streaming/navigation/model import are out of scope.

Validation: source settings ownership, resource/scene references, inherited entity paths and script UID moves, scene load counts, old-path checks and git whitespace review. **Godot/.NET unavailable here: compile, scene load, migration and visuals not passed.** Local handoff: build project; F6 WORLD/World.tscn with an old bake and confirm terrain loads/no rebake; verify player arrows/Shift/crouch, rifle aim/fire/95 damage and zombie 100 HP, both camera projections/copy settings, weather/time, robot patrol and zombie pursuit. F5 must still open the parked FPS scene. Restart with migrated data and confirm originals persist. No extended test suite/profiling performed.
