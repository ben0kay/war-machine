# StreetLampLighting — reusable local illumination

**Independent, scene-instantiable light component** at `StreetLampLighting.tscn`.

- Child `Downlight` is a Godot `SpotLight3D`, rotated -90° about X so its local -Z beam points down; default 75.6° half cone, 20m range, warm colour and 4.5 energy.
- Child `AmbientFill` is a Godot `OmniLight3D`, 12m range / 0.75 energy, always shadowless.
- The scene root has a `[Tool]` `StreetLampLighting.cs`, which configures the two lights from `Configuration/StreetLampLightingMaster.tres` once in `_Ready`.
- `SetEnabled(bool)` toggles native GPU lights **and** per-instance LED-panel emission. `BindLedPanel(StandardMaterial3D)` is an optional hook for the owning prop's physical LED panel; the lighting scene works even without an LED panel, e.g. in another floodlight fixture.
- This component is position-agnostic. The physical fixture instances it and places it at its own light-head coordinates. It knows no physical lamp heights, collision, world positions, convenience store or Terrain3D.
- No `_Process`, `_PhysicsProcess`, timers or per-frame checks, either on or off. Enabling real lights incurs native GPU work, and each lamp can contribute two sources. Keep spotlight shadows off unless deliberately needed; native light culling/ranges do not eliminate all render cost.

**Master ownership:** `ENVIRONMENT/Lighting/Configuration/LightingMaster.tres` remains the existing shared/global light defaults for muzzle effects; this specialized `Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres` is a *per-component preset*, not another global/world environment. No global values or Sky3D settings were modified by this refactor.

**Next:** PL0B assembles the convenience-store site; PL2 can add selected lamps on/off from the existing sky/night controller and one optional flickering lamp. Build/preview remains pending locally.

### Expanded street illumination — 2026-10-28

For the four lamps at ConvenienceStoreSite001, the shared default spotlight is brighter and wider: `SpotEnergy=4.5` (was 2.5), `SpotRange=20`m (was 13m), `SpotHalfAngle=63`° (was 51°). Shadowless ambient fill is `FillEnergy=0.75` (was 0.35), `FillRange=12`m (was 8m). The physical pole is also 7.0m (was 5.8m), and the existing head-position binding automatically lifts the light source with the head. Spot shadow casting stays disabled; no new light nodes, runtime loops, time-of-day control or fog changes. All four lamps share this master. Verify visual intensity/coverage in nighttime Godot and fine-tune the same `.tres` if necessary.

### Additional spotlight spread — 2026-10-28

The shared spotlight half-angle is now **75.6°**, exactly **20% wider in angle** than the previous 63° default. The Godot Inspector range and fixture-side clamp have both been raised to **85°** to avoid silently clipping this preset to the former 75° maximum. The existing 20m spotlight range and 4.5 energy, 7.0m pole and 0.75/12m ambient fill are unchanged; shadows remain disabled. The 20m light range still limits the extreme cone's actual floor reach, so cone-angle growth does not imply a 20% increase in ground radius. All convenience-store lamps inherit this master after reload. No extra light nodes or processing callbacks.

### Soft visible beam + scheduled streetlamps — 2026-10-28

**Source implemented, local Godot/.NET and visual/performance validation pending.**

`StreetLampLighting` now creates one optional `MeshInstance3D` named `VisibleLightCone` beneath the existing fixture head. It uses a shared, very small open `CylinderMesh` tapered into a cone, and `VisibleCone.gdshader` for a subtle unshaded warm-yellow translucent shaft, fading gradually at both ends and softening silhouettes. The beam is a visual approximation only: it does not cast light, shadow or collision and is not a new Sky3D fog volume. Existing `SpotLight3D`, `OmniLight3D`, fog energy and LED panel stay untouched. `SetEnabled(false)` immediately hides the beam as well as both native lights and LED emission. No per-lamp timers, _Process or _PhysicsProcess calls.

