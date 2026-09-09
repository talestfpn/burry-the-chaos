extends Area2D

# --- CONFIGURAÇÕES ---
@export var velocidade_patrulha: float = 40.0
@export var velocidade_ataque: float = 700.0
@export var velocidade_retorno: float = 150.0
@export var distancia_patrulha: float = 80.0
@export var tempo_vulneravel: float = 1.0
@export var cooldown_ataque: float = 2.0
@export var limite_do_chao_y: float = 152.0

var posicao_inicial_x: float
var altura_patrulha_original: float
var indo_direita: bool = true
var pode_atacar: bool = true
var estado: String = "patrulhando"
var ponto_de_retorno: Vector2
var vida: int = 3

# --- REFERÊNCIAS AOS NÓS ---
@onready var anim = $InimigoAtaque
@onready var sprite_machucado = $InimigoMachucado
@onready var area_deteccao = $AreaDeteccao

func _ready():
	posicao_inicial_x = global_position.x
	altura_patrulha_original = global_position.y
	anim.play("fly")
	sprite_machucado.visible = false
	sprite_machucado.modulate = Color(1, 1, 1, 1)
	rotation_degrees = 0

func _process(delta: float) -> void:
	if estado == "morto":
		return

	match estado:
		"patrulhando": _logica_patrulha(delta)
		"atacando": _logica_ataque(delta)
		"retornando": _logica_retorno(delta)

func _logica_patrulha(delta):
	rotation_degrees = 0
	global_position.y = altura_patrulha_original
	if pode_atacar:
		for body in area_deteccao.get_overlapping_bodies():
			if body.name == "bury":
				iniciar_ataque()
				break
	var dir = 1.0 if indo_direita else -1.0
	global_position.x += (velocidade_patrulha * dir) * delta
	anim.flip_h = !indo_direita
	sprite_machucado.flip_h = !indo_direita
	if indo_direita and global_position.x >= posicao_inicial_x + distancia_patrulha: indo_direita = false
	elif not indo_direita and global_position.x <= posicao_inicial_x - distancia_patrulha: indo_direita = true

func _logica_ataque(delta):
	global_position.y += velocidade_ataque * delta
	if global_position.y >= limite_do_chao_y:
		global_position.y = limite_do_chao_y
		ficar_no_chao() # Se chegar no chão sem bater no player, fica vulnerável

func _logica_retorno(delta):
	rotation_degrees = 0
	var direcao = global_position.direction_to(ponto_de_retorno)
	global_position += direcao * velocidade_retorno * delta
	if global_position.distance_to(ponto_de_retorno) < 5.0:
		global_position = ponto_de_retorno
		estado = "patrulhando"
		iniciar_cooldown()

func tomar_dano(quantidade: int, _atacante: Node2D = null):
	if estado == "morto": return
	vida -= quantidade
	anim.visible = false
	sprite_machucado.visible = true
	
	if vida <= 0:
		sprite_machucado.modulate = Color(1, 1, 1, 1)
		morrer()
	else:
		sprite_machucado.modulate = Color(1.0, 0.2, 0.2)
		if estado == "no_chao": estado = "retornando"
		await get_tree().create_timer(0.2).timeout
		if estado != "morto":
			sprite_machucado.modulate = Color(1, 1, 1, 1)
			sprite_machucado.visible = false
			anim.visible = true
			anim.play("fly" if estado in ["patrulhando", "retornando"] else "hit")

func morrer():
	estado = "morto"
	
	# Desativa as colisões para não machucar mais o player
	monitoring = false
	monitorable = false
	if has_node("CollisionShape2D"):
		$CollisionShape2D.set_deferred("disabled", true)
	if has_node("AreaDeteccao/CollisionShape2D"):
		$AreaDeteccao/CollisionShape2D.set_deferred("disabled", true)
	
	# Garante que o sprite de ataque suma e o de morte apareça alinhado
	anim.visible = false
	sprite_machucado.visible = true
	sprite_machucado.rotation_degrees = 0
	
	# Toca a explosão
	sprite_machucado.play("morte")
	
	# Espera o sangue estourar e apaga da cena
	await sprite_machucado.animation_finished
	queue_free()

func iniciar_ataque():
	if estado != "patrulhando" or not pode_atacar: return
	estado = "preparando"
	pode_atacar = false
	ponto_de_retorno = Vector2(global_position.x, altura_patrulha_original)
	await get_tree().create_timer(0.3).timeout
	if estado == "preparando":
		estado = "atacando"
		anim.play("hit")

func ficar_no_chao():
	if estado in ["no_chao", "morto"]: return
	estado = "no_chao"
	anim.play("hit")
	await get_tree().create_timer(tempo_vulneravel).timeout
	if estado == "no_chao":
		estado = "retornando"
		anim.play("fly")

func iniciar_cooldown():
	pode_atacar = false
	await get_tree().create_timer(cooldown_ataque).timeout
	pode_atacar = true

# LÓGICA DE HIT ATUALIZADA
func _on_body_entered(body):
	if body.name == "bury":
		if body.has_method("tomar_dano"):
			# Passa 'self' para o player saber de onde veio o dano (Knockback)
			body.tomar_dano(1, self)
			
			# SE DER HIT NO PLAYER: Volta a voar imediatamente
			if estado == "atacando":
				estado = "retornando"
				anim.play("fly")
