extends Node3D
class_name Entity

var type;
var identifier;
var data;

func configure(type_ : String, identifier_ : String, data_ : Dictionary, customTextureName : String):
    identifier = identifier_;
    data = data_;
    var gizmo = get_node_or_null("Gizmo");
    if gizmo:
        get_node("Gizmo/IdentifierLabel").set_text(identifier_);
        if customTextureName != "":
            var files = get_node("/root/App").files;
            for file in files:
                if file.gdTextures.has(customTextureName):
                    get_node("Gizmo/Icon").set_texture(file.gdTextures[customTextureName]);
                    break;

func use():
    pass;
