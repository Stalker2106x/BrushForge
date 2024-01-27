extends CharacterBody3D

# Modifier keys' speed multiplier
const BASE_SPEED = 10
const SHIFT_SPEED = 4
const ALT_SPEED = 25
const MOUSE_SENSIVITY = 3

@export_range(0.0, 1.0) var sensitivity: float = 0.25

var camera;

# Mouse state
var _mouse_position = Vector2(0.0, 0.0)
var _total_pitch = 0.0

# Movement state
var direction = Vector3(0.0, 0.0, 0.0)
var computedVelocity = Vector3(0.0, 0.0, 0.0)
var _acceleration = 30
var _deceleration = -10
var _vel_multiplier = 4

func _input(event):
    # Only rotates mouse if the mouse is captured
    if Input.get_mouse_mode() != Input.MOUSE_MODE_CAPTURED:
        return;
    if event is InputEventMouseMotion:
        updateMouselook(event);
    
    # Receives mouse button input
    if event is InputEventMouseButton:
        match event.button_index:
            MOUSE_BUTTON_RIGHT: # Only allows rotation if right click down
                Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED if event.pressed else Input.MOUSE_MODE_VISIBLE)
            MOUSE_BUTTON_WHEEL_UP: # Increases max velocity
                _vel_multiplier = clamp(_vel_multiplier * 1.1, 0.2, 20)
            MOUSE_BUTTON_WHEEL_DOWN: # Decereases max velocity
                _vel_multiplier = clamp(_vel_multiplier / 1.1, 0.2, 20)

# Updates mouselook and movement every frame
func _process(delta):
    updateMovement(delta)

# Updates camera movement
func updateMovement(delta):
    # Computes desired direction from key states
    direction = Vector3(Input.get_axis("StrafeLeft", "StrafeRight"),
                             Input.get_axis("Down", "Up"),
                             Input.get_axis("Forward", "Backwards")).normalized();
    # Compute modifiers speed multiplier
    var speed_multi = BASE_SPEED
    if Input.is_action_pressed("Speed"):
        speed_multi = SHIFT_SPEED
    if Input.is_action_pressed("HighSpeed"):
        speed_multi = ALT_SPEED
    
    # Checks if we should bother translating the camera
    if direction == Vector3.ZERO:
        # Sets the computedVelocity to 0 to prevent jittering due to imperfect deceleration
        velocity = Vector3.ZERO
    else:
        velocity = direction * speed_multi;
    translate(velocity * delta);
    #move_and_slide();

# Updates mouse look 
func updateMouselook(event):
    rotation.y -= (event.relative.x / 1000) * MOUSE_SENSIVITY;
    rotation.x -= (event.relative.y / 1000) * MOUSE_SENSIVITY;
    rotation.x = clamp(rotation.x, PI/-2, PI/2)

func getRaycastHit():
    var space = get_world_3d().direct_space_state
    var query = PhysicsRayQueryParameters3D.create(global_position,
            global_position - global_transform.basis.z * 100)
    return space.intersect_ray(query);
