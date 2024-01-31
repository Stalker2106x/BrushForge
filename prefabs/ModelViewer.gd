extends SubViewportContainer


# Called when the node enters the scene tree for the first time.
func _ready():
    pass # Replace with function body.

func _input(event):
    if event is InputEventMouseMotion:
        var center = get_node("SubViewport/Center");
        center.rotate_x(-event.relative.y * 0.001);
        center.rotate_y(-event.relative.x * 0.001);
    if event is InputEventMouseButton:
        if event.button_index == MOUSE_BUTTON_WHEEL_UP:
            var camera = get_node("SubViewport/Center/Camera3D");
            camera.position.z -= 0.1;
        if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
            var camera = get_node("SubViewport/Center/Camera3D");
            camera.position.z += 0.1;
        

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    pass
