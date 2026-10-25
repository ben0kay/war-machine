# WORLD — Integrated landscape

`testworld.tscn` is the playable environment foundation and project main scene. **F5** runs it; open it and press **F6** to run directly. Build with a Godot .NET editor and .NET 8 SDK first. No competing WorldRoot exists.

- `Configuration/WorldMaster.tres` / `WorldConfig.cs`: seed, landscape bake settings, saved-data directory, spawn and grass placement.
- `Terrain/`: own Terrain3D material/assets, shared GPU grass shader, saved region data.
- `Generation/`: explicit, overwrite-protected offline C# bake scene. Gameplay never bakes.
- `TestWorld.cs`: selects saved terrain and safely positions the reusable `PLAYER/Explorer.tscn`.
- `../ENVIRONMENT/`: single Sky3D owner and master time/wind configuration.

The 1024 × 1024 metre world has a winding valley, ridges and noisy relief, using the main demo's grass/cliff autoshader and macro variation. It contains 46,845 saved grass clumps, rendered with Terrain3D instancing and a shared wind shader. World art resources are owned here; unchanged CC0 texture files remain referenced from `demo/assets/textures` and must ship with the project.

Controls: **WASD** flight, **QE/Space** vertical, **Shift** boost, **Esc** mouse capture, **P** solar pause, **F8** accelerated full-day preview. Flight intentionally has no collision and can pass through land; start is sampled above the ground. This is an environment inspection controller.

WE0/WE1 implementation and automated checks are complete; both remain **Awaiting Validation** for local engine/visual/performance acceptance. See [validation](WE0_WE1_VALIDATION.md) and [phase plan](ENVIRONMENT_IMPLEMENTATION_PLAN.md). Later phases remain Planned.
