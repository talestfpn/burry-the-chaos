extends CharacterBody2D

@export_group("Visual")
@export_enum("branco", "cinza", "marrom") var tipo_rato: String = "marrom"
@export var animacao_fps: float = 8.0
@export var virar_sprite_ao_andar: bool = true

@export_group("Patrulha")
@export var patrulhar: bool = true
@export var velocidade: float = 35.0
@export var distancia_patrulha: float = 80.0
@export var comecar_para_esquerda: bool = true
@export var tempo_parado_ao_virar: float = 1.5

@export_group("Física")
@export var usar_gravidade: bool = true
@export var gravidade: float = 900.0

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D

var posicao_inicial: Vector2
var direcao: int = -1
var esperando: bool = false


func _ready() -> void:
	posicao_inicial = global_position
	
	if comecar_para_esquerda:
		direcao = -1
	else:
		direcao = 1
	
	_configurar_animacoes()
	_atualizar_sprite()
	_tocar_animacao_idle()


func _physics_process(delta: float) -> void:
	if usar_gravidade and not is_on_floor():
		velocity.y += gravidade * delta
	
	if not patrulhar or esperando:
		velocity.x = 0
		_tocar_animacao_idle()
		move_and_slide()
		return
	
	velocity.x = direcao * velocidade
	
	_tocar_animacao_walk()
	_atualizar_sprite()
	move_and_slide()
	
	_verificar_limite_patrulha()


func _verificar_limite_patrulha() -> void:
	var distancia_atual: float = absf(global_position.x - posicao_inicial.x)
	
	if distancia_atual >= distancia_patrulha:
		_parar_e_inverter_direcao()
		return
	
	if is_on_wall():
		_parar_e_inverter_direcao()
		return


func _parar_e_inverter_direcao() -> void:
	if esperando:
		return
	
	esperando = true
	velocity.x = 0
	_tocar_animacao_idle()
	
	await get_tree().create_timer(tempo_parado_ao_virar).timeout
	
	direcao *= -1
	_atualizar_sprite()
	esperando = false


func _atualizar_sprite() -> void:
	if not virar_sprite_ao_andar:
		return
	
	if direcao < 0:
		anim.flip_h = true
	else:
		anim.flip_h = false


func _tocar_animacao_idle() -> void:
	var nome_animacao: String = tipo_rato + "_idle"
	_tocar_animacao_segura(nome_animacao)


func _tocar_animacao_walk() -> void:
	var nome_animacao: String = tipo_rato + "_walk"
	_tocar_animacao_segura(nome_animacao)


func _tocar_animacao_segura(nome_animacao: String) -> void:
	if anim.sprite_frames == null:
		print("ERRO: AnimatedSprite2D está sem SpriteFrames.")
		return
	
	if not anim.sprite_frames.has_animation(nome_animacao):
		print("ERRO: animação não encontrada: ", nome_animacao)
		return
	
	if anim.animation != nome_animacao:
		anim.play(nome_animacao)


func _configurar_animacoes() -> void:
	if anim.sprite_frames == null:
		return
	
	var animacoes: Array[String] = [
		"branco_idle",
		"branco_walk",
		"cinza_idle",
		"cinza_walk",
		"marrom_idle",
		"marrom_walk"
	]
	
	for nome: String in animacoes:
		if anim.sprite_frames.has_animation(nome):
			anim.sprite_frames.set_animation_speed(nome, animacao_fps)
