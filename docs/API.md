# Runtime API

## API stability

The Version 1 public surface is frozen around `WeatherController3D`, `WeatherProfile`, `WeatherZone3D`, and the five nodes in `weather_system_3d.tscn`. Public methods, signals, profile properties, state keys, and component node names were contract-validated during development.

Methods beginning with `_` and dynamically created particle/audio child nodes are implementation details.

## Controller methods

| Method | Purpose |
| --- | --- |
| `transition_to(profile, duration)` | Smoothly apply a profile. |
| `clear_weather(duration)` | Transition to a neutral clear state. |
| `set_intensity(value)` | Scale weather without replacing the profile. |
| `set_camera(camera)` | Switch camera-following weather to another camera at runtime. |
| `set_effect_target(target)` | Center local weather around a player or vehicle instead of the camera. |
| `get_effect_anchor_position()` | Read the resolved target-or-camera anchor position. |
| `set_wind(direction, strength)` | Override current horizontal wind. |
| `set_quality(value)` | Select Low, Medium, or High allocations. |
| `get_wind_velocity(time_offset)` | Sample current gust-aware wind. |
| `get_state()` | Read the unscaled interpolated state. |
| `get_effective_state()` | Read intensity- and quality-aware state. |
| `get_target_profile()` | Read the active transition or settled profile. |
| `trigger_lightning(strength, thunder_delay)` | Request a manual flash and thunder cue. |
| `is_volumetric_fog_available()` | Check whether the Forward+ enhancement is active. |

`request_zone()` and `release_zone()` are public for advanced custom zone implementations. Most projects should use `WeatherZone3D` directly.

## Profiles and transitions

```gdscript
weather.transition_to(profile, 3.0)
weather.clear_weather(2.0)

if weather.is_transitioning():
    print("Changing weather")
```

Calling `transition_to()` while a weather zone is active updates the global fallback profile without interrupting the active zone. The new global profile appears when all zones are released.

## Runtime overrides

```gdscript
weather.set_intensity(0.65)
weather.set_wind(Vector3(1.0, 0.0, 0.25), 8.0)
weather.set_quality(WeatherController3D.Quality.LOW)
```

`set_intensity()` scales precipitation, airborne particles, mist, fog, and ambient weather-audio levels. Values above `1.0` are accepted, although individual visual components may clamp their effective emission.

## Shared wind

```gdscript
var wind_velocity: Vector3 = weather.get_wind_velocity()
foliage_shader.set_shader_parameter("wind_vector", wind_velocity)
```

The sampled velocity includes the current profile's gust strength, gust frequency, and lateral turbulence. Supplying a `time_offset` lets different systems avoid perfectly synchronized motion:

```gdscript
var cloth_wind := weather.get_wind_velocity(0.8)
```

## Runtime camera switching

```gdscript
weather.set_camera(vehicle_camera)
```

The precipitation, atmosphere, splash, and wind-debug nodes follow the new controller camera automatically on the next frame.

Fixed, elevated, isometric, and cinematic cameras can keep their rendering camera while centering local effects on gameplay:

```gdscript
weather.set_effect_target(player)
```

Precipitation, mist, airborne particles, and ground splashes then follow the target's horizontal position. Clearing `effect_target` restores camera anchoring.

Use `effect_target_offset` for a world-space composition offset, such as centering effects ahead of a character near the lower edge of an isometric view. `AtmosphereParticles3D.max_camera_distance` controls how far mist and airborne billboards remain visible from elevated cameras.

## Forward+ volumetric fog

```gdscript
if weather.is_volumetric_fog_available():
    weather.enable_forward_plus_volumetric_fog = true
    weather.volumetric_fog_density_scale = 0.8
```

The optional volumetric layer follows the active profile's fog density and color. Compatibility/OpenGL continues to use traditional depth and height fog plus particle mist.

## Lightning and thunder

```gdscript
weather.trigger_lightning(0.9, 1.2)
```

Listen for delayed thunder cues without coupling the toolkit to a specific sound library:

```gdscript
func _ready() -> void:
    weather.thunder_requested.connect(_on_thunder_requested)

func _on_thunder_requested(delay_seconds: float, strength: float) -> void:
    await get_tree().create_timer(delay_seconds).timeout
    thunder_player.volume_db = lerpf(-12.0, 0.0, strength)
    thunder_player.play()
```

## Signals

- `transition_started(from_profile, to_profile, duration)`
- `transition_midpoint(profile)`
- `transition_completed(profile)`
- `state_changed(state)`
- `quality_changed(quality)`
- `lightning_requested(strength)`
- `thunder_requested(delay_seconds, strength)`

## Effective state

```gdscript
var state := weather.get_effective_state()
print(state.rain_intensity)
print(state.snow_intensity)
print(state.mist_intensity)
print(state.wind_speed)
```

Use `get_state()` for the unscaled interpolated profile state and `get_effective_state()` for intensity- and quality-aware values intended for runtime consumers.
