# WORLD — Integrated landscape

`testworld.tscn` is the playable environment foundation and project main scene. **F5** runs it; open it and press **F6** to run directly. Build with a Godot .NET editor and .NET 8 SDK first. No competing WorldRoot exists.

- `Configuration/WorldMaster.tres` / `WorldConfig.cs`: seed, landscape bake settings, saved-data directory, spawn and grass placement.
- `Terrain/`: own Terrain3D material/assets, legacy grass resources, saved region data.
- `Generation/`: explicit, overwrite-protected offline C# bake scene. Gameplay never bakes.
- `TestWorld.cs`: selects saved terrain and safely positions the reusable `PLAYER/Explorer.tscn`.
- `../ENVIRONMENT/`: single Sky3D owner and master time/wind configuration.
- `Vegetation/Trees/`: separate C# procedural oak preview, seeded Terrain3D height-based chunk placement and replaceable mesh assets. Config: `Vegetation/Trees/Configuration/TreeMaster.tres`. No WE4 phase status change.

The 1024 × 1024 metre world has a winding valley, ridges and noisy relief, using the main demo's grass/cliff autoshader and macro variation. It retains 46,845 legacy saved grass clumps for authoring compatibility, with their crossed-card asset disabled. Visible grass is now the ribbon-based GPU particle adaptation under `VEGETATION/`, configured by its own master resource. No terrain rebake is needed. World art resources are owned here; unchanged CC0 texture files remain referenced from `demo/assets/textures` and must ship with the project.

Controls: **WASD** flight, **QE/Space** vertical, **Shift** boost, **Esc** mouse capture, **P** solar pause, **F8** accelerated full-day preview. Flight intentionally has no collision and can pass through land; start is sampled above the ground. This is an environment inspection controller.

The authorized grass/mist refinement now uses Sky3D density 0.025, start 0m and end 200m; settings live in EnvironmentMaster. See `../VEGETATION/Validation/README.md` for the latest grass/mist checks.

WE0/WE1 implementation and automated checks are complete; both remain **Awaiting Validation** for local engine/visual/performance acceptance. See [validation](WE0_WE1_VALIDATION.md) and [phase plan](ENVIRONMENT_IMPLEMENTATION_PLAN.md). Later phases remain Planned.
