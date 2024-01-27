extends PathFollow3D

var speed;

# Called when the node enters the scene tree for the first time.
func _ready():
    pass # Replace with function body.

func configure(speed_ : float):
    speed = speed_;

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
    if speed:
        progress += speed * delta;
