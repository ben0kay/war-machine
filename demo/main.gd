extends Node3D

@onready var weather: WeatherController3D = $WeatherController3D
@onready var control_panel: Control = $UI/ControlPanel
@onready var ui_visibility_hint: Control = $UI/UIVisibilityHint
@onready var status_label: Label = $UI/MarginContainer/VBoxContainer/Status
@onready var preset_picker: OptionButton = $UI/MarginContainer/VBoxContainer/PresetPicker
@onready var intensity_slider: HSlider = $UI/MarginContainer/VBoxContainer/IntensityRow/Intensity
@onready var transition_slider: HSlider = $UI/MarginContainer/VBoxContainer/TransitionRow/Transition
@onready var wind_slider: HSlider = $UI/MarginContainer/VBoxContainer/WindRow/Wind
@onready var quality_picker: OptionButton = $UI/MarginContainer/VBoxContainer/QualityPicker
@onready var intensity_value: Label = $UI/MarginContainer/VBoxContainer/IntensityRow/Value
@onready var transition_value: Label = $UI/MarginContainer/VBoxContainer/TransitionRow/Value
@onready var wind_value: Label = $UI/MarginContainer/VBoxContainer/WindRow/Value
@onready var renderer_info: Label = $UI/MarginContainer/VBoxContainer/RendererInfo
@onready var wind_debug: CheckButton = $UI/MarginContainer/VBoxContainer/WindDebug
@onready var diagnostics: Label = $UI/MarginContainer/VBoxContainer/Diagnostics
@onready var transition_status: Label = $UI/MarginContainer/VBoxContainer/TransitionStatus
@onready var transition_progress: ProgressBar = $UI/MarginContainer/VBoxContainer/TransitionProgress
@onready var help_details: Label = $UI/MarginContainer/VBoxContainer/HelpDetails
@onready var help_toggle: Button = $UI/MarginContainer/VBoxContainer/HelpToggle

var presets: Array[WeatherProfile] = [
	preload("res://addons/weather_atmosphere/presets/clear_calm.tres"),
	preload("res://addons/weather_atmosphere/presets/breezy_day.tres"),
	preload("res://addons/weather_atmosphere/presets/morning_mist.tres"),
	preload("res://addons/weather_atmosphere/presets/dense_fog.tres"),
	preload("res://addons/weather_atmosphere/presets/light_drizzle.tres"),
	preload("res://addons/weather_atmosphere/presets/steady_rain.tres"),
	preload("res://addons/weather_atmosphere/presets/heavy_rain.tres"),
	preload("res://addons/weather_atmosphere/presets/thunderstorm.tres"),
	preload("res://addons/weather_atmosphere/presets/gentle_snow.tres"),
	preload("res://addons/weather_atmosphere/presets/heavy_snow.tres"),
	preload("res://addons/weather_atmosphere/presets/blizzard.tres"),
	preload("res://addons/weather_atmosphere/presets/dust_storm.tres"),
]

var preset_index := 0
var _diagnostics_elapsed := 0.0
var _transition_elapsed := 0.0
var _transition_duration := 0.0
var _transition_target_name := ""
var _capture_mode := false


func _ready() -> void:
	get_viewport().size_changed.connect(_update_ui_scale)
	weather.state_changed.connect(_on_weather_state_changed)
	weather.transition_started.connect(_on_transition_started)
	weather.transition_completed.connect(_on_transition_completed)
	for profile: WeatherProfile in presets:
		preset_picker.add_item(profile.display_name)
	quality_picker.add_item("Low — 1,052 particles")
	quality_picker.add_item("Medium — 2,459 particles")
	quality_picker.add_item("High — 4,792 particles")
	quality_picker.select(2)
	preset_picker.item_selected.connect(_on_preset_selected)
	intensity_slider.value_changed.connect(_on_intensity_changed)
	transition_slider.value_changed.connect(_on_transition_changed)
	wind_slider.value_changed.connect(_on_wind_changed)
	quality_picker.item_selected.connect(_on_quality_selected)
	$UI/MarginContainer/VBoxContainer/ProfileActions/Previous.pressed.connect(_select_previous_profile)
	$UI/MarginContainer/VBoxContainer/ProfileActions/Apply.pressed.connect(_apply_selected_profile)
	$UI/MarginContainer/VBoxContainer/ProfileActions/Next.pressed.connect(_select_next_profile)
	$UI/MarginContainer/VBoxContainer/QuickActions/Clear.pressed.connect(_clear_weather)
	$UI/MarginContainer/VBoxContainer/QuickActions/Lightning.pressed.connect(_trigger_lightning)
	wind_debug.toggled.connect(_toggle_wind_debug)
	help_toggle.pressed.connect(_toggle_help)
	intensity_value.text = "%d%%" % roundi(intensity_slider.value * 100.0)
	transition_value.text = "%.2fs" % transition_slider.value
	wind_value.text = "%.1f m/s" % wind_slider.value
	renderer_info.text = "%s renderer  •  Volumetric fog %s" % [
		"Forward+" if weather.is_volumetric_fog_available() else "Compatibility",
		"available" if weather.is_volumetric_fog_available() else "uses fallback",
	]
	weather.transition_to(presets[preset_index], 0.0)
	_update_ui_scale()


