extends SubViewportContainer

var clicked;

# Called when the node enters the scene tree for the first time.
func _ready():
    connect("gui_input", Callable(self, "input"));

func input(event):
    if clicked && event is InputEventMouseMotion:
        var model = get_node("SubViewport/Container").get_child(0);
        model.rotate_y(event.relative.x * 0.001);
        model.rotate_z(-event.relative.y * 0.001);
    if event is InputEventMouseButton:
        if event.button_index == MOUSE_BUTTON_LEFT:
            clicked = event.pressed;
        if event.button_index == MOUSE_BUTTON_WHEEL_UP:
            var camera = get_node("SubViewport/Camera3D");
            camera.position.z -= 0.1;
        if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
            var camera = get_node("SubViewport/Camera3D");
            camera.position.z += 0.1;
