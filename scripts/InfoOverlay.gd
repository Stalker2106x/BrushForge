extends PanelContainer

var timer;
var state;
var text;

# Called when the node enters the scene tree for the first time.
func _ready():
    timer = get_node("Timer")
    timer.connect("timeout", Callable(self, "showOverlay"));


func setState(enabled, text_):
    state = enabled;
    text = text_;
    if enabled:
        timer.start(1.0);
    else:
        timer.start(0.1);

func showOverlay():
    if state:
        var files = get_node("/root/App").files;
        get_node("Label").set_text(text);
        set_position(get_viewport().get_mouse_position() - (size) - Vector2(10, 0))
    visible = state;