Edit `Configuration/StreetLampLightingMaster.tres` globally for all lamps using this shared preset:
- `VisibleConeEnabled=true`, `VisibleConeOpacity=0.035` (very faint), `VisibleConeLength=7.4` m, `VisibleConeBaseRadius=3.0` m.
- `LightColor=Color(1, 0.82, 0.61)` tints both the existing spotlight and the visual shaft.
- These values are independent of the existing real spotlight's much wider 75.6° half-angle; the visible cone is deliberately shorter/narrower to avoid a huge transparent surface.
- Shader is shared across lamps; mesh shape is shared and each fixture scales one instance. One additional transparent mesh per active lamp is still potential GPU overdraw. Test FPS when many beams overlap and lower opacity/radius or disable the effect if costly.

**All-lamp time schedule** belongs to `Configuration/StreetLampScheduleMaster.tres`, with `AutomaticTimeSwitching=true`, `TurnOnHour=17.25` (**17:15**), `TurnOffHour=6.5` (**06:30**). These are **in-game Sky3D clock hours**, not system wall-clock time. Set ON and OFF to the same time for all-day operation, or disable automatic switching to leave the per-fixture `LightsEnabled` setting in control. Active lamps switch OFF during the day, and ON at night; lights do not emit or draw beam when OFF.

One `StreetLampSchedule` instance is created by `GameEnvironment`. It listens to `SkyController.TimeChanged` (bridged from Sky3D's own `TimeOfDay.time_changed`) and the existing/recently spawned `StreetLampLighting` fixtures. Lamp nodes register/unregister via events and one fallback group scan at manager initialization. Every clock update is a small boolean comparison: GPU light/cone toggles occur **only** when ON/OFF changes, not 10 times a second. New prefab instances immediately inherit the current schedule. No global scene traversal on each time update, no per-lamp polling and no extra clock.

**Testing:** F6 `WORLD/World.tscn` and F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`; at default 09:00 (game's `GameplayEnvironmentMaster.tres` starts paused), all four convenience-store lamps should be off. Change the Sky3D time with existing debug controls to 19:00 and verify real spot/fill, LED and faint beam are on. Set 12:00: all off. Test 06:29→06:30 and 17:14→17:15, accelerated time (F8 if enabled by debug sky controller or existing UI time controls), spawned lamps during day/night, manual `SetLampEnabled` override expectations and shutdown. Note `StartPaused=true` means schedule won't transition automatically until the game clock is resumed or a manual time change occurs. `dotnet build war-machine.csproj`; inspect shader import for supported render modes; visually tune opacity and radius before acceptance.

### Ground-reaching beams and earlier evening switch (2026-10-28)

**Latest source settings (supersedes the initial 5.8m beam and 18:30 switch):** VisibleConeLength is **7.4m** as a standalone-fixture fallback. The actual StreetLamp001 pole has its light head at PoleHeight - 0.48m: for the default 7m pole this is **6.52m** above the fixture's base, previously leaving a 0.72m gap beneath the 5.8m cone. StreetLampMesh now calls `StreetLampLighting.FitVisibleConeToGround(headHeightAboveBase)` exactly once after positioning the light head. For streetlamp poles, the rendered cone dynamically uses **head height + VisibleConeGroundOverlap (0.85m)**, placing its open end beneath the pole base and letting ordinary terrain depth occlude the cut-off; this also works when authoring different pole heights. The fade shader, ~3m base radius, 0.035 opacity, light spread, physics and GPU lights are unchanged. The master fallback only applies when the reusable fixture is used outside a pole that calls FitVisibleConeToGround.

Global lamp schedule now switches ON at **17:15 (TurnOnHour=17.25)** and remains OFF at **06:30 (TurnOffHour=6.5)**, configured in `Configuration/StreetLampScheduleMaster.tres`. Switching remains Sky3D event-driven with no per-lamp polling. Test at 17:14 / 17:15 and 06:29 / 06:30, check ground contact from FPS and top-down, inspect sloped terrain, and confirm no visible floating ring. Build and visual verification pending locally.
