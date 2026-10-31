# DF0 forest configuration

`ForestMaster.tres` is the **only authoritative dense-forest test-zone centre and size**, exposed as `WorldMaster.tres → DenseForest`. `WorldController` duplicates `WorldSettings` (including its nested forest settings) at startup; edits to the authoring resource take effect after restarting the scene.

- **Enabled=true as of DF2 preview activation:** on a successful DF0 survey, new canopy trees replace original pines in the test footprint. Set false and restart to restore original forest/rock placement.
- `Enabled=true` + valid terrain/reservations: the existing pines and random rocks are excluded from the 96 × 96m footprint and DF2 canopy trees are streamed in. On survey rejection the original pines remain.
- `ShowDebugOutline=true` (independent of Enabled) renders a one-shot bright green wire perimeter following Terrain3D. OFF removes it on restart; no collider or animated callback.
- `CenterXZ=(667,567)` is the survey-only east-side candidate; `SizeXZ=(96,96)`, `TransitionWidth=12`, `SurveyStep=8`, `MaximumSlope=0.75`. Validation also samples an approach from the village's east road.

The survey runs once at world initialization (only when Enabled or ShowDebugOutline). It refuses invalid/missing terrain, reserved rectangles and excessively steep samples; a rejected zone leaves the original pines alone. The location **has not passed local in-engine verification**. No grass/rock exclusions are changed in DF0; DF2/DF4 will decide their forest-specific blending. [DF phases](../FOREST_IMPLEMENTATION_PLAN.md).

## DF2 world population settings (source implemented)

The same ForestMaster resource includes Seed, Mature/Young Spacing, Density, MinSeparation, scale ranges, TrailWidth, ChunkSize=32, near/far shadow toggles and group trunk collider controls. **No DF2-specific render/LOD distance remains**: GV1 draws near/far at the global 35% threshold and disappears at the fog-defined boundary. DF0 centre/size and approval remain the single authoritative ownership. Forest is now authored ON for testing; a scene restart and successful startup survey are still required. With an approved site, the old pines and random rocks are cleared only inside the zone, and the new trees render in streamed MultiMeshes. No per-tree scene scripts or permanently running renderer callbacks.

**White-tree diagnostic/safe mode (2026-10-31):** `UseSafePaletteMaterials=true` now defaults ON. This draws DF2 with Godot's regular `StandardMaterial3D` palette (green foliage, muted brown-grey bark) instead of the unverified custom foliage/bark shader path. Source mesh surfaces are explicitly assigned their material after commit, and Output prints a one-time `DF2 COLOR CHECK [SAFE]` line. F1 shared colour/strength controls also update the safe foliage material. This isolates the white appearance, but temporarily disables individual DF2 top-down canopy fading and per-face colour variation; set false and restart for the shader/fade path once the baseline has been visually accepted. World coordinates, density, LOD, trunk collisions and shadows remain unchanged.
