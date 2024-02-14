extends CharacterBody3D

var sensitivity : float = 3
var base_speed : float = 5
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
                get_node("/root/App").view3D.setMouseCapture(event.pressed);
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

# Returns a dict with keys:
# collider: The colliding object.
# collider_id: The colliding object's ID.
# normal: The object's surface normal at the intersection point, or Vector3(0, 0, 0) if the ray starts inside the shape and PhysicsRayQueryParameters3D.hit_from_inside is true.
# position: The intersection point.
# face_index: The face index at the intersection point.
# Note: Returns a valid number only if the intersected shape is a ConcavePolygonShape3D. Otherwise, -1 is returned.
# rid: The intersecting object's RID.
# shape: The shape index of the colliding shape.
func getRaycastHit():
    var space = get_world_3d().direct_space_state
    var query = PhysicsRayQueryParameters3D.create(global_position,
            global_position - global_transform.basis.z * 100)
    query.set_collide_with_areas(true); # Need to collide with triggers
    query.set_collision_mask(3); # 1 & 2nd layers
    return space.intersect_ray(query);
