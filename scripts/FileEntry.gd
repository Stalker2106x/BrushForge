extends HBoxContainer

var fileId;

func configure(fileId_):
    fileId = fileId_;
    var files = get_node("/root/App").files;
    get_node("Data/FilenameLabel").set_text(files[fileId].GetFileName());
    # Connect buttons
    var unlinkBtn = get_node("RemoveButton");
    unlinkBtn.connect("pressed", Callable(get_node("/root/App"), "unlinkFile").bind(fileId));
    var browseBtn = get_node("Data/BrowseButton");
    browseBtn.connect("pressed", Callable(get_node("/root/App/FileContentDialog"), "showContent").bind(fileId));
    # Connect overlay
    var overlay = get_node("/root/App/InfoOverlay");
    connect("mouse_entered", Callable(overlay, "setState").bind(true, files[fileId].path));
    connect("mouse_exited", Callable(overlay, "setState").bind(false, files[fileId].path));
