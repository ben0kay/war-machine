# DF0 forest configuration

`ForestMaster.tres` is the **only authoritative dense-forest test-zone centre and size**, exposed as `WorldMaster.tres → DenseForest`. `WorldController` duplicates `WorldSettings` (including its nested forest settings) at startup; edits to the authoring resource take effect after restarting the scene.

- `Enabled=false` by default: no forest-related pine masking or additional rendering.
- `Enabled=true` + valid terrain/reservations: the existing pines are **excluded** from the 96 × 96m footprint, ready for DF2 replacement. **No new canopy trees exist yet**, so the enabled test plot will look empty until DF1/DF2.
- `ShowDebugOutline=true` (independent of Enabled) renders a one-shot bright green wire perimeter following Terrain3D. OFF removes it on restart; no collider or animated callback.
- `CenterXZ=(667,567)` is the survey-only east-side candidate; `SizeXZ=(96,96)`, `TransitionWidth=12`, `SurveyStep=8`, `MaximumSlope=0.75`. Validation also samples an approach from the village's east road.

The survey runs once at world initialization (only when Enabled or ShowDebugOutline). It refuses invalid/missing terrain, reserved rectangles and excessively steep samples; a rejected zone leaves the original pines alone. The location **has not passed local in-engine verification**. No grass/rock exclusions are changed in DF0; DF2/DF4 will decide their forest-specific blending. [DF phases](../FOREST_IMPLEMENTATION_PLAN.md).
