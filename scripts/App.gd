extends Control

const FileEntryPrefab = preload("res://prefabs/FileEntry.tscn");
const Metadata = preload("res://importers/Metadata.cs");

var files = [];

var filesTree;

# Called when the node enters the scene tree for the first time.
func _ready():
    Log.logNode = get_node("Layout/TabContainer/Logs/LogLabel");
    #File UI
    get_node("FileDialog").connect("file_selected", Callable(self, "openAsset"));
    get_node("FileDialog").connect("files_selected", Callable(self, "openFiles"));
    get_node("FolderDialog").connect("dir_selected", Callable(self, "openFolder"));
    #File Texture content dialog
    var fileContentDialog = get_node("FileContentDialog")
    fileContentDialog.connect("close_requested", Callable(fileContentDialog, "set_visible").bind(false));
    #About dialog
    var aboutDialog = get_node("AboutDialog")
    aboutDialog.connect("close_requested", Callable(aboutDialog, "set_visible").bind(false));
    #Level select
    var view3D = get_node("Layout/CenterLayout/Main/3DView");
    var levelSelect = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    levelSelect.connect("item_selected", Callable(self, "loadSelectedLevel"));
    # Files
    filesTree = get_node("Layout/CenterLayout/TabContainer/Files/ScrollContainer/FilesList");
    filesTree.connect("item_activated", Callable(self, "showFileContent"));
    filesTree.hide_root = true;
    var treeRoot = filesTree.create_item();
    treeRoot.set_text(0, "Files");

func showFileContent():
    var selectedItem = filesTree.get_selected();
    if selectedItem.get_child_count() > 0:
        return; #Is a folder
    var dialog = get_node("/root/App/FileContentDialog");
    var fileName = selectedItem.get_text(0);
    for i in range(0, files.size()):
        if files[i].GetFileName() == fileName:
            dialog.showContent(i);

func openFolder(folderPath):
    var parentEntry = filesTree.create_item(filesTree.get_root());
    parentEntry.set_text(0, folderPath.split("/")[-1]);
    var dir = DirAccess.open(folderPath)
    if dir:
        dir.list_dir_begin()
        var entry = dir.get_next()
        while entry != "":
            if dir.current_is_dir():
                openFolder(folderPath+"/"+entry);
            else:
                openAsset(folderPath+"/"+entry, parentEntry);
            entry = dir.get_next();

func openFiles(filesPath):
    for file in filesPath:
        openAsset(file, null);

func openAsset(filePath, parent):
    filePath = filePath.replace("\\", "/"); #Windows sucks
    Log.info("Loading %s..." % filePath);
    #Allow some time for the dialog to close
    await get_tree().create_timer(0.75).timeout;
    var metadata = Metadata.new();
    metadata.Discover(filePath);
    if !metadata:
        Log.error("Failed loading file");
        return; #Error loading metadata
    var asset = metadata.ImportAsset();
    if !asset:
        Log.error("Failed loading file");
        return;
    files.push_back(asset);
    var entry = filesTree.create_item(parent if parent else filesTree.get_root());
    entry.set_text(0, asset.GetFileName());
    entry.set_tooltip_text(0, asset.path);
    #fileEntry.configure(files.size()-1);
    if asset.type == "Pack" && asset.format != "Folder":
        addLevelsToSelect(files.size()-1, asset.GetFileName(), asset.GetLevels());
    Log.success("Finished");

func unlinkFile(fileId):
    var filesList = get_node("Layout/CenterLayout/TabContainer/Files/ScrollContainer/FilesList");
    filesList.get_child(fileId).queue_free();
    files[fileId].queue_free();
    var view3D = get_node("Layout/CenterLayout/Main/3DView");
    var levelMetadata = view3D.getLevelMetadata();
    removeLevelFromSelect(levelMetadata);
    if levelMetadata.fileId == fileId:
        view3D.unload();
    elif view3D.hasLevelLoaded():
        view3D.reloadLevel();

func addLevelsToSelect(fileId, filename, levels):
    var select = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    for level in levels:
        var levelLabel = str(filename);
        if levels.size() > 1:
            levelLabel += ":%s" % level;
        select.add_item(levelLabel);
        var selectId = select.item_count-1;
        select.set_item_metadata(selectId, { "selectId": selectId, "fileId": fileId, "levelId": level });
        if selectId == 0:
            loadSelectedLevel(selectId);

func removeLevelFromSelect(levelMetadata):
    var select = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    select.remove_item(levelMetadata.selectId);
    var view3D = get_node("Layout/CenterLayout/Main/3DView");
    if view3D.getLevelMetadata().fileId == levelMetadata.fileId:
        view3D.unload(true);

func loadSelectedLevel(selectId):
    var select = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    var metadata = select.get_item_metadata(selectId);
    get_node("Layout/CenterLayout/Main/3DView").loadLevel(metadata);
    get_node("Layout/CenterLayout/TabContainer/Level Textures").fill(metadata, files)
