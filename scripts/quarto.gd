extends Node2D
@onready var player = $bury
@onready var camera = $bury/Camera2D
@onready var cama_sprite = $tiles/cama
@onready var sprite_player = $bury/AnimatedSprite2D
@onready var ponto_spawn = $PontoDeSpawn

func _ready() -> void:
	if camera:
		camera.enabled = true
		camera.zoom = Vector2(1.5, 1.5)
		camera.position = Vector2.ZERO
		camera.offset.y = -35
		camera.limit_bottom = 1000000
		camera.limit_top = -1000000

	if ponto_spawn:
		player.global_position = ponto_spawn.global_position

	player.velocity = Vector2.ZERO
	player.set_physics_process(false)
	player.bloqueio_animacao = true
	player.visible = false

	cama_sprite.play("levantando")
	cama_sprite.frame = 0

	while cama_sprite.frame < 5:
		await get_tree().process_frame

	sprite_player.play("idle")
	sprite_player.frame = 0
	player.visible = true

	await cama_sprite.animation_finished

	# Zera qualquer velocidade acumulada
	player.velocity = Vector2.ZERO
	player.set_physics_process(true)

	# Espera até o player pousar no chão
	while not player.is_on_floor():
		player.velocity.y = 0
		player.move_and_slide()
		await get_tree().physics_frame

	player.bloqueio_animacao = false

func _process(_delta: float) -> void:
	pass
