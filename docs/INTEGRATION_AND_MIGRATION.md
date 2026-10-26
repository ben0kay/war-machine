# Integration and Migration

## Add to an existing project

1. Back up or commit the project.
2. Copy the complete `addons/weather_atmosphere/` directory into the project's `addons/` directory.
3. Open the project in Godot once so global script classes and UID sidecars are imported.
4. Instance `weather_system_3d.tscn` in the gameplay scene.
5. Assign the active camera and `WorldEnvironment`; optionally assign a directional sun or moon light.
6. Choose an included profile and run the scene.

No editor plugin, autoload, project setting, or input action is required by the add-on. The packaged demo has its own movement actions, but those are not needed in a customer project.

## Environment ownership

By default, `WeatherController3D` duplicates the assigned `Environment` resource at startup. Weather changes therefore remain local to that `WorldEnvironment` instead of mutating a resource shared by several scenes.

If another system owns fog, ambient color, background energy, or directional-light styling, either disable `manage_environment` or coordinate updates through `state_changed`.

## Existing camera systems

Assign the gameplay camera in the Inspector. For projects that switch cameras at runtime:

```gdscript
weather.set_camera(active_camera)
```

All camera-following toolkit components adopt the new camera on the next frame.

For a fixed, elevated, isometric, or cinematic camera, keep `camera` assigned for billboard rendering but anchor local weather around gameplay:

```gdscript
weather.set_effect_target(player)
```

If `effect_target` is empty, the toolkit retains its camera-following default.

`effect_target_offset` is a world-space offset for composition-sensitive views. Mist and airborne particles remain visible up to `AtmosphereParticles3D.max_camera_distance`, which defaults to 45 meters for elevated-camera support.

## Updating the toolkit

1. Back up custom profiles and any edits made inside the add-on directory.
2. Replace the complete `addons/weather_atmosphere/` folder rather than mixing files from different versions.
3. Open Godot and allow the import scan to finish.
4. Run the project's scenes and review any warnings.
5. Reapply deliberate modifications as overrides or project-side scripts where practical.

The Version 1 public methods, signals, profile properties, state keys, and component node names are contract-tested. Files or methods beginning with `_`, generated particle children, and material internals are not public extension points.

## Removing the toolkit

Remove toolkit scene instances and script references first, then remove `addons/weather_atmosphere/`. Custom project resources that still use `WeatherProfile` must be removed or converted before deleting the scripts.
