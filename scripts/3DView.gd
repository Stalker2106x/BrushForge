extends Control

const CameraPrefab = preload("res://prefabs/Camera.tscn");

var settings;
var shading;
var camera;
var worldContainer;
var crosshair;

var currentLevelMetadata;
var currentLevelFile;

# Called when the node enters the scene tree for the first time.
func _ready():
    shading = "texturized";
    settings = {
        "render": {
            "skybox": false,
            "lights": true,
            "pointEntities": true,
            "modelEntities": true,
            "beams": true
        },
        "entities": {
            "playAudio": true,
            "runFuncTrain": true
        },
        "camera": {
            "collisions": false,
            "gravity": false
        }
    };
    get_node("/root/App/Layout/CenterLayout/Main/Top/TitleBar/").setButtonStates(settings);
    # Nodes
    crosshair = get_node("../Crosshair");
    worldContainer = get_node("World/Container");
    camera = get_node("World/Camera");
    connect("gui_input", Callable(self, "viewInput"));
    # Toolbar
    var sidebar = get_node("/root/App/Layout/CenterLayout/Main/Views/Sidebar/TopRight/Shading");
    sidebar.get_node("WireframeBtn").connect("pressed", Callable(self, "setShading").bind("wireframe"));
    sidebar.get_node("ShadedBtn").connect("pressed", Callable(self, "setShading").bind("shaded"));
    sidebar.get_node("TexturizedBtn").connect("pressed", Callable(self, "setShading").bind("texturized"));

func _input(event):
    if event.is_action_pressed("Escape"):
        setMouseCapture(false);

func _physics_process(delta):
    if camera:
        var coord = "Origin: %.02f %.02f  %.02f" % [camera.get_position().x, camera.get_position().y, camera.get_position().z]
        get_node("/root/App/Layout/CenterLayout/Main/Views/Sidebar/CoordinatesLabel").set_text(coord);

func viewInput(event):
    if event is InputEventMouseButton && event.pressed:
        setMouseCapture(true);

func setMouseCapture(enabled):
    if enabled:
        Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED);
        crosshair.visible = true;
    else:
        Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE);
        crosshair.visible = false;

func setSetting(section, key, value):
    if settings[section][key] == value:
        return; #Already set
    settings[section][key] = value;
    applySettings();
    
func setShading(shading_):
    shading = shading_;
    reloadLevel();

func applySettings():
    get_node("World/Container/Map/Skybox").visible = !settings.render.skybox;
    var skyMat = camera.get_node("Camera").environment.sky.sky_material;
    if settings.render.skybox:
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
    for entity in get_node("World/Container/Map/PointEntities").get_children():
        for child in entity.get_children():
            if child is OmniLight3D:
                child.visible = settings.render.lights;
            if child is Beam:
                child.enabled = settings.render.beams;
    for path in get_node("World/Container/Map/Paths").get_children():
        path.get_node("Wagon").enabled = settings.entities.runFuncTrain;
    get_node("World/Container/Map/PointEntities").visible = settings.render.pointEntities;
    get_node("World/Container/Map/ModelEntities").visible = settings.render.modelEntities;
    camera.get_node("CameraCollider").disabled = !settings.camera.collisions;
    camera.gravity = settings.camera.gravity;

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
    var mapNode = currentLevelFile.BuildGDLevel("", shading, files);
    if (mapNode == null):
        return;
    worldContainer.add_child(mapNode);
    applySettings();
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
    