func _process(delta: float) -> void:
	_update_transition_feedback(delta)
	_diagnostics_elapsed += delta
	if _diagnostics_elapsed < 0.25:
		return
	_diagnostics_elapsed = 0.0
	if _capture_mode:
		diagnostics.text = "Live FPS monitor  •  %d allocated particles  •  %s quality" % [
			_get_allocated_particle_count(),
			WeatherController3D.Quality.keys()[weather.quality].capitalize(),
		]
	else:
		diagnostics.text = "%d FPS  •  %d allocated particles  •  %s quality" % [
			Engine.get_frames_per_second(),
			_get_allocated_particle_count(),
			WeatherController3D.Quality.keys()[weather.quality].capitalize(),
		]


func _input(event: InputEvent) -> void:
	if not event is InputEventKey or not event.pressed or event.echo:
		return
	if event.keycode == KEY_TAB:
		_toggle_control_panel()
		get_viewport().set_input_as_handled()
	elif event.keycode == KEY_F1:
		_toggle_help()
		get_viewport().set_input_as_handled()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed(&"ui_accept"):
		_select_next_profile()
		get_viewport().set_input_as_handled()
		return
	if not event is InputEventKey or not event.pressed or event.echo:
		return
	match event.keycode:
		KEY_Q:
			_select_previous_profile()
		KEY_E:
			_select_next_profile()
		KEY_C:
			_clear_weather()
		KEY_L:
			_trigger_lightning()
		KEY_V:
			wind_debug.button_pressed = not wind_debug.button_pressed
		_:
			return
	get_viewport().set_input_as_handled()


func _on_preset_selected(index: int) -> void:
	preset_index = index
	_apply_selected_profile()


func _on_intensity_changed(value: float) -> void:
	intensity_value.text = "%d%%" % roundi(value * 100.0)
	weather.set_intensity(value)


func _on_transition_changed(value: float) -> void:
	transition_value.text = "%.2fs" % value


func _on_wind_changed(value: float) -> void:
	wind_value.text = "%.1f m/s" % value
	var direction: Vector3 = weather.get_state().get("wind_direction", Vector3.RIGHT)
	weather.set_wind(direction, value)


func _on_quality_selected(index: int) -> void:
	weather.set_quality(index)


func _apply_selected_profile() -> void:
	apply_profile(presets[preset_index], transition_slider.value)


func _select_previous_profile() -> void:
	preset_index = wrapi(preset_index - 1, 0, presets.size())
	preset_picker.select(preset_index)
	_apply_selected_profile()


func _select_next_profile() -> void:
	preset_index = (preset_index + 1) % presets.size()
	preset_picker.select(preset_index)
	_apply_selected_profile()


func _clear_weather() -> void:
	weather.clear_weather(transition_slider.value)


func _trigger_lightning() -> void:
	weather.trigger_lightning(1.0, 0.8)


func _toggle_wind_debug(value: bool) -> void:
	var visualizer := weather.get_node("WindVisualizer3D") as WeatherWindVisualizer3D
	visualizer.set_enabled(value)


func _toggle_help() -> void:
	help_details.visible = not help_details.visible
	help_toggle.text = "Hide shortcuts" if help_details.visible else "Show keyboard shortcuts"


