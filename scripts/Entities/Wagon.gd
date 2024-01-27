extends PathFollow3D

var enabled;

var speed;

# Called when the node enters the scene tree for the first time.
func _ready():
    enabled = false;

func configure(speed_ : float):
    speed = speed_;

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if enabled && speed:
        progress += speed * delta;
