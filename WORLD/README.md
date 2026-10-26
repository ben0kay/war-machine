# WORLD — Integrated landscape

`testworld.tscn` is the playable environment foundation and project main scene. **F5** runs it; open it and press **F6** to run directly. Build with a Godot .NET editor and .NET 8 SDK first. No competing WorldRoot exists.

**Standalone orthographic camera test:** [TopDownWorldTest/IMPLEMENTATION_PLAN.md](TopDownWorldTest/IMPLEMENTATION_PLAN.md) — TD0–TD2 implemented, awaiting local validation: a separate Terrain3D landscape, low-poly walking player and orthographic follow camera. TD2 directly reuses the existing Robot_001 on a nearby preview patrol. Run the preview-only bake scene once locally, then F6 the preview; see its README. Does not replace TestWorld.

**Surface terrain roadmap:** [SURFACE_IMPLEMENTATION_PLAN.md](SURFACE_IMPLEMENTATION_PLAN.md) — optional SF0–SF5 work for erosion gullies, rock outcrops, scree, forest-edge ecology and one surface landmark. All phases remain Planned; separate from the WE environment and CV cave roadmaps.

- `Configuration/WorldMaster.tres` / `WorldConfig.cs`: seed, landscape bake settings, saved-data directory, spawn and grass placement.
- `Terrain/`: own Terrain3D material/assets, legacy grass resources, saved region data.
- `Generation/`: explicit, overwrite-protected offline C# bake scene. Gameplay never bakes.
- `TestWorld.cs`: selects saved terrain and safely positions the reusable `PLAYER/Explorer.tscn`.
- `../ENVIRONMENT/`: single Sky3D owner and master time/wind configuration.
- `Content/Vegetation/Trees/`: separate C# procedural pine preview, seeded Terrain3D height-based chunk placement and replaceable mesh assets. Config: `Content/Vegetation/Trees/Configuration/TreeMaster.tres`. No WE4 phase status change.

The active 2048 × 2048 metre world has a broad rolling meadow valley blending into mountain shoulders, using the main demo's grass/cliff autoshader and macro variation. The previous authored region and legacy grass remain untouched in Terrain/Data; active saved terrain lives in Terrain/PineLandscape without legacy cards. Visible grass is now the ribbon-based GPU particle adaptation under `VEGETATION/`, configured by its own master resource. No terrain rebake is needed. World art resources are owned here; unchanged CC0 texture files remain referenced from `demo/assets/textures` and must ship with the project.

Controls: **WASD / arrows** flight, **Ctrl** down, **RMB** up, **QE/Space** vertical aliases, **Shift** boost, **Esc** mouse capture, **P** solar pause, **F8** accelerated full-day preview. Flight intentionally has no collision and can pass through land; start is sampled above the ground. This is an environment inspection controller.

The authorized grass/mist refinement now uses Sky3D density 0.025, start 0m and end 200m; settings live in EnvironmentMaster. See `../VEGETATION/Validation/README.md` for the latest grass/mist checks.

WE0/WE1 implementation and automated checks are complete; both remain **Awaiting Validation** for local engine/visual/performance acceptance. See [validation](WE0_WE1_VALIDATION.md) and [phase plan](ENVIRONMENT_IMPLEMENTATION_PLAN.md). Later phases remain Planned.

The top-left FPS counter is configured in `../DEBUG/Configuration/DebugMaster.tres` and refreshes at 2Hz while visible. Tree canopy/shader/LOD fixes and controls validation are recorded in `Content/Vegetation/Trees/Validation/README.md`.

F9 opens the runtime environment tuning panel. See `../DEBUG/EnvironmentTuning/README.md`. Current pine/panel/terrain work awaits user visual and performance validation.

Decorative flowers, bushes, fallen logs and rocks now live in `Content/`, alongside relocated pines. All are integrated into TestWorld, with configuration resources and F9 controls. See `Content/README.md`.

F6/top-right button switches free flight and a first-person capsule (WASD/arrows, Shift sprint, Space jump). Terrain and cave have collision. A winding, height-varying, crystal-lit cave outcrop is near spawn; look 48m east / 28m north for the CAVE marker. See `Content/Caves/README.md`. This revision awaits local compilation/runtime validation.

Latest refinement: cave is now hidden beneath a runtime-shaped Terrain3D ridge, with a small entrance hole and irregular portal rocks. No saved regions are overwritten. Trees default to 1.5x height; rocks have nine varied forms and instance-oriented texture coordinates. F9 Weather uses the new weather_atmosphere addon for rain/snow, local mist/motes, gusts/turbulence, size/speed and quality, while Sky3D retains sky/time/fog ownership. See ENVIRONMENT/Weather/README.md and Content/Caves/README.md. Awaiting local compilation/runtime validation.

Entrance seam fix adds a solid rock collar joining the tunnel profile to surrounding land. Optional F9 Ground haze adds a low terrain-anchored volumetric layer, independent of Sky3D distance fog; off by default, Forward+ required. Source checks only; local validation pending.

## Robot encounter — RM4

`RobotEncounter.tscn` is instanced in TestWorld and initialized after existing terrain/content setup. Its world-owned script samples saved heights for four editable route markers in the spawn clearing; entity code stays in ENTITIES. The roughly 11m robot starts about 20m west / 6m north of spawn (ahead and left), walks a 20m × 14m loop, stops, scans with a cyan cone, recovers and resumes. See [Robot_001](../ENTITIES/TYPES/Robots/Robot_001/README.md). It does not edit terrain, player controls or environment settings. Placement checks terrain/slope at startup, with errors for unsafe paths rather than terrain regeneration.

Relevance uses existing explorer movement events (wake 180m / sleep 240m), plus the active patrol's own move event; no world polling callback. Explicit `RobotEncounter.SetEncounterEnabled` handles on/off. Console stage/target events are diagnostic only. Build/runtime, two-cycle visual acceptance and a second-instance smoke check remain pending; no Godot/.NET installation is available on this host. RM5 F9/audio/profiling is not implemented.

Latest cave/robot polish: cave stays in `Content/Caves`, with seeded FastNoiseLite width/ceiling/wall/ridge variation and a curved entrance apron sharing the exact tunnel rim, replacing the long collar. Robot F9 tuning and stronger cyan beam are implemented with optional quiet mechanical audio. No saved terrain files were written. Both changes await local build/render/audio acceptance; see their owning READMEs.

Cave extension/current entrance correction: main path 120m → shared-port junction → two descending 75m branches, with subtle subdivided floor noise. Ground-projected entrance folds are replaced by an ordered rim and consistent front faces. Pine trunks default to 2× with seeded 0.85–1.25 thickness variation, preserving foliage/branch sizes and matching LOD widths. Source/topology checks only; local build, entrance appearance and both branch traversal remain pending.

Current underground/haze refinement: noise-steered cave corridors descend 30m over the main route and 12m farther along each branch. Naturally buried routes no longer receive a generated covering ridge; only the shallow entrance support is shaped. Ground haze now defaults ON with animated close ground wisps (density 0.18, height 1.1m) and F9 scale/drift/contrast controls. Cave and fog are source-checked, awaiting local build and visual/traversal acceptance.

Current visual update: cave entrance uses a dedicated world-aligned hillside material sharing terrain rock/ground textures and larger cliff detail, while its interior stays unchanged. Robot_001 is now a cyan heavy biped with paired head-side sensor pods; the head scanner/patrol remain. Local build/render/gait acceptance pending.