func _toggle_control_panel() -> void:
	control_panel.visible = not control_panel.visible
	$UI/MarginContainer.visible = control_panel.visible
	ui_visibility_hint.visible = not control_panel.visible
	if not control_panel.visible:
		control_panel.release_focus()


func _update_ui_scale() -> void:
	var viewport_size := get_viewport().get_visible_rect().size
	var scale_factor := clampf(minf(viewport_size.x / 1280.0, viewport_size.y / 720.0), 0.75, 1.0)
	var ui_scale := Vector2.ONE * scale_factor
	control_panel.scale = ui_scale
	$UI/MarginContainer.scale = ui_scale
	ui_visibility_hint.scale = ui_scale


func apply_profile(profile: WeatherProfile, duration: float) -> void:
	var index := presets.find(profile)
	if index >= 0:
		preset_index = index
		preset_picker.select(index)
	weather.transition_to(profile, duration)


func set_showcase_ui_visible(visible: bool) -> void:
	$UI.visible = visible
	if visible:
		control_panel.visible = true
		ui_visibility_hint.visible = false


func prepare_capture(show_ui := true) -> void:
	_capture_mode = true
	var player := $Player as CharacterBody3D
	player.set_process_unhandled_input(false)
	player.set_physics_process(false)
	player.position = Vector3(10.0, 0.9, 12.0)
	player.rotation_degrees = Vector3(0.0, 40.0, 0.0)
	var capture_camera := player.get_node("Camera3D") as Camera3D
	capture_camera.rotation_degrees = Vector3(-9.0, 0.0, 0.0)
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	set_showcase_ui_visible(show_ui)


func _on_weather_state_changed(state: Dictionary) -> void:
	var target := weather.get_target_profile()
	var profile_name := target.display_name if target != null else "Clear"
	wind_slider.set_value_no_signal(float(state["wind_speed"]))
	wind_value.text = "%.1f m/s" % float(state["wind_speed"])
	var sheltered := profile_name == "Indoor Shelter"
	status_label.modulate = Color(0.74, 0.95, 0.8) if sheltered else Color.WHITE
	status_label.text = "%s   —   %s\nRain %d%%   Snow %d%%   Mist %d%%   Air %d%%   •   Wind %.1f m/s" % [
		"● SHELTER ZONE" if sheltered else "● OUTDOORS",
		profile_name,
		roundi(float(state["rain_intensity"]) * 100.0),
		roundi(float(state["snow_intensity"]) * 100.0),
		roundi(float(state["mist_intensity"]) * 100.0),
		roundi(float(state["airborne_intensity"]) * 100.0),
		state["wind_speed"],
	]


func _on_transition_started(_from_profile: WeatherProfile, to_profile: WeatherProfile, duration: float) -> void:
	_transition_elapsed = 0.0
	_transition_duration = duration
	_transition_target_name = to_profile.display_name
	transition_progress.value = 0.0
	transition_progress.visible = duration > 0.0
	transition_status.text = "Transitioning to %s" % _transition_target_name if duration > 0.0 else "Applied %s" % _transition_target_name


func _on_transition_completed(profile: WeatherProfile) -> void:
	transition_progress.value = 100.0
	transition_progress.visible = false
	transition_status.text = "Active profile  •  %s" % profile.display_name


func _update_transition_feedback(delta: float) -> void:
	if not weather.is_transitioning() or _transition_duration <= 0.0:
		return
	_transition_elapsed = minf(_transition_elapsed + delta, _transition_duration)
	var progress := _transition_elapsed / _transition_duration
	transition_progress.value = progress * 100.0
	transition_status.text = "Transitioning to %s  •  %d%%" % [_transition_target_name, roundi(progress * 100.0)]


func _get_allocated_particle_count() -> int:
	return (
		(weather.get_node("Precipitation3D") as WeatherPrecipitation3D).get_allocated_particle_count()
		+ (weather.get_node("AtmosphereParticles3D") as WeatherAtmosphereParticles3D).get_allocated_particle_count()
		+ (weather.get_node("RainSplash3D") as WeatherRainSplash3D).get_allocated_particle_count()
	)
