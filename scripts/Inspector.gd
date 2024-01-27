extends ScrollContainer


# Called when the node enters the scene tree for the first time.
func _ready():
    pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    var collision = get_node("/root/App/Layout/CenterLayout/Main/3DView").camera.getRaycastHit();
    if !collision:
        return;
    var collider = collision.collider;
    if collider is Entity:
        if !collider.data:
            return; #Unloaded map
        get_node("Layout/IdentifierLabel").set_text(collider.identifier);
        var dataText = "";
        for field in collider.data.keys():
            dataText += "%s: %s\n" % [field, collider.data[field]];
        get_node("Layout/DataLabel").set_text(dataText);
    else:
        get_node("Layout/IdentifierLabel").set_text("?");
        get_node("Layout/DataLabel").set_text("...");
