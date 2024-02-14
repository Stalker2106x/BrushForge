extends Control

const FaceSelectedMaterial = preload("res://materials/FaceSelectedMaterial.tres");
const NoTexture = preload("res://assets/Missing.png");

var texturePreview;
var identifierLabel;
var dataLabel;
var actionButton;

var overlayMesh;

# Called when the node enters the scene tree for the first time.
func _ready():
    overlayMesh = MeshInstance3D.new();
    var world = get_node("/root/App/Layout/CenterLayout/Main/Views/3DView/World");
    world.add_child(overlayMesh);
    texturePreview = get_node("ScrollContainer/Layout/TexturePreview");
    identifierLabel = get_node("ScrollContainer/Layout/IdentifierLabel");
    dataLabel = get_node("ScrollContainer/Layout/DataLabel");
    actionButton = get_node("ScrollContainer/Layout/ActionButton");

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _input(event):
    if event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_LEFT && event.pressed:
        inspect();

func inspect():
    var collision = get_node("/root/App").view3D.camera.getRaycastHit();
    if !collision:
        return;
    var collider = collision.collider;
    if collider is Entity:
        identifierLabel.set_text(collider.identifier);
        texturePreview.set_texture(NoTexture);
        var dataText = "";
        var fields = collider.getFields();
        for field in fields.keys():
            dataText += "%s: %s\n" % [field, fields[field]];
        dataLabel.set_text(dataText);
        if collider.identifier == "trigger_changelevel":
            #if actionButton.is_connected("pressed", Callable(get_node("/root/App").view3D, "loadChangelevel")):
            #    actionButton.disconnect("pressed", Callable(get_node("/root/App").view3D, "loadChangelevel"));
            actionButton.connect("pressed", Callable(self, "loadChangelevel").bind(collider.get_position(), fields["map"]))
            actionButton.disabled = false;
    elif collider is StaticBody3D:
        inspectMesh(collision, collider);
    else:
        identifierLabel.set_text("?");
        dataLabel.set_text("...");

func inspectMesh(collision, collider):
    var mdt = MeshDataTool.new();
    var mesh = collider.get_node("Mesh").mesh;
    var files = get_node("/root/App").files;
    var bsp = files[get_node("/root/App").view3D.currentLevelFileId];
    for surface in range(0, mesh.get_surface_count()):
        var found = false;
        mdt.create_from_surface(mesh, surface);
        for face in range(0, mdt.get_face_count()):
            var vertices = [mdt.get_vertex(mdt.get_face_vertex(face, 0)),
                            mdt.get_vertex(mdt.get_face_vertex(face, 1)),
                            mdt.get_vertex(mdt.get_face_vertex(face, 2))];
            if (Geometry3D.segment_intersects_triangle(collision.position - collision.normal, collision.position + collision.normal,
                 vertices[0], vertices[1], vertices[2])):
                var compFace = bsp.GetFaceFromTriangle(vertices[0], vertices[1], vertices[2]);
                return; #skip
                var faceVertices = compFace.BuildTriFanVertices(bsp.bsp);
                found = true;
                var immesh = ImmediateMesh.new();
                immesh.surface_begin(Mesh.PRIMITIVE_TRIANGLES, FaceSelectedMaterial)
                for v in faceVertices:
                    immesh.surface_add_vertex(v + (collision.normal * 0.1));
                immesh.surface_end()
                overlayMesh.mesh = immesh;
                # Set UI
                texturePreview.set_texture(mdt.get_material().albedo_texture);
                identifierLabel.set_text("...");
                #dataLabel.set_text("vs: %v, vt: %v, sShift: %d, tShift: %d" % [texInfo.vs.GetGDVector3(), texInfo.vt.GetGDVector3(), texInfo.sShift, texInfo.tShift]);
        if found:
            break;

func loadChangeLevel(pos, map):
    get_node("/root/App").view3D.loadNextLevel(pos, map)
