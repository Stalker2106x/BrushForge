extends MenuBar

var view3D;

var fileDropdown;
var viewDropdown;
var aboutDropdown;

var settingsMap;

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

func setButtonStates(settings):
    settingsMap = {};
    var itemId = 0;
    for section in settings.keys():
        viewDropdown.add_separator(section, itemId);
        itemId += 1;
        for param in settings[section].keys():
            settingsMap[itemId] = "%s;%s" % [section, param];
            viewDropdown.add_item(param, itemId);
            viewDropdown.set_item_as_checkable(itemId, true);
            viewDropdown.set_item_checked(itemId, settings[section][param]);
            itemId += 1;

func fileButtonPressed(idx):
    if idx == 0:
        get_node("/root/App/FileDialog").set_visible(true);
    if idx == 1:
        get_node("/root/App/FolderDialog").set_visible(true);
    if idx == 1:
        get_node("/root/App/PreferencesDialog").set_visible(true);

func viewButtonPressed(idx):
    var enabled = !viewDropdown.is_item_checked(idx)
    var keys = settingsMap[idx].split(";");
    view3D.setSetting(keys[0], keys[1], enabled);
    viewDropdown.set_item_checked(idx, enabled);

func aboutButtonPressed(idx):
    get_node("/root/App/AboutDialog").visible = true;
