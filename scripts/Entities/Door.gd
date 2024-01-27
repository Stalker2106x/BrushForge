extends Entity

const StartsOpen = 1 << 0;
const NonSolid = 1 << 1;
const Passable = 1 << 2;
const Toggle = 1 << 3;
const Usable = 1 << 4;
const NoNPC = 1 << 5;
const TouchOpens = 1 << 6;
const StartsLocked = 1 << 7;
const DoorSilent = 1 << 8;
const OnlyStill = 1 << 9;

var direction;
var speed;
var extents;
var wait;
var flags;

var targetPos;

var closeTimer;

var opened;
var locked;

func _ready():
    closeTimer = Timer.new();
    closeTimer.one_shot = true;
    add_child(closeTimer);
    closeTimer.connect("timeout", Callable(self, "open").bind(false));

func configureDoor(entity):
    extents = get_node("Mesh").mesh.get_aabb();
    speed = float(entity["SPEED"]) / 1000 if entity.has("SPEED") else 1.0;
    direction = entity["ANGLE"] if entity.has("ANGLE") else Vector3(0, 0, -1);
    wait = float(entity["WAIT"]) if entity.has("WAIT") else 1.0;
    flags = int(entity["FLAGS"]) if entity.has("FLAGS") else 0;
    locked = flags & StartsLocked || entity.has("TARGETNAME");
    opened = false;

func use():
    open(true);

func open(enabled):
    if (opened == enabled):
        return;
    if locked:
        return;
    opened = enabled;
    if (opened):
        targetPos = position + (extents.size * direction);
        closeTimer.start(wait);
    else:
        targetPos -= position - (extents.size * direction);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if targetPos && position != targetPos:
        position += lerp(position, targetPos, speed);
