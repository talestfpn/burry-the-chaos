extends CharacterBody2D

@export_group("Visual")
@export var animacao_fps: float = 5.0
@export var virar_sprite_ao_andar: bool = true
@export var nome_animacao_king_rat: String = "king_rat"

@export_group("Patrulha")
@export var patrulhar: bool = true
@export var velocidade: float = 25.0
@export var distancia_patrulha: float = 90.0
@export var comecar_para_esquerda: bool = true
@export var tempo_parado_ao_virar: float = 1.2

@export_group("Ataque")
@export var atacar: bool = true
@export var cena_tiro: PackedScene
@export var intervalo_ataque: float = 2.0
@export var frame_do_tiro: int = 12
@export var tempo_depois_do_tiro: float = 0.4
@export var velocidade_tiro: float = 90.0
@export var atacar_parado: bool = true
@export var reiniciar_animacao_ao_atacar: bool = true
@export var offset_tiro_x: float = 18.0
@export var offset_tiro_y: float = 0.0
@export var inverter_direcao_do_tiro: bool = true

@export_group("Física")
@export var usar_gravidade: bool = false
@export var gravidade: float = 900.0

@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var timer_ataque: Timer = $TimerAtaque

var posicao_inicial: Vector2
var direcao: int = -1
var esperando: bool = false
var atacando: bool = false


func _ready() -> void:
	posicao_inicial = global_position
	
	if comecar_para_esquerda:
		direcao = -1
	else:
		direcao = 1
	
	_configurar_animacao()
	_atualizar_sprite()
	_tocar_animacao_king_rat()
	
	timer_ataque.wait_time = intervalo_ataque
	timer_ataque.one_shot = false
	
	if not timer_ataque.timeout.is_connected(_on_timer_ataque_timeout):
		timer_ataque.timeout.connect(_on_timer_ataque_timeout)
	
	if atacar:
		timer_ataque.start()


func _physics_process(delta: float) -> void:
	if usar_gravidade and not is_on_floor():
		velocity.y += gravidade * delta
	
	if atacando and atacar_parado:
		velocity.x = 0
		move_and_slide()
		return
	
	if not patrulhar or esperando:
		velocity.x = 0
		_tocar_animacao_king_rat()
		move_and_slide()
		return
	
	velocity.x = direcao * velocidade
	
	_tocar_animacao_king_rat()
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
	if esperando or atacando:
		return
	
	esperando = true
	velocity.x = 0
	_tocar_animacao_king_rat()
	
	await get_tree().create_timer(tempo_parado_ao_virar).timeout
	
	# Se começou um ataque durante a espera, aguarda terminar antes de virar.
	while atacando:
		await get_tree().physics_frame
	
	direcao *= -1
	_atualizar_sprite()
	esperando = false


func _on_timer_ataque_timeout() -> void:
	if not atacar:
		return
	
	if atacando:
		return
	
	# Evita atacar exatamente no momento de virar a patrulha.
	if esperando:
		return
	
	_executar_ataque()


func _executar_ataque() -> void:
	atacando = true
	velocity.x = 0
	
	_atualizar_sprite()
	
	if reiniciar_animacao_ao_atacar:
		anim.stop()
		anim.frame = 0
	
	_tocar_animacao_king_rat()
	
	await _esperar_frame_do_tiro()
	
	# Pega a direção somente no momento real do disparo.
	var direcao_do_tiro: int = direcao
	
	if inverter_direcao_do_tiro:
		direcao_do_tiro *= -1
	
	_criar_tiro(direcao_do_tiro)
	
	await get_tree().create_timer(tempo_depois_do_tiro).timeout
	
	atacando = false


func _esperar_frame_do_tiro() -> void:
	if anim.sprite_frames == null:
		await get_tree().create_timer(0.5).timeout
		return
	
	if not anim.sprite_frames.has_animation(nome_animacao_king_rat):
		await get_tree().create_timer(0.5).timeout
		return
	
	var total_frames: int = anim.sprite_frames.get_frame_count(nome_animacao_king_rat)
	var frame_alvo: int = clampi(frame_do_tiro, 0, total_frames - 1)
	
	while anim.frame < frame_alvo:
		await anim.frame_changed


func _criar_tiro(direcao_do_tiro: int) -> void:
	if cena_tiro == null:
		print("ERRO: cena_tiro não foi definida no Inspector.")
		return
	
	var tiro: Node2D = cena_tiro.instantiate()
	
	tiro.set("direcao", direcao_do_tiro)
	tiro.set("velocidade", velocidade_tiro)
	tiro.global_position = global_position + Vector2(offset_tiro_x * direcao_do_tiro, offset_tiro_y)
	
	get_parent().add_child(tiro)


func _atualizar_sprite() -> void:
	if not virar_sprite_ao_andar:
		return
	
	if direcao < 0:
		anim.flip_h = true
	else:
		anim.flip_h = false


func _tocar_animacao_king_rat() -> void:
	if anim.sprite_frames == null:
		print("ERRO: AnimatedSprite2D está sem SpriteFrames.")
		return
	
	if not anim.sprite_frames.has_animation(nome_animacao_king_rat):
		print("ERRO: animação não encontrada: ", nome_animacao_king_rat)
		return
	
	if anim.animation != nome_animacao_king_rat:
		anim.play(nome_animacao_king_rat)
	elif not anim.is_playing():
		anim.play(nome_animacao_king_rat)


func _configurar_animacao() -> void:
	if anim.sprite_frames == null:
		return
	
	if anim.sprite_frames.has_animation(nome_animacao_king_rat):
		anim.sprite_frames.set_animation_speed(nome_animacao_king_rat, animacao_fps)
