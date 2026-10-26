extends "res://addons/weather_atmosphere/core/weather_controller_3d.gd"
## Project adapter: additive rain/snow controls without taking Sky3D's environment or sun.
func apply_preview(values: Dictionary) -> void:
	_transition_active = false
	_current_state = WeatherProfile.clear_state()
	_current_state.merge(values, true)
	_apply_runtime_state()
	set_process(false)
