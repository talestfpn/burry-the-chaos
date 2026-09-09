extends StaticBody2D # (Ou Node2D, dependendo do que está na sua cena)

@onready var anim = $AnimatedSprite2D
@onready var area = $AreaDeteccao

func _ready():
	anim.play("fechado")
