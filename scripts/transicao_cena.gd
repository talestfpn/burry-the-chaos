extends Area2D

@export_file("*.tscn") var proxima_cena: String
@export var precisa_interagir: bool = false
@export var sprite_do_elevador: Node2D

var player_na_area: bool = false
var player_node: CharacterBody2D = null
var transicionando: bool = false 

func _ready():
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body):
	if body.name == "bury":
		player_node = body
		if precisa_interagir:
			player_na_area = true
		else:
			iniciar_transicao()

func _on_body_exited(body):
	if body.name == "bury":
		player_na_area = false
		player_node = null

func _input(event):
	if player_na_area and precisa_interagir and not transicionando:
		# AQUI FOI A MUDANÇA: O Godot cuida do Teclado e do Controle sozinho!
		if event.is_action_pressed("interact"):
			iniciar_transicao()

func iniciar_transicao():
	transicionando = true 

	if precisa_interagir:
		if player_node != null and sprite_do_elevador != null:

			# 1. Trava o player e pega a referência da animação
			player_node.set_physics_process(false) 
			player_node.velocity = Vector2.ZERO
			var anim_player = player_node.get_node("AnimatedSprite2D")

			# PASSO 1: Aperta Botão -> Vira de costas e FICA 
			anim_player.play("personagem_entrando")

			await get_tree().create_timer(0.9).timeout

			# PASSO 2: Porta abre 
			anim_player.pause()
			sprite_do_elevador.anim.play("abrindo") 
			await sprite_do_elevador.anim.animation_finished

			# PASSO 3: Começa a andar direto pra dentro 
			anim_player.play("personagem_andando")
			player_node.z_index = -1 

			# Faz o movimento pro fundo
			var tween = get_tree().create_tween()
			tween.tween_property(player_node, "global_position:x", sprite_do_elevador.global_position.x, 0.6)
			tween.parallel().tween_property(player_node, "scale", Vector2(0.85, 0.85), 0.6)
			tween.parallel().tween_property(player_node, "modulate", Color(0.3, 0.3, 0.3, 1.0), 0.6)

			await tween.finished
			
			# PASSO 4: Fecha a porta 
			sprite_do_elevador.anim.play_backwards("abrindo")
			await sprite_do_elevador.anim.animation_finished

			await get_tree().create_timer(0.4).timeout

		else:
			await get_tree().create_timer(1.0).timeout

	mudar_de_cena()

func mudar_de_cena():
	if proxima_cena != "":
		get_tree().change_scene_to_file(proxima_cena)
