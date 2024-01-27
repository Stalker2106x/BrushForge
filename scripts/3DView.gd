extends Control

const CameraPrefab = preload("res://prefabs/Camera.tscn");

var renderSettings;
var camera;
var worldContainer;

var currentLevelMetadata;
var currentLevelFile;

# Called when the node enters the scene tree for the first time.
func _ready():
    renderSettings = {
        "skybox": false,
        "lights": true,
        "pointEntities": true,
        "modelEntities": true,
        "playAudio": true,
        "shading": "texturized",
        "collisions": true
    };
    get_node("../Top/TitleBar/").setButtonStates(renderSettings);
    # Nodes
    worldContainer = get_node("Viewport/World/Container");
    camera = get_node("Viewport/World/Camera");
    get_node("Viewport").connect("gui_input", Callable(self, "viewInput"));
    # Toolbar
    var sidebar = get_node("Sidebar/TopRight/Shading");
    sidebar.get_node("WireframeBtn").connect("pressed", Callable(self, "setRenderSetting").bind("shading", "wireframe"));
    sidebar.get_node("ShadedBtn").connect("pressed", Callable(self, "setRenderSetting").bind("shading", "shaded"));
    sidebar.get_node("TexturizedBtn").connect("pressed", Callable(self, "setRenderSetting").bind("shading", "texturized"));

func _input(event):
    if event.is_action_pressed("Escape"):
        setMouseCapture(false);

func _physics_process(delta):
    if camera:
        var coord = "Origin: %.02f %.02f  %.02f" % [camera.get_position().x, camera.get_position().y, camera.get_position().z]
        get_node("Sidebar/CoordinatesLabel").set_text(coord);

func viewInput(event):
    if event is InputEventMouseButton && event.pressed:
        setMouseCapture(true);

func setMouseCapture(enabled):
    if enabled:
        Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED);
    else:
        Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE);

func setRenderSetting(key, value):
    if renderSettings[key] == value:
        return; #Already set
    renderSettings[key] = value;
    if key == "shading":
        reloadLevel();
    else:
        applyRenderSettings();
    
func applyRenderSettings():
    get_node("Viewport/World/Container/Map/Skybox").visible = !renderSettings.skybox;
    var skyMat = camera.get_node("Camera").environment.sky.sky_material;
    if renderSettings.skybox:
        var files = get_node("/root/App").files;
        var cubemapTextures = currentLevelFile.GetSkyCubemapTextures(files);
        skyMat.set_shader_parameter("front", cubemapTextures[0]);
        skyMat.set_shader_parameter("left", cubemapTextures[1]);
        skyMat.set_shader_parameter("back", cubemapTextures[2]);
        skyMat.set_shader_parameter("right", cubemapTextures[3]);
        skyMat.set_shader_parameter("top", cubemapTextures[4]);
        skyMat.set_shader_parameter("bottom", cubemapTextures[5]);
    else:
        var blackPixel = load("res://assets/blackpixel.png");
        skyMat.set_shader_parameter("front", blackPixel);
        skyMat.set_shader_parameter("left", blackPixel);
        skyMat.set_shader_parameter("back", blackPixel);
        skyMat.set_shader_parameter("right", blackPixel);
        skyMat.set_shader_parameter("top", blackPixel);
        skyMat.set_shader_parameter("bottom", blackPixel);
    for entity in get_node("Viewport/World/Container/Map/PointEntities").get_children():
        for child in entity.get_children():
            if child is OmniLight3D:
                child.visible = renderSettings.lights;
    get_node("Viewport/World/Container/Map/PointEntities").visible = renderSettings.pointEntities;
    get_node("Viewport/World/Container/Map/ModelEntities").visible = renderSettings.modelEntities;
    camera.get_node("CameraCollider").disabled = !renderSettings.collisions;

func hasLevelLoaded():
    return worldContainer.get_child_count() > 0;

func getLevelMetadata():
    return currentLevelMetadata;

func reloadLevel():
    if !currentLevelFile:
        return;
    if hasLevelLoaded():
        unload();
    var files = get_node("/root/App").files;
    var mapNode = currentLevelFile.BuildGDLevel("", renderSettings, files);
    worldContainer.add_child(mapNode);
    applyRenderSettings();
    #get_node("Viewport/World/VoxelGI").bake(get_node("Viewport/World/Container"));

func unload(clearCache = false):
    for child in worldContainer.get_children():
        child.name = child.name+"_d";
        child.queue_free();
    if clearCache:
        currentLevelFile = null;
        currentLevelMetadata = null;

func loadLevel(levelMetadata):
    currentLevelMetadata = levelMetadata;
    currentLevelFile = get_node("/root/App").files[levelMetadata.fileId];
    reloadLevel();
    camera.set_position(currentLevelFile.GetLevelStart());
    camera.set_rotation(Vector3(0,0,0));
    
