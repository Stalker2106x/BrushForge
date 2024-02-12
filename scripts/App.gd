extends Control

signal dependencies_loaded;

const FileEntryPrefab = preload("res://prefabs/FileEntry.tscn");
const Metadata = preload("res://importers/Metadata.cs");

var files = [];

var filesTree;

var view3D;
var importer; #Importer
var gamePath;

# Called when the node enters the scene tree for the first time.
func _ready():
    Log.logNode = get_node("Layout/TabContainer/Logs/LogLabel");
    importer = get_node("/root/App/Importer");
    view3D = get_node("Layout/CenterLayout/Main/3DView");
    #File UI
    get_node("FileDialog").connect("file_selected", Callable(self, "openAsset"));
    get_node("FileDialog").connect("files_selected", Callable(self, "openFiles").bind(false));
    get_node("FolderDialog").connect("dir_selected", Callable(self, "openFolder"));
    #File Texture content dialog
    var fileContentDialog = get_node("FileContentDialog")
    fileContentDialog.connect("close_requested", Callable(fileContentDialog, "set_visible").bind(false));
    #About dialog
    var aboutDialog = get_node("AboutDialog")
    aboutDialog.connect("close_requested", Callable(aboutDialog, "set_visible").bind(false));
    #Deps dialog
    var depsDialog = get_node("DependencyLoadDialog");
    depsDialog.connect("canceled", Callable(self, "emit_signal").bind("dependencies_loaded"));
    #Level select
    var levelSelect = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    levelSelect.connect("item_selected", Callable(self, "loadSelectedLevel"));
    # Files
    filesTree = get_node("Layout/CenterLayout/TabContainer/Files/ScrollContainer/FilesList");
    filesTree.connect("item_activated", Callable(self, "showFileContent"));
    filesTree.hide_root = true;
    var treeRoot = filesTree.create_item();
    treeRoot.set_text(0, "Files");
    #Locate game install
    gamePath = importer.LocateInstall();
    print(gamePath+"/valve/halflife.wad");
    if FileAccess.file_exists(gamePath+"/valve/halflife.wad"):
        Log.info("Found Half-Life Game Files at %s" % gamePath);
    else:
        Log.info("No Half-Life Content folder detected");

func openDependencyDialog(dependencies):
    var depsDialog = get_node("DependencyLoadDialog");
    depsDialog.visible = true;
    var fulldeps = [];
    for dep in dependencies:
        fulldeps.push_back(gamePath+dep.to_lower());
    depsDialog.connect("confirmed", Callable(self, "openFiles").bind(fulldeps, true));

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

func openFiles(filesPath, triggerSignal):
    for file in filesPath:
        openAsset(file, null);
    if triggerSignal:
        emit_signal("dependencies_loaded");

func openAsset(filePath, parent):
    Log.info("Loading %s..." % filePath);
    #Allow some time for the dialog to close
    await get_tree().create_timer(0.75).timeout;
    importer.Discover(filePath);
    if !importer:
        Log.error("Failed loading file");
        return; #Error loading metadata
    var asset = importer.ImportAsset(self);
    if !asset:
        Log.error("Failed loading file");
        return;
    # Skip deps for now, autoload
    # if asset.dependencies.size() > 0:
    #    openDependencyDialog(asset.dependencies);
    #    await dependencies_loaded;
    for dep in asset.dependencies:
        await openAsset(gamePath+dep.to_lower(), null);
    if asset.type == "Model":
        asset.BuildGDModel(files);
    files.push_back(asset);
    var entry = filesTree.create_item(parent if parent else filesTree.get_root());
    entry.set_text(0, asset.GetFileName());
    entry.set_tooltip_text(0, asset.path);
    if asset.type == "Pack" && asset.format != "Folder":
        addLevelsToSelect(files.size()-1, asset.GetFileName(), asset.GetLevels());
    Log.success("Finished");

func unlinkFile(fileId):
    var filesList = get_node("Layout/CenterLayout/TabContainer/Files/ScrollContainer/FilesList");
    filesList.get_child(fileId).queue_free();
    files[fileId].queue_free();
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
    if view3D.getLevelMetadata().fileId == levelMetadata.fileId:
        view3D.unload(true);

func loadSelectedLevel(selectId):
    var select = get_node("Layout/CenterLayout/Main/Top/LevelSelect");
    var metadata = select.get_item_metadata(selectId);
    view3D.loadLevel(metadata);
