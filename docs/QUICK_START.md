# Quick Start

## Install

Copy the complete `addons/weather_atmosphere/` folder into the `addons/` folder of a Godot 4.2-or-newer project.

This toolkit does not require an editor plugin to be enabled.

Open the project in the Godot editor once and let the initial filesystem scan finish before running the game or a headless build. Godot 4.2 registers the add-on's named GDScript classes during this first import; launching a newly copied project headlessly before that scan can produce temporary “type not found” parse errors.

## Explore the showcase

Run the packaged project to open the interactive showcase. The cursor starts free so the weather panel is immediately usable; click the 3D scene to enter mouse-look and press Escape to release the cursor again.

Choosing a preset applies it immediately with the selected transition duration. Use Q/E or Space to browse profiles, C to clear the weather, L to trigger lightning, V to show the wind arrow, F1 for the complete shortcut reference, and Tab to hide or restore the panel. The demo scales its interface down to the supported 960×540 compact window size.

## Add the system

1. Instance `addons/weather_atmosphere/weather_system_3d.tscn` in the gameplay scene.
2. Select the new `WeatherSystem3D` node.
3. Assign the active `Camera3D` to `camera`.
4. For fixed, elevated, isometric, or cinematic cameras, optionally assign the player or vehicle to `effect_target`.
5. Assign the scene's `WorldEnvironment` to `world_environment`.
6. Optionally assign a sun or moon `DirectionalLight3D` to `sun_light`.
7. Assign any included `WeatherProfile` to `initial_profile`.
8. Run the scene.

The system duplicates the assigned `Environment` resource on startup by default. This prevents runtime weather changes from modifying an environment resource shared by other scenes. Disable `duplicate_environment_on_ready` only when shared mutation is intentional.

For high fixed cameras, tune `effect_target_offset` to compose the local volume in front of the player and increase `AtmosphereParticles3D.max_camera_distance` only when the camera is farther than the default 45 meters.

## Change weather from code

```gdscript
@export var weather: WeatherController3D
@export var storm: WeatherProfile

func begin_storm() -> void:
    weather.transition_to(storm, 4.0)
```

## Choose a quality tier

```gdscript
weather.set_quality(WeatherController3D.Quality.MEDIUM)
```

Quality tiers change the allocated rain, snow, atmosphere, ripple, and splash particle counts. Runtime intensity transitions use emission ratios so visual changes do not repeatedly restart every emitter.

## Configure rain splashes

The included rain-splash component targets a flat ground plane. Select `RainSplash3D` under the system scene and set `ground_height` to the surface height. Disable the component for interiors, non-flat terrain, or scenes that provide their own collision-aware splash implementation.

At runtime:

```gdscript
var splash: WeatherRainSplash3D = weather.get_node("RainSplash3D")
splash.set_ground_height(player.global_position.y)
```

## Add a weather zone

1. Add an `Area3D` with the `WeatherZone3D` script.
2. Add a `CollisionShape3D` defining the region.
3. Assign the requested weather profile.
4. Set its weather priority and transition duration.
5. Add the player physics body to the `weather_target` group.

When zones overlap, the highest priority wins. If priorities match, the most recently entered zone wins. Leaving the final zone restores the current global weather.

## Debug wind

Enable `WindVisualizer3D` under `WeatherSystem3D`, or call:

```gdscript
var wind_debug: WeatherWindVisualizer3D = weather.get_node("WindVisualizer3D")
wind_debug.set_enabled(true)
```

The cyan runtime arrow shows the current gust- and turbulence-aware wind direction and relative strength. It is disabled by default and has no cost beyond its hidden node when unused.

## Renderer notes

Rain, snow, flat-ground splashes, particle mist, airborne particles, traditional fog, weather profiles, transitions, zones, and lightning are designed for Compatibility/OpenGL and Forward+/Vulkan.

On Forward+, `WeatherController3D` can add volumetric fog using the same profile fog density and color. It is enabled by default when `enable_forward_plus_volumetric_fog` is on. Use `volumetric_fog_density_scale` to tune the enhancement without changing the cross-renderer profile, or disable it to retain only traditional fog and particle mist.

Compatibility ignores this enhancement and retains the complete traditional-fog and particle-mist path. No profile changes are required when switching renderers.

## Add weather audio

Select `WeatherAudio` under `WeatherSystem3D` and assign looping rain and wind streams plus any number of thunder samples. Profile transitions drive the two ambience levels. Lightning emits a delayed thunder cue and the component chooses a thunder sample with subtle pitch variation.

Leaving the streams empty keeps the toolkit silent without warnings or missing-resource errors.
