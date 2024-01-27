extends VBoxContainer

const TextureEntryPrefab = preload("res://prefabs/TextureEntry.tscn");

# Called when the node enters the scene tree for the first time.
func _ready():
    pass # Replace with function body.

func fill(metadata, files):
    var textureList = get_node("ScrollContainer/TextureList");
    for child in textureList.get_children():
        child.queue_free();
    for levelTexture in files[metadata.fileId].gdTextures:
        var entry = TextureEntryPrefab.instantiate();
        entry.get_node("Data/NameLabel").set_text(levelTexture.textureName);
        for file in files:
            if file.gdTextures.has(levelTexture.textureName):
                entry.get_node("Texture").set_texture(file.gdTextures[levelTexture.textureName].texture);
                entry.get_node("Data/SourceLabel").set_text(file.filename);
                break;
        textureList.add_child(entry);
