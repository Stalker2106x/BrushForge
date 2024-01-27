extends MenuBar

var view3D;

var fileDropdown;
var viewDropdown;
var aboutDropdown;

# Called when the node enters the scene tree for the first time.
func _ready():
    view3D = get_node("../../3DView");
    # Bindings
    fileDropdown = get_node("File");
    fileDropdown.connect("index_pressed", Callable(self, "fileButtonPressed"));
    viewDropdown = get_node("View");
    viewDropdown.connect("index_pressed", Callable(self, "viewButtonPressed"));
    aboutDropdown = get_node("About");
    aboutDropdown.connect("index_pressed", Callable(self, "aboutButtonPressed"));

func setButtonStates(renderSettings):
    viewDropdown.set_item_checked(0, renderSettings.skybox);
    viewDropdown.set_item_checked(1, renderSettings.lights);
    viewDropdown.set_item_checked(2, renderSettings.pointEntities);
    viewDropdown.set_item_checked(3, renderSettings.modelEntities);
    viewDropdown.set_item_checked(4, renderSettings.collisions);

func fileButtonPressed(idx):
    if idx == 0:
        get_node("/root/App/FileDialog").set_visible(true);
    if idx == 1:
        get_node("/root/App/FolderDialog").set_visible(true);

func viewButtonPressed(idx):
    var enabled = !viewDropdown.is_item_checked(idx)
    if idx == 0: #Sky
        view3D.setRenderSetting("skybox", enabled);
    elif idx == 1: #Lights
        view3D.setRenderSetting("lights", enabled);
    elif idx == 2: #PointEntities
        view3D.setRenderSetting("pointEntities", enabled);
    elif idx == 3: #ModelEntities
        view3D.setRenderSetting("modelEntities", enabled);
    elif idx == 4: #Collisions
        view3D.setRenderSetting("collisions", enabled);
    viewDropdown.set_item_checked(idx, enabled);

func aboutButtonPressed(idx):
    get_node("/root/App/AboutDialog").visible = true;
