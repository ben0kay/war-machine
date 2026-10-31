extends "res://addons/weather_atmosphere/components/rain_splash_3d.gd"
## Project-owned style for the addon's pooled ripple/droplet effects.
## Change mesh sizes only when rain intensity changes; GPU particle randomness varies each impact.

@export_range(0.5, 1.5, 0.05) var splash_scale := 1.0


# =========================================================
func _apply_state(state: Dictionary) -> void:
	super._apply_state(state)
	if _ripples == null or _droplets == null:
		return
	var rain: float = clampf(float(state.get("rain_intensity", 0.0)), 0.0, 1.0)
	var ring_size: float = splash_scale * lerpf(0.84, 1.20, rain)
	var ring := _ripples.draw_pass_1 as QuadMesh
	if ring != null:
		ring.size = Vector2(0.38, 0.38) * ring_size
	var spray := _droplets.draw_pass_1 as QuadMesh
	if spray != null:
		spray.size = Vector2(0.014, 0.105) * splash_scale * lerpf(0.90, 1.12, rain)
