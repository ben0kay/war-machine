# Troubleshooting

## “Could not find type WeatherProfile” after copying the add-on

Open the project in the Godot editor and wait for the initial filesystem scan to complete. Godot 4.2 needs this first import to register the add-on's `class_name` scripts before a headless run. If the editor was already open while files were copied, rescan the filesystem or restart the editor once.

## Particles are missing

- Confirm the controller has an active `Camera3D` reference.
- Confirm the profile has a non-zero precipitation, mist, or airborne intensity.
- Check that global intensity is above zero.
- Wait for Godot's first import scan to finish after copying the add-on.
- Test visual particles with a real renderer. Headless Godot can use a dummy renderer and is intended for logic tests, not visual review.

## Particles disappear at unusual angles

The included emitters use explicit visibility bounds sized for their default camera-follow regions. If emitter dimensions are heavily increased in custom code, increase the corresponding `visibility_aabb` as well.

Transparent particles can also expose depth-sorting limitations when placed through dense transparent third-party materials. Test foliage, windows, and custom fog planes together in the target scene.

## Rain ripples float or clip

`RainSplash3D` assumes a flat surface. Set `ground_height` to the target plane:

```gdscript
var splashes: WeatherRainSplash3D = weather.get_node("RainSplash3D")
splashes.set_ground_height(ground_y + 0.02)
```

Disable this component for uneven terrain or replace it with project-specific collision-aware impacts.

## Fog conflicts with another system

`WeatherController3D` writes fog, ambient tint, background energy, and optional volumetric properties when `manage_environment` is enabled. Disable that option if another system owns the environment and consume `state_changed` in project code instead.

The controller duplicates the assigned Environment by default. If inspecting the original resource shows no runtime changes, inspect the `WorldEnvironment.environment` instance instead.

## Volumetric fog is unavailable

Volumetric fog requires Forward+. Compatibility/OpenGL intentionally uses traditional fog plus particle mist. Check `is_volumetric_fog_available()` before showing Forward+-specific settings.

## A weather zone does not trigger

- The entering object must be a physics body detected by the `Area3D`.
- Add the player body to the zone's `target_group`, which defaults to `weather_target`.
- Verify collision layers and masks overlap.
- Assign both a profile and controller.
- When zones overlap, the highest `weather_priority` wins.

## Switching cameras leaves weather behind

Use `weather.set_camera(new_camera)`. Assigning unrelated component camera fields is unnecessary when using the included drop-in scene.

## Performance is lower than expected

- Switch to Medium or Low quality and confirm the allocation count changes.
- Disable rain splashes if they are not visible.
- Reduce mist or airborne intensity before reducing rain readability.
- Avoid stacking another full-screen fog or particle-weather system over the toolkit.
- Compare Compatibility and Forward+ in the complete project; the best renderer depends on the rest of the scene.

See `COMPATIBILITY_AND_PERFORMANCE.md` for exact default allocation budgets.

## Custom resource InvalidCastException in editor previews

ParkingLotMesh, ConvenienceStoreMesh, Car001Mesh, the four Building00x mesh builders and streetlamp preview/lighting scripts run with [Tool]. Their exported concrete C# Resource types (and shared StructureSettings base) must also be marked [Tool] as well as [GlobalClass], otherwise the editor can supply a native Godot.Resource placeholder and the generated property setter fails to cast. These are data-only resources; this annotation adds no update loop, physics or regeneration. Resource paths and existing .tres values are preserved.

After pulling the fix on implement-td0-td1, stop play, rebuild the C# project and reopen the editor if it still holds cached script instances. Clear Output and F6 WORLD/World.tscn. Expected current startup includes Village survey / Village graded messages. Old mountain-test and showcase building-offset messages in retained Output can belong to a previous run; confirm against a fresh run before diagnosing them as new village failures. Do not edit generated .godot/mono source files or recreate the settings masters.

Source/attribute dependency and whitespace checks passed. Godot/.NET unavailable here: compilation/editor reload/runtime verification remains local.
