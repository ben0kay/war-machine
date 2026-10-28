# StreetLampLighting — reusable local illumination

**Independent, scene-instantiable light component** at `StreetLampLighting.tscn`.

- Child `Downlight` is a Godot `SpotLight3D`, rotated -90° about X so its local -Z beam points down; default 63° half cone, 20m range, warm colour and 4.5 energy.
- Child `AmbientFill` is a Godot `OmniLight3D`, 12m range / 0.75 energy, always shadowless.
- The scene root has a `[Tool]` `StreetLampLighting.cs`, which configures the two lights from `Configuration/StreetLampLightingMaster.tres` once in `_Ready`.
- `SetEnabled(bool)` toggles native GPU lights **and** per-instance LED-panel emission. `BindLedPanel(StandardMaterial3D)` is an optional hook for the owning prop's physical LED panel; the lighting scene works even without an LED panel, e.g. in another floodlight fixture.
- This component is position-agnostic. The physical fixture instances it and places it at its own light-head coordinates. It knows no physical lamp heights, collision, world positions, convenience store or Terrain3D.
- No `_Process`, `_PhysicsProcess`, timers or per-frame checks, either on or off. Enabling real lights incurs native GPU work, and each lamp can contribute two sources. Keep spotlight shadows off unless deliberately needed; native light culling/ranges do not eliminate all render cost.

**Master ownership:** `ENVIRONMENT/Lighting/Configuration/LightingMaster.tres` remains the existing shared/global light defaults for muzzle effects; this specialized `Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres` is a *per-component preset*, not another global/world environment. No global values or Sky3D settings were modified by this refactor.

**Next:** PL0B assembles the convenience-store site; PL2 can add selected lamps on/off from the existing sky/night controller and one optional flickering lamp. Build/preview remains pending locally.

### Expanded street illumination — 2026-10-28

For the four lamps at ConvenienceStoreSite001, the shared default spotlight is brighter and wider: `SpotEnergy=4.5` (was 2.5), `SpotRange=20`m (was 13m), `SpotHalfAngle=63`° (was 51°). Shadowless ambient fill is `FillEnergy=0.75` (was 0.35), `FillRange=12`m (was 8m). The physical pole is also 7.0m (was 5.8m), and the existing head-position binding automatically lifts the light source with the head. Spot shadow casting stays disabled; no new light nodes, runtime loops, time-of-day control or fog changes. All four lamps share this master. Verify visual intensity/coverage in nighttime Godot and fine-tune the same `.tres` if necessary.
