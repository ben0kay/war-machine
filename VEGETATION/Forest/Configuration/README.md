# DF0 forest configuration

`ForestMaster.tres` is the **only authoritative dense-forest test-zone centre and size**, exposed as `WorldMaster.tres → DenseForest`. `WorldController` duplicates `WorldSettings` (including its nested forest settings) at startup; edits to the authoring resource take effect after restarting the scene.

- **Enabled=true as of DF2 preview activation:** on a successful DF0 survey, new canopy trees replace original pines in the test footprint. Set false and restart to restore original forest/rock placement.
- `Enabled=true` + valid terrain/reservations: the existing pines and random rocks are excluded from the 96 × 96m footprint and DF2 canopy trees are streamed in. On survey rejection the original pines remain.
- `ShowDebugOutline=true` (independent of Enabled) renders a one-shot bright green wire perimeter following Terrain3D. OFF removes it on restart; no collider or animated callback.
- `CenterXZ=(667,567)` is the survey-only east-side candidate; `SizeXZ=(96,96)`, `TransitionWidth=12`, `SurveyStep=8`, `MaximumSlope=0.75`. Validation also samples an approach from the village's east road.

The survey runs once at world initialization (only when Enabled or ShowDebugOutline). It refuses invalid/missing terrain, reserved rectangles and excessively steep samples; a rejected zone leaves the original pines alone. The location **has not passed local in-engine verification**. No grass/rock exclusions are changed in DF0; DF2/DF4 will decide their forest-specific blending. [DF phases](../FOREST_IMPLEMENTATION_PLAN.md).

## DF2 world population settings (source implemented)

The same ForestMaster resource now includes Seed, Mature/Young Spacing, Density, MinSeparation, scale ranges, TrailWidth, ChunkSize=32, RenderDistance=160, LodDistance=64, native range margin, near/far shadow toggles and group trunk collider controls. DF0 centre/size and approval remain the single authoritative ownership. Forest is now authored ON for testing; a scene restart and successful startup survey are still required. With an approved site, the old pines and random rocks are cleared only inside the zone, and the new trees render in streamed MultiMeshes. No per-tree scene scripts or permanently running renderer callbacks.
