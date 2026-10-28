# Car001 — GLB static hatchback

**Current art:** `Artwork/Car001.glb` is the shared, imported **low-poly hatchback**, replacing the old collection of C# BoxMesh/CylinderMesh visuals. It has a sloped hood/windscreen, shaped roof/hatch, layered body panels, darker glass, inset lamps/grille, mirrors, wheels/rims, a minimal cabin and a separately hinged front-left driver door. About **2,028 triangles**, 12 colour materials and 10 grouped meshes; no texture dependencies or animation ticks.

**Compatibility:** `Car001.tscn` and its `Car001Mesh.cs` script remain the public prefab/API. All existing fuel-station, convenience-store and debug FPS placements continue to instance the same scene; there is **no world placement change**. The camera-occluder child still collects the generated GLB meshes after readiness. The car is a stationary prop, not a drivable vehicle.

**Dimensions/orientation:** approximately 4.35m long × 1.83m wide × 1.81m tall, front **-Z**, driver's side **-X**, wheels on ground at Y≈0. The GLB is authored at the master dimensions; the scene adapter scales the complete model and its simplified colliders to the configured length/width/roof height. `Car001Master.tres` still controls paint/glass colours.

**Door:** the GLB contains the named `DriverDoor` pivot (front-left), with the door visual as a child. `DriverDoorAngleOverride = -1` inherits the master default **closed (0°)**. Overrides of 0–82° and `SetDoorAngle(float)` rotate the imported door visual and attached collision together, **without modifying the shared master resource**. The remaining doors stay fixed; the model intentionally does not implement CAR-MOD optional/damaged variants yet.

**Collision/performance:** a few static box shapes cover the chassis, hood and roof plus a separate door collider. Decoration, windows, wheels and handles do not have per-mesh collision. The GLB mesh buffers are shared across instances; only palette material overrides and lightweight collision bodies are per instance. Neither the car nor door runs `_Process`/`_PhysicsProcess`.

**Files:** `Artwork/Car001.glb` = editable/exportable visual source, `C/Car001Mesh.cs` = tiny GLB/palette/collision/door adapter, `Configuration/Car001Settings.cs` and `Car001Master.tres` = defaults. See [MODULAR_VARIANTS_PLAN.md](MODULAR_VARIANTS_PLAN.md) for separately proposed independent wheels, doors, damage and removals.

**Local checks still needed:** import the GLB in Godot, build C#, preview `Car001.tscn` and `ConvenienceStoreSite001.tscn` from top-down/FPS, check wheel ground contact, shading and model scale. Override one driver's door to 58° and verify visible/collider rotation, then restore it. Ensure no missing resource errors and unchanged camera obstruction. No in-engine visual or physical test was performed here.
