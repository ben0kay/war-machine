# StreetLamp001 — reusable parking-lot / roadside streetlight

**Status: standalone prefab with real lights implemented; awaiting Godot/.NET validation.** No site assembly or active-world placement is performed. `StreetLamp001.tscn` can be instanced by any site, independently of ParkingLot001 or ConvenienceStore001.

## Visual fixture and actual illumination

- A roughly **5.8m tapered metal pole** with footplate/pedestal, projecting **1.1m arm**, enclosed dark light housing and recessed luminous LED panel. Pole and footplate have static layer-1 collision; high arm/head meshes are decorative.
- One real `Lighting/Downlight` **SpotLight3D**, positioned just below the fixture and rotated **-90° about X** to aim straight down along world -Y. Defaults: warm `Color(1, 0.82, 0.61)`, intensity **2.5**, cone half-angle **51°**, range **13m**, attenuation **0.85**, adjustable angular softening. It casts a broad diffuse pool on asphalt, characters and other standard lit meshes.
- One `Lighting/AmbientFill` **OmniLight3D** at the same head position, **0.35 energy**, **8m range**. This adds subtle scattered illumination around nearby objects and ground without turning every lamp into an expensive shadow caster. It is **not** a substitute for real bounce lighting/GI.
- Spotlight shadows are **off by default** but can be enabled via `SpotShadows` for selected hero lights. Fill shadows are always off. Both have independently adjustable volumetric fog energy; real in-game haze response requires the renderer's fog configuration.
- The warm LED panel is slightly emissive and visible even in the absence of other scenery lighting. Its emissive state follows `SetLampEnabled`.

**One lamp contains up to two active GPU lights.** Godot automatically bounds light influence by configured range, but active native lights still cost GPU work even when the C# object is idle. For a future site with four lamps this means as many as eight local lights; consider lowering fill intensity/range, disabling fill for some lamps and only enabling a small number of shadow-casting spots. No performance measurement has been made.

## Configuration and lifecycle

`StreetLamp001.tscn` references `C/StreetLampMesh.cs` and `Configuration/StreetLampMaster.tres`. `StreetLampSettings : Resource` exports grouped fixture geometry, warm light colour, spot energy/range/cone/falloff, soft edge, shadows, fill enable/energy/range/falloff, and spot/fill fog intensity. The separate light nodes remain inspectable in the Godot scene tree.

The `[Tool]` model builder creates all art/collision only **once at Ready**, configures the two authored native lights then calls `SetProcess(false)` / `SetPhysicsProcess(false)`. `SetLampEnabled(bool)` is an event-driven hook for future day/night or activation; it toggles both visible native light nodes and the LED material, so OFF really disables lighting. No flicker, timer, distance polling, global environment manager, automatic terrain query, or continuous C# light updates exist.

**Time-of-day integration and the flickering lamp are still PL2 tasks.** The prefab defaults to lights ON for isolated night preview; it does not yet automatically turn off during daylight. The user can edit `LightsEnabled` in the master before instancing, or a future controller can call `SetLampEnabled(false)`. Avoid introducing another `WorldEnvironment` or coupling the prefab to Sky3D.

## Test in Godot

1. Pull the current development branch, compile C#, open `StreetLamp001.tscn` and inspect its tapered metal post, arm, light housing, emissive LED and distinct Spot/Omni nodes.
2. Instance it in a **temporary scene** above a dark plane/asphalt, set a night Sky3D hour or dark preview scene, and verify warm light pools downward, while fill adds softer background illumination. The lamp is **not** in `World.tscn` yet.
3. Check `SpotHalfAngle`, `SpotRange`, `SpotEnergy`, `FillEnergy` and `FillRange` in `Configuration/StreetLampMaster.tres` (re-instance after edits). Confirm `LightsEnabled=false` disables both light nodes; test `SpotShadows=true` only for one test lamp.
4. Compare four lamps in the scene and check FPS; adjust warm tint/fog and avoid costly overlapping shadow casters. Rendering, mesh appearance, physics, fog interaction and shader compatibility are **unverified in Godot**.

See [site PL plan](../../../../Sites/ConvenienceStoreSite001/IMPLEMENTATION_PLAN.md) for the remaining PL0B assembly, PL1 world placement and PL2 time-of-day/flicker tasks.
