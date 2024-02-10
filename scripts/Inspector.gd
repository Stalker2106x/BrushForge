extends Control

const NoTexture = preload("res://assets/Missing.png");

var texturePreview;
var identifierLabel;
var dataLabel;

var selectedOverlay;

# Called when the node enters the scene tree for the first time.
func _ready():
    selectedOverlay = MeshInstance3D.new();
    get_tree().get_root().add_child(selectedOverlay);
    texturePreview = get_node("ScrollContainer/Layout/TexturePreview");
    identifierLabel = get_node("ScrollContainer/Layout/IdentifierLabel");
    dataLabel = get_node("ScrollContainer/Layout/DataLabel");

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _input(event):
    if event is InputEventMouseButton && event.pressed:
        inspect();

func inspect():
    var collision = get_node("/root/App/Layout/CenterLayout/Main/3DView").camera.getRaycastHit();
    if !collision:
        return;
    var collider = collision.collider;
    if collider is Entity:
        if !collider.data:
            return; #Unloaded map
        identifierLabel.set_text(collider.identifier);
        texturePreview.set_texture(NoTexture);
        var dataText = "";
        for field in collider.data.keys():
            dataText += "%s: %s\n" % [field, collider.data[field]];
        dataLabel.set_text(dataText);
    elif collider is StaticBody3D:
        inspectMesh(collision, collider);
    else:
        identifierLabel.set_text("?");
        dataLabel.set_text("...");

func inspectMesh(collision, collider):
    var mdt = MeshDataTool.new();
    var mesh = collider.get_node("Mesh").mesh;
    for surface in range(0, mesh.get_surface_count()):
        var found = false;
        mdt.create_from_surface(mesh, surface);
        for face in range(0, mdt.get_face_count()):
            var vertices = [mdt.get_vertex(mdt.get_face_vertex(face, 0)), mdt.get_vertex(mdt.get_face_vertex(face, 1)), mdt.get_vertex(mdt.get_face_vertex(face, 2))];
            if (Geometry3D.segment_intersects_triangle(collision.position - collision.normal, collision.position + collision.normal,
                 vertices[0], vertices[1], vertices[2])):
                # Draw immediate mesh (select overlay)
                var sftool := SurfaceTool.new()
                sftool.begin(Mesh.PRIMITIVE_TRIANGLES);
                for vertex in vertices:
                    sftool.add_vertex(vertex + collision.normal);
                selectedOverlay.mesh = sftool.commit();
                # Set UI
                texturePreview.set_texture(mdt.get_material().albedo_texture);
                identifierLabel.set_text("...");
                dataLabel.set_text("");
                found = true;
                break;
        if found:
            break;

