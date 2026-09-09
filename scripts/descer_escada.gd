extends Area2D

@export var ponto_cima: Marker2D
@export var ponto_baixo: Marker2D
@export var ponto_saida_cima: Marker2D
@export var velocidade: float = 0.8
@export var velocidade_ida: float = 0.4

var player_perto: bool = false
var player_node: CharacterBody2D = null
var em_movimento: bool = false

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body: Node2D) -> void:
	if body.name == "bury":
		player_perto = true
		player_node = body

func _on_body_exited(body: Node2D) -> void:
	if body.name == "bury" and not em_movimento:
		player_perto = false
		player_node = null

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("interact") and player_perto and player_node != null and not em_movimento:
		usar_escada()

func usar_escada() -> void:
	em_movimento = true

	player_node.set_physics_process(false)
	player_node.velocity = Vector2.ZERO

	var anim_player = player_node.get_node("AnimatedSprite2D")
	var colisao = player_node.get_node("ColisaoNormal")

	var dist_cima = abs(player_node.global_position.y - ponto_cima.global_position.y)
	var dist_baixo = abs(player_node.global_position.y - ponto_baixo.global_position.y)

	var subindo = dist_baixo < dist_cima
	var ponto_inicio = ponto_baixo if subindo else ponto_cima
	var ponto_destino = ponto_cima if subindo else ponto_baixo

	colisao.set_deferred("disabled", true)

	# 1. Anda até a escada
	var alvo_x = ponto_inicio.global_position.x
	var direcao = sign(alvo_x - player_node.global_position.x)
	if abs(alvo_x - player_node.global_position.x) > 2:
		anim_player.flip_h = (direcao < 0)
		player_node.facing_direction = int(direcao)
		anim_player.play("walk")

		var tween_ir = get_tree().create_tween()
		tween_ir.tween_property(player_node, "global_position:x", alvo_x, velocidade_ida)
		await tween_ir.finished

	# 2. Animação de escada
	anim_player.flip_h = false
	player_node.facing_direction = 1
	anim_player.play("personagem_descendo")

	# 3. Move vertical
	var tween_escada = get_tree().create_tween()
	tween_escada.tween_property(player_node, "global_position:y", ponto_destino.global_position.y, velocidade)
	await tween_escada.finished

	# 4. Se subiu, anda até o chão ao lado da escada
	if subindo and ponto_saida_cima:
		anim_player.play("walk")
		var direcao_saida = sign(ponto_saida_cima.global_position.x - player_node.global_position.x)
		anim_player.flip_h = (direcao_saida < 0)
		player_node.facing_direction = int(direcao_saida)

		var tween_saida = get_tree().create_tween()
		tween_saida.tween_property(player_node, "global_position", ponto_saida_cima.global_position, velocidade_ida)
		await tween_saida.finished

	# 5. Volta ao normal
	colisao.set_deferred("disabled", false)
	anim_player.play("idle")
	player_node.velocity = Vector2.ZERO
	player_node.set_physics_process(true)
	em_movimento = false
