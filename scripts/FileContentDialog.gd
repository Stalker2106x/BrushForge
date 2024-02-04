extends Window

const FileTextureEntryPrefab = preload("res://prefabs/FileTextureEntry.tscn");
const FileSoundEntryPrefab = preload("res://prefabs/FileSoundEntry.tscn");

func _ready():
    var playButton = get_node("Layout/TabContainer/Sounds/VBoxContainer/PlayButton");
    playButton.connect("pressed", Callable(self, "play"));

func showContent(fileId):
    var files = get_node("/root/App").files;
    #Levels
    if files[fileId].type == "Pack":
        var levelsList = get_node("Layout/TabContainer/Levels");
        for child in levelsList.get_children():
            child.queue_free();
        for level in files[fileId].GetLevels():
            var label = Label.new();
            label.set_text(level);
            levelsList.add_child(label);
    #Textures
    var texturesList = get_node("Layout/TabContainer/Textures/ScrollContainer/TextureList");
    for child in texturesList.get_children():
        child.queue_free();
    var fileTextures = files[fileId].gdTextures;
    for textureName in fileTextures.keys():
        var entryIt = FileTextureEntryPrefab.instantiate();
        entryIt.get_node("Texture").set_texture(fileTextures[textureName]);
        entryIt.get_node("TextureName").set_text(textureName);
        texturesList.add_child(entryIt);
    get_node("Layout/TabContainer/Textures/Info").set_text("%d Textures found" % fileTextures.size());
    #Model
    var model = files[fileId].gdModel;
    if model:
        var anims = model.get_node("anims");
        var animations = anims.get_animation_list();
        var animationsList = get_node("Layout/TabContainer/Model/ScrollContainer/AnimationsList");
        for child in animationsList.get_children():
            child.queue_free();
        for anim in animations:
            var btn = Button.new();
            btn.set_text(anim);
            btn.connect("pressed", Callable(anims, "play").bind(anim));
            animationsList.add_child(btn);
        var modelContainer = get_node("Layout/TabContainer/Model/SubViewportContainer/SubViewport/Container");
        for child in modelContainer.get_children():
            child.queue_free();
        modelContainer.add_child(model);
    #Sounds
    var soundsList = get_node("Layout/TabContainer/Sounds/ScrollContainer/SoundsList");
    for child in soundsList.get_children():
        child.queue_free();
    var fileSounds = files[fileId].gdSounds;
    for soundName in fileSounds.keys():
        var entryIt = FileSoundEntryPrefab.instantiate();
        entryIt.get_node("PlayButton").connect("pressed", Callable(self, "setStream").bind(fileSounds[soundName]));
        entryIt.get_node("SoundName").set_text(soundName);
        soundsList.add_child(entryIt);
    set_visible(true);

func setStream(fStream):
    var player = get_node("Layout/TabContainer/Sounds/VBoxContainer/AudioStreamPlayer");
    player.stream = fStream;
    play();

func play():
    var player = get_node("Layout/TabContainer/Sounds/VBoxContainer/AudioStreamPlayer");
    if player.playing:
        player.stop();
    else:
        player.play();

func _process(delta):
    var player = get_node("Layout/TabContainer/Sounds/VBoxContainer/AudioStreamPlayer");
    if player.playing:
        var progress = get_node("Layout/TabContainer/Sounds/VBoxContainer/SoundProgress");
        progress.value = player.get_playback_position();
