extends CharacterBody2D

# --- Configurações da Caveira (Editáveis no Inspetor) ---
@export_group("Status do Inimigo")
@export var velocidade_patrulha: float = 25.0
@export var velocidade_perseguicao: float = 45.0
@export var velocidade_ataque: float = 1.0 # <-- AQUI! Controla a rapidez da mordida
@export var distancia_patrulha: float = 80.0
@export var distancia_visao: float = 180.0
@export var distancia_ataque: float = 30.0
@export var tempo_vulneravel: float = 5.0
@export var dano_ataque: int = 1
@export var vida_maxima: int = 3

var gravidade = ProjectSettings.get_setting("physics/2d/default_gravity")
var direcao = -1 
var estado = "PATRULHA" 
var vida = vida_maxima
var player_ref: CharacterBody2D = null
var posicao_boca_x: float = 0.0 
var timer_vulneravel: float = 0.0

var posicao_inicial_x: float = 0.0 

# --- Referências dos Nós ---
@onready var anim = $AnimatedSprite2D
@onready var hitbox_ataque = $HitboxAtaque
@onready var hitbox_shape = $HitboxAtaque/CollisionShape2D

func _ready():
	hitbox_ataque.body_entered.connect(_on_hitbox_entered)
	hitbox_shape.set_deferred("disabled", true)
	vida = vida_maxima
	posicao_boca_x = abs(hitbox_ataque.position.x)
	posicao_inicial_x = global_position.x 

func _physics_process(delta):
	if not is_on_floor():
		velocity.y += gravidade * delta

	if estado == "MORTO":
		move_and_slide()
		return

	if player_ref == null:
		var possivel_player = get_tree().get_root().find_child("bury", true, false)
		if possivel_player and possivel_player is CharacterBody2D:
			player_ref = possivel_player

	var distancia_player = 9999.0
	if player_ref != null:
		distancia_player = global_position.distance_to(player_ref.global_position)

	match estado:
		"PATRULHA":
			if distancia_player < distancia_visao:
				estado = "PERSEGUICAO"
			else:
				patrulhar()

		"PERSEGUICAO":
			if distancia_player > distancia_visao + 50:
				estado = "PATRULHA"
				posicao_inicial_x = global_position.x 
			elif distancia_player <= distancia_ataque:
				iniciar_ataque()
			else:
				perseguir()

		"ATAQUE":
			velocity.x = 0
			if player_ref != null:
				direcao = -1 if player_ref.global_position.x < global_position.x else 1
				atualizar_direcao(direcao)
				
		"VULNERAVEL":
			velocity.x = 0
			timer_vulneravel -= delta
			if timer_vulneravel <= 0:
				estado = "PERSEGUICAO"

	move_and_slide()

func patrulhar():
	velocity.x = direcao * velocidade_patrulha
	anim.play("walk")
	atualizar_direcao(direcao)

	if is_on_wall() or abs(global_position.x - posicao_inicial_x) > distancia_patrulha:
		direcao *= -1
		global_position.x += direcao * 2 

func perseguir():
	if player_ref:
		direcao = -1 if player_ref.global_position.x < global_position.x else 1
		
	velocity.x = direcao * velocidade_perseguicao
	anim.play("walk")
	atualizar_direcao(direcao)

func iniciar_ataque():
	if estado == "ATAQUE": return 
	
	estado = "ATAQUE"
	
	# A MÁGICA TÁ AQUI: Passamos a variável como multiplicador de velocidade!
	anim.play("attack", velocidade_ataque) 
	
	hitbox_shape.set_deferred("disabled", false)

	await anim.animation_finished

	hitbox_shape.set_deferred("disabled", true)
	estado = "VULNERAVEL"
	timer_vulneravel = tempo_vulneravel
	
	if anim.sprite_frames.has_animation("idle"):
		anim.play("idle")
	else:
		anim.play("walk") 
		anim.pause() 

func atualizar_direcao(dir):
	anim.flip_h = (dir > 0)
	if dir > 0:
		hitbox_ataque.position.x = posicao_boca_x
	else:
		hitbox_ataque.position.x = -posicao_boca_x

func _on_hitbox_entered(body):
	if body.name == "bury" and estado == "ATAQUE":
		if body.has_method("tomar_dano"):
			body.tomar_dano(dano_ataque, self)

func tomar_dano(quantidade: int, origem: Node2D = null):
	if estado == "MORTO": return
	
	vida -= quantidade
	anim.modulate = Color(1, 0.3, 0.3)
	await get_tree().create_timer(0.2).timeout
	anim.modulate = Color.WHITE
	
	if vida <= 0:
		morrer()

func morrer():
	estado = "MORTO"
	velocity.x = 0
	$CollisionShape2D.set_deferred("disabled", true)
	hitbox_shape.set_deferred("disabled", true)
	
	var tween = get_tree().create_tween()
	tween.tween_property(self, "modulate:a", 0.0, 0.5)
	await tween.finished
	queue_free()
