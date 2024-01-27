extends Node3D
class_name Beam

var enabled;

var targetName;
var targetPath;

# Called when the node enters the scene tree for the first time.
func _ready():
    enabled = false;

func configure(origin : Vector3, targetName_ : String):
    set_position(origin);
    targetName = targetName_;
    targetPath = get_node("/root/App/Layout/CenterLayout/Main/3DView/Viewport/World/Container/Map/Paths/%s/Wagon" % targetName);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if enabled && targetPath:
        line(get_position(), targetPath.get_position());

func line(pos1: Vector3, pos2: Vector3, color = Color.RED):
    var mesh_instance := MeshInstance3D.new()
    var immediate_mesh := ImmediateMesh.new()
    var material := ORMMaterial3D.new()

    mesh_instance.mesh = immediate_mesh
    mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

    immediate_mesh.surface_begin(Mesh.PRIMITIVE_LINES, material);
    immediate_mesh.surface_add_vertex(pos1);
    immediate_mesh.surface_add_vertex(pos2);
    immediate_mesh.surface_end();

    material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED;
    material.albedo_color = color;

    get_tree().get_root().add_child(mesh_instance);
    await get_tree().physics_frame;
    mesh_instance.queue_free();
