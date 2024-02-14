extends SubViewportContainer

var rCamera;
var gizmo;

# Called when the node enters the scene tree for the first time.
func _ready():
    rCamera = get_node("/root/App/Layout/CenterLayout/Main/Views/3DView/World/Camera");
    gizmo = get_node("World/Gizmo");


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    gizmo.set_rotation(rCamera.get_rotation());
