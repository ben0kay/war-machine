# VEGETATION — GPU ribbon grass

`Grass/TerrainGrass.cs` adapts Terrain3D's included MIT particle grass into the actual `WORLD/testworld.tscn`. It uses the same ribbon mesh, noise-shaped clumps/patches, randomized spacing/orientation/height, terrain colour map, slope filtering, tip pinching and wind bending. World-owned shader copies leave the addon/demo untouched.

The old baked crossed-card mesh asset is **disabled**, not removed from saved region data. No rebake is required. Native terrain height/control/colour maps and authored grass data remain byte-for-byte unchanged; the new decorative GPU placement follows those maps and rejects holes, absent regions and painted cliff texture 0.

## Data-driven controls

- `Configuration/VegetationMaster.tres`: enabled, density, Low/Medium/High quality, hard particle budget, simulation FPS, shadows, render radius, tile width, fade start, minimum terrain-normal Y, blade height/width, clump/patch variation, colour and seed.
- `../ENVIRONMENT/Configuration/EnvironmentMaster.tres`: shared wind direction, grass wind strength/speed, plus Sky3D mist. Wind speed now uses the demo's noise-UV units: **0.025**, rather than the old sine-sway value 1.4. Strength defaults to the demo's 1.0.
- Density=1 / High uses the demo's 0.25m spacing; Medium multiplies density by 0.6, Low by 0.25 (and caps simulation at 20 FPS). Increasing radius/density cannot exceed the configured particle budget: spacing increases to fit it. Defaults: 60m fade radius, 24m tiles, 30 FPS simulation, shadows off, 49 tiles / 451,584 allocated particle slots. Filtered blades are not drawn, but slots still have GPU cost.
- Default **Height=0.6** scales the demo's original height distribution so tall noise clumps fit the exploration camera better. Height=1 restores its original scale. BladeColour stores the demo's linear RGB values; avoid adding a source-colour conversion to the shader uniform.

Edit master resources in the Inspector and restart the scene. For explicit runtime changes call `Vegetation.ApplySettings()`; there is no config polling. Enabled=false or Density=0 removes emitters and all grass GPU simulation; call ApplySettings again to reactivate. Solar pause intentionally does not stop grass/cloud wind.

## Lifecycle / performance

World initializes grass after selecting saved terrain and placing the explorer. Explorer transform signals are checked against the current tile coordinate: no callbacks run while the camera is stationary. Inside the same tile there are no grass uniform writes or emitter restarts. Crossing a tile boundary recycles only incoming rows/columns (seven tiles for one axis crossing with defaults); teleports wrap the same fixed pool. GPU ribbon fading continuously follows the actual camera, avoiding per-metre restarts.

Terrain map signals coalesce into one deferred binding upload/restart. Height/control/colour RIDs, region map and AABBs are **not** rewritten per frame. Bounds include terrain height range and configured blade/wind margin; normal Godot frustum culling applies. Zero wind skips the GPU wind-noise lookup; active grass still retains particle/rendering cost. Off/zero density has no emitters. Child resources and subscriptions clean up on world unload. This is localized decorative rendering, not persistent world streaming or vegetation gameplay.

See [validation results](Validation/README.md), including before/after/reference captures and the target-PC checks still required. All project gameplay glue is C#; the optional GDScript validation harness is diagnostic only.
