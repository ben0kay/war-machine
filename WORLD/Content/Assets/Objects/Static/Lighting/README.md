# Fixed reusable light fixture models

World content owns the **physical** objects (metal lamp poles, heads, collision, mounts) only. **All real lighting code, native Light3D nodes and illumination presets live in `ENVIRONMENT/Lighting/Fixtures/`**, and the physical fixtures instance those lighting scenes at their emitters.

[StreetLamp001](StreetLamp001/README.md) now instances `ENVIRONMENT/Lighting/Fixtures/StreetLamp/StreetLampLighting.tscn` and delegates its existing `SetLampEnabled(bool)` method to the light component. The standalone light scene controls downward spot, soft omni spill and LED emission. No site/world placement, flicker or day/night coupling is implemented yet.
