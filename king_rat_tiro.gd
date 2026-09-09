extends Area2D

@export var velocidade: float = 90.0
@export var tempo_de_vida: float = 3.0

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D

var direcao: int = -1


func _ready() -> void:
	anim.play("tiro")
	
	if direcao < 0:
		anim.flip_h = true
	else:
		anim.flip_h = false
	
	await get_tree().create_timer(tempo_de_vida).timeout
	queue_free()


func _physics_process(delta: float) -> void:
	global_position.x += float(direcao) * velocidade * delta
