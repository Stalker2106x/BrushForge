extends Entity

var direction;
var speed;
var extents;

var open;

func configureDoor(speed_, direction_ = Vector3(0, 0, -1)):
    extents = get_node("Mesh").mesh.get_aabb();
    speed = speed_;
    direction = direction_;
    open = false;

func use():
    open = !open;
    if (open):
        position += (extents.size * direction);
    else:        
        position -= (extents.size * direction);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    pass
