extends CharacterBody3D

var sensitivity : float = 3
var base_speed : float = 500
var gravity = false;
var localVelocity = Vector3.ZERO;

func _input(event):        
	if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		if event is InputEventMouseMotion:
			rotation.y -= event.relative.x / 1000 * sensitivity
			rotation.x -= event.relative.y / 1000 * sensitivity
			rotation.x = clamp(rotation.x, PI/-2, PI/2)
	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_RIGHT:
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED if event.pressed else Input.MOUSE_MODE_VISIBLE)
	if event.is_action_pressed("Use"):
		var collision = getRaycastHit();
		if collision && collision.collider is Entity:
			collision.collider.use();

func getSpeed():
	if Input.is_action_pressed("Speed"):
		return base_speed * 5;
	elif Input.is_action_pressed("HighSpeed"):
		return base_speed * 10;
	else:
		return base_speed;

func _process(delta):        
	var forwardAxis = Input.get_axis("Forward", "Backwards");      
	var strafeAxis = Input.get_axis("StrafeLeft", "StrafeRight");      
	var verticalAxis = Input.get_axis("Down", "Up");
	var direction = (transform.basis * Vector3(strafeAxis, verticalAxis, forwardAxis)).normalized();
	localVelocity = direction * getSpeed();
	
	if gravity && !is_on_floor():
		localVelocity += Vector3(0, -ProjectSettings.get_setting("physics/3d/default_gravity"), 0);
	velocity = localVelocity;
	move_and_slide();
		
func getRaycastHit():
	var space = get_world_3d().direct_space_state
	var query = PhysicsRayQueryParameters3D.create(global_position,
			global_position - global_transform.basis.z * 100)
	query.collide_with_areas = true; # Need to collide with triggers
	query.set_collision_mask(3); # 1 & 2nd layers
	return space.intersect_ray(query);
