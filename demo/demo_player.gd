extends CharacterBody3D

@export_range(1.0, 20.0, 0.1) var move_speed := 5.5
@export_range(1.0, 3.0, 0.1) var sprint_multiplier := 1.8
@export_range(0.01, 1.0, 0.01) var mouse_sensitivity := 0.12
@export var capture_mouse_on_start := false

@onready var camera: Camera3D = $Camera3D

var _pitch := -17.0


func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED if capture_mouse_on_start else Input.MOUSE_MODE_VISIBLE
	camera.rotation_degrees.x = _pitch


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rotation_degrees.y -= event.relative.x * mouse_sensitivity
		_pitch = clampf(_pitch - event.relative.y * mouse_sensitivity, -75.0, 75.0)
		camera.rotation_degrees.x = _pitch
	elif event is InputEventMouseButton and event.pressed:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	elif event.is_action_pressed(&"ui_cancel"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE


func _physics_process(_delta: float) -> void:
	var input_vector := Input.get_vector(&"demo_left", &"demo_right", &"demo_forward", &"demo_back")
	var direction := (transform.basis * Vector3(input_vector.x, 0.0, input_vector.y)).normalized()
	var speed := move_speed * (sprint_multiplier if Input.is_action_pressed(&"demo_sprint") else 1.0)
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed
	velocity.y = 0.0
	move_and_slide()
