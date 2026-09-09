extends CharacterBody2D

# --- Configurações de Habilidades ---
@export_group("Habilidades Liberadas")
@export var pode_pular: bool = true
@export var pode_pulo_duplo: bool = true
@export var pode_atacar: bool = true
@export var pode_dash: bool = true

# --- Ajustes de Pulo ---
@export_group("Ajustes de Pulo")
@export var forca_do_pulo: float = -300.0

# --- Ajustes Cinematográficos ---
@export_group("Cenas e Ladeiras")
@export var velocidade_escorregamento: float = 150.0

# --- Ajustes de Parede ---
@export_group("Ajustes de Parede")
@export var velocidade_escalada_parede: float = 60.0
@export var velocidade_deslize_parede: float = 80.0
@export var offset_visual_parede_x: float = 2.0
@export var offset_visual_parede_y: float = 0.0
@export var gasto_stamina_parede: float = 20.0

# --- Valores de Movimento ---
const SPEED = 85.0
const DASH_VELOCITY = 240.0
const DASH_DURATION = 0.2
const STAMINA_MAXIMA = 100.0
const CUSTO_DO_DASH = 50.0
const TEMPO_DE_RECARGA = 1.5
const VIDA_MAXIMA = 4
const DANO_ATAQUE = 1
const FORCA_KNOCKBACK = 120.0
const FORCA_POGO = -250.0 

# --- Ajustes de Game Feel ---
const COYOTE_TIME_MAX = 0.15
const JUMP_BUFFER_MAX = 0.12

# --- Ajustes Fixos da Parede ---
const WALL_JUMP_PUSH = 180.0
const MOMENTUM_FRICTION = 15.0
const WALL_STAMINA_MAX = 2.0    

# --- Nodes ---
@onready var anim: AnimatedSprite2D = $AnimatedSprite2D
@onready var colisao_normal: CollisionShape2D = $ColisaoNormal
@onready var colisao_dash: CollisionShape2D = $ColisaoDash
@onready var teto_check: RayCast2D = $TetoCheck
@onready var hitbox_ataque: Area2D = $HitboxAtaque
@onready var hitbox_shape: CollisionShape2D = $HitboxAtaque/CollisionShape2D
@onready var taxa_de_recarga: float = STAMINA_MAXIMA / TEMPO_DE_RECARGA

# --- Estados Internos ---
var stamina: float = 100.0
var vida_atual: int = VIDA_MAXIMA
var facing_direction: int = 1
var dash_direction: Vector2 = Vector2.ZERO

var is_dashing: bool = false
var esta_atacando: bool = false
var esta_invulneravel: bool = false
var esta_em_knockback: bool = false
var deu_pulo_duplo: bool = false
var bloqueio_animacao: bool = false
var esta_morto: bool = false 
var em_hitstop: bool = false 

var ataque_id: int = 0
var dash_id: int = 0

# --- ESTADOS CINEMATOGRÁFICOS ---
var cena_escorregando: bool = false
var direcao_escorregamento: float = 1.0

# --- Timers Internos ---
var coyote_timer: float = 0.0
var jump_buffer_timer: float = 0.0
var wall_folego: float = WALL_STAMINA_MAX 

# ------------------------------------------------------------------------------

func _ready() -> void:
	hitbox_shape.set_deferred("disabled", true)
	anim.animation_finished.connect(_on_animacao_terminou)
	
	var health_node = get_node_or_null("%healthbar")
	if health_node:
		health_node.max_value = VIDA_MAXIMA
		health_node.value = vida_atual


func _physics_process(delta: float) -> void:
	if esta_morto:
		if not is_on_floor():
			velocity += get_gravity() * delta
		else:
			velocity.x = move_toward(velocity.x, 0, MOMENTUM_FRICTION)
		move_and_slide()
		return

	# --- Sequestro de Controle na Ladeira ---
	if cena_escorregando:
		_aplicar_gravidade(delta)
		
		velocity.x = direcao_escorregamento * velocidade_escorregamento
		velocity.y += 150.0
		
		anim.offset = Vector2.ZERO
		
		if direcao_escorregamento < 0:
			_tocar_animacao_direta("personagem_escorregando_esq", "personagem_escorregando")
		else:
			_tocar_animacao_direta("personagem_escorregando", "personagem_escorregando")
		
		move_and_slide()
		return

	_atualizar_timers(delta)
	_recuperar_stamina(delta)
	_processar_stamina_parede(delta)
	_processar_inputs()
	
	if is_dashing:
		_processar_dash_fisica()
		hitbox_ataque.scale.x = facing_direction
		return
	
	_aplicar_gravidade(delta)
	_processar_movimento(delta)
	
	hitbox_ataque.scale.x = facing_direction
	move_and_slide()


# --- Função que o gatilho da cena vai chamar ---
func forcar_escorregamento(direcao_da_ladeira: float) -> void:
	cena_escorregando = true
	direcao_escorregamento = direcao_da_ladeira
	is_dashing = false 
	hitbox_shape.set_deferred("disabled", true) 


# --- Funções de Animação ---
func _animacao_para_direcao(nome_base: String) -> String:
	if facing_direction < 0:
		var nome_esq := nome_base + "_esq"
		
		if anim.sprite_frames and anim.sprite_frames.has_animation(nome_esq):
			return nome_esq
	
	return nome_base


func _tocar_animacao(nome_base: String) -> void:
	anim.flip_h = false
	anim.play(_animacao_para_direcao(nome_base))


func _tocar_animacao_direta(nome_desejado: String, fallback: String) -> void:
	anim.flip_h = false
	
	if anim.sprite_frames and anim.sprite_frames.has_animation(nome_desejado):
		anim.play(nome_desejado)
	else:
		anim.play(fallback)


func _forcar_animacao_pos_dash() -> void:
	if esta_morto:
		return
	
	if is_on_floor():
		var direction := Input.get_axis("left", "right")
		
		if direction != 0:
			_tocar_animacao("walk")
		elif Input.is_action_pressed("up"):
			_tocar_animacao_direta("look_up", "look_up")
		else:
			_tocar_animacao("idle")
	else:
		_tocar_animacao("jump")


# --- Gerencia tempos e fôlego ---
func _atualizar_timers(delta: float) -> void:
	if is_on_floor():
		coyote_timer = COYOTE_TIME_MAX
		wall_folego = WALL_STAMINA_MAX
		deu_pulo_duplo = false
	else:
		coyote_timer -= delta
		
	if Input.is_action_just_pressed("jump"):
		jump_buffer_timer = JUMP_BUFFER_MAX
	else:
		jump_buffer_timer -= delta


func _processar_stamina_parede(delta: float) -> void:
	var direction := Input.get_axis("left", "right")
	var normal_parede: float = get_wall_normal().x
	
	if is_on_wall_only() and direction != 0 and sign(direction) == -sign(normal_parede):
		stamina -= gasto_stamina_parede * delta
		if stamina < 0: 
			stamina = 0
	
	_atualizar_UI()


# --- Lógica de Gravidade e Deslize ---
func _aplicar_gravidade(delta: float) -> void:
	if not is_on_floor():
		var direcao := Input.get_axis("left", "right")
		
		if is_on_wall_only() and direcao != 0 and stamina > 0:
			var normal_parede: float = get_wall_normal().x
			
			if sign(direcao) == -sign(normal_parede):
				if Input.is_action_pressed("up"):
					velocity.y = -velocidade_escalada_parede 
				elif Input.is_action_pressed("down"):
					velocity.y = velocidade_deslize_parede 
				else:
					velocity.y = 0.0 
			else:
				velocity += get_gravity() * delta
		else:
			velocity += get_gravity() * delta


func _processar_inputs() -> void:
	if esta_em_knockback:
		return

	if not is_dashing:
		if pode_pular and jump_buffer_timer > 0:
			if coyote_timer > 0:
				_executar_pulo()
			elif is_on_wall_only() and stamina > 0:
				_executar_wall_jump()
			elif pode_pulo_duplo and not deu_pulo_duplo:
				_executar_pulo_duplo()
		
		if pode_dash and Input.is_action_just_pressed("dash") and stamina >= CUSTO_DO_DASH:
			executar_dash()
	
	# Não permite usar espada enquanto estiver na parede.
	if pode_atacar and Input.is_action_just_pressed("attack") and not esta_atacando and not is_on_wall_only():
		executar_ataque()


func _executar_pulo() -> void:
	velocity.y = forca_do_pulo
	jump_buffer_timer = 0
	coyote_timer = 0


func _executar_wall_jump() -> void:
	var wall_normal := get_wall_normal()
	velocity.y = forca_do_pulo
	velocity.x = wall_normal.x * WALL_JUMP_PUSH
	facing_direction = sign(wall_normal.x)
	jump_buffer_timer = 0
	deu_pulo_duplo = false


func _executar_pulo_duplo() -> void:
	velocity.y = forca_do_pulo
	jump_buffer_timer = 0
	deu_pulo_duplo = true
	anim.stop()
	_tocar_animacao("jump")


func _processar_movimento(_delta: float) -> void:
	if esta_em_knockback or is_dashing:
		return
	
	var direction := Input.get_axis("left", "right")
	var speed_multiplier: float = 0.5 if esta_atacando else 1.0
	var target_speed: float = direction * (SPEED * speed_multiplier)
	
	anim.offset = Vector2.ZERO
	
	if direction != 0:
		if abs(velocity.x) > SPEED and sign(velocity.x) == direction and not is_on_floor():
			velocity.x = move_toward(velocity.x, target_speed, MOMENTUM_FRICTION)
		else:
			velocity.x = target_speed 
			
		facing_direction = sign(direction)
		
		if is_on_floor() and not esta_atacando and not bloqueio_animacao:
			_tocar_animacao("walk")
	else:
		var friction: float = SPEED if is_on_floor() else MOMENTUM_FRICTION
		velocity.x = move_toward(velocity.x, 0, friction)
		
		if is_on_floor() and not esta_atacando and not bloqueio_animacao:
			if Input.is_action_pressed("up"):
				_tocar_animacao_direta("look_up", "look_up")
			else:
				_tocar_animacao("idle")
	
	if not is_on_floor() and not esta_atacando and not bloqueio_animacao:
		if is_on_wall_only() and direction != 0 and stamina > 0:
			var wall_normal: float = get_wall_normal().x
			
			if sign(direction) == -sign(wall_normal):
				var esta_subindo_ou_descendo: bool = Input.is_action_pressed("up") or Input.is_action_pressed("down")
				
				if wall_normal > 0:
					anim.offset = Vector2(offset_visual_parede_x, offset_visual_parede_y)
					
					if esta_subindo_ou_descendo:
						_tocar_animacao_direta("personagem_parede_esq", "personagem_parede")
					else:
						_tocar_animacao_direta("personagem_parede_idle_esq", "personagem_parede_idle")
				
				elif wall_normal < 0:
					anim.offset = Vector2(-offset_visual_parede_x, offset_visual_parede_y)
					
					if esta_subindo_ou_descendo:
						_tocar_animacao_direta("personagem_parede", "personagem_parede")
					else:
						_tocar_animacao_direta("personagem_parede_idle", "personagem_parede")
			else:
				_tocar_animacao("jump")
		else:
			_tocar_animacao("jump")


# --- Sistema de Dash ---
func executar_dash() -> void:
	if is_dashing:
		return
	
	is_dashing = true
	dash_id += 1
	var meu_dash_id := dash_id
	
	stamina -= CUSTO_DO_DASH
	
	# Se estiver na parede, o dash sempre sai para o lado oposto da parede.
	if is_on_wall_only():
		var wall_normal := get_wall_normal()
		dash_direction = Vector2(wall_normal.x, 0).normalized()
	else:
		var input_dir := Input.get_vector("left", "right", "up", "down")
		dash_direction = input_dir.normalized() if input_dir != Vector2.ZERO else Vector2(facing_direction, 0)
	
	if dash_direction.x != 0:
		facing_direction = sign(dash_direction.x)
	
	anim.offset = Vector2.ZERO
	
	if not esta_atacando:
		_tocar_animacao("dash")
	
	colisao_normal.set_deferred("disabled", true)
	colisao_dash.set_deferred("disabled", false)
	
	criar_fantasma()
	if has_node("TimerRastro"):
		$TimerRastro.start()
	
	await get_tree().create_timer(DASH_DURATION).timeout
	
	if meu_dash_id != dash_id:
		return
	
	while teto_check.is_colliding():
		await get_tree().physics_frame
	
	if meu_dash_id != dash_id:
		return
	
	finalizar_dash()


func _processar_dash_fisica() -> void:
	velocity = dash_direction * DASH_VELOCITY
	move_and_slide()


func finalizar_dash() -> void:
	if not is_dashing:
		return
	
	is_dashing = false
	
	if has_node("TimerRastro"):
		$TimerRastro.stop()
	
	colisao_normal.set_deferred("disabled", false)
	colisao_dash.set_deferred("disabled", true)
	
	var direction := Input.get_axis("left", "right")
	velocity.x = direction * SPEED
	
	if velocity.y < 0:
		velocity.y = 0 
	
	if not esta_atacando and not esta_morto:
		_forcar_animacao_pos_dash()


# --- Combate e Dano ---
func executar_ataque() -> void:
	if esta_atacando:
		return
	
	if is_on_wall_only():
		return
	
	esta_atacando = true
	ataque_id += 1
	var meu_ataque_id := ataque_id
	
	var animacao_escolhida := "attack"
	var posicao_hitbox := Vector2.ZERO
	var segurando_lado: bool = Input.is_action_pressed("left") or Input.is_action_pressed("right")
	
	if Input.is_action_pressed("left"):
		facing_direction = -1
	elif Input.is_action_pressed("right"):
		facing_direction = 1
	
	if not is_on_floor() and Input.is_action_pressed("down"):
		animacao_escolhida = "attack_down"
		posicao_hitbox = Vector2(0, 7.0) 
			
	elif Input.is_action_pressed("up"):
		if segurando_lado:
			animacao_escolhida = "attack_diag"
			posicao_hitbox = Vector2(0.2 * facing_direction, -7) 
		else:
			animacao_escolhida = "attack_up"
			posicao_hitbox = Vector2(0, -7) 
			
	else:
		animacao_escolhida = "attack"
		posicao_hitbox = Vector2(1.0 * facing_direction, 0) 
		
	anim.offset = Vector2.ZERO
	_tocar_animacao(animacao_escolhida)
	
	hitbox_ataque.position = posicao_hitbox
	hitbox_shape.set_deferred("disabled", false)
	
	await get_tree().physics_frame
	await get_tree().physics_frame
	
	if meu_ataque_id != ataque_id:
		return
	
	_detectar_colisao_ataque(animacao_escolhida)
	
	var duracao: float = _pegar_duracao_animacao(_animacao_para_direcao(animacao_escolhida))
	await get_tree().create_timer(duracao).timeout
	
	if meu_ataque_id == ataque_id:
		_finalizar_ataque()


func _finalizar_ataque() -> void:
	esta_atacando = false
	hitbox_shape.set_deferred("disabled", true)
	hitbox_ataque.position = Vector2.ZERO
	
	if not is_dashing and not esta_morto:
		_forcar_animacao_pos_dash()


func _pegar_duracao_animacao(nome_animacao: String) -> float:
	if anim.sprite_frames == null:
		return 0.25
	
	if not anim.sprite_frames.has_animation(nome_animacao):
		return 0.25
	
	var quantidade_frames: int = anim.sprite_frames.get_frame_count(nome_animacao)
	var fps: float = anim.sprite_frames.get_animation_speed(nome_animacao)
	
	if fps <= 0:
		return 0.25
	
	return max(quantidade_frames / fps, 0.08)


func _detectar_colisao_ataque(animacao_escolhida: String) -> void:
	var acertou_algo := false
	
	for body in hitbox_ataque.get_overlapping_bodies():
		if body != self and body.has_method("tomar_dano"):
			body.tomar_dano(DANO_ATAQUE, self)
			acertou_algo = true
			
	for area in hitbox_ataque.get_overlapping_areas():
		if area.has_method("tomar_dano"):
			area.tomar_dano(DANO_ATAQUE, self)
			acertou_algo = true
			
	if acertou_algo:
		aplicar_hitstop_e_shake()
		
		if animacao_escolhida == "attack_down":
			velocity.y = FORCA_POGO


# --- GAME FEEL: O Peso do Combate ---
func aplicar_hitstop_e_shake() -> void:
	if em_hitstop:
		return 
	
	em_hitstop = true
	Engine.time_scale = 0.05
	
	var camera = $Camera2D 
	
	if camera:
		camera.offset = Vector2(randf_range(-6.0, 6.0), randf_range(-6.0, 6.0))
		
	await get_tree().create_timer(0.04, true, false, true).timeout
	
	Engine.time_scale = 1.0
	
	if camera:
		camera.offset = Vector2.ZERO
		
	em_hitstop = false 


func tomar_dano(quantidade: int, origem: Node2D = null) -> void:
	if esta_invulneravel or esta_morto:
		return 
	
	if is_dashing:
		finalizar_dash()
	
	vida_atual -= quantidade
	_atualizar_UI()
	
	if vida_atual <= 0:
		_morrer() 
	else:
		if origem:
			aplicar_knockback(origem.global_position)
		_iniciar_invulnerabilidade()


# --- Morte Definitiva ---
func _morrer() -> void:
	if esta_morto:
		return 
	
	esta_morto = true
	esta_invulneravel = true 
	
	Engine.time_scale = 1.0 
	
	hitbox_shape.set_deferred("disabled", true)
	
	velocity.y = -250
	velocity.x = -facing_direction * 50 
	
	anim.offset = Vector2.ZERO
	anim.stop() 
	_tocar_animacao("personagem_morte")
	
	await anim.animation_finished
	get_tree().reload_current_scene()


func aplicar_knockback(posicao_origem: Vector2) -> void:
	esta_em_knockback = true
	var direcao: int = 1 if global_position.x > posicao_origem.x else -1
	velocity = Vector2(direcao * FORCA_KNOCKBACK, -80)
	await get_tree().create_timer(0.2).timeout
	esta_em_knockback = false


# --- Funções Auxiliares ---
func _recuperar_stamina(delta: float) -> void:
	if stamina < STAMINA_MAXIMA and is_on_floor() and not is_dashing:
		stamina = move_toward(stamina, STAMINA_MAXIMA, taxa_de_recarga * delta)
		
		if stamina == STAMINA_MAXIMA:
			_piscar_dash_pronto()


func _piscar_dash_pronto() -> void:
	if not esta_invulneravel:
		var tween_bury = get_tree().create_tween()
		tween_bury.tween_property(anim, "modulate", Color(2.0, 2.0, 2.0, 1.0), 0.05)
		tween_bury.tween_property(anim, "modulate", Color.WHITE, 0.2)


func _atualizar_UI() -> void:
	var health_node = get_node_or_null("%healthbar")
	if health_node and "value" in health_node:
		health_node.value = vida_atual
	
	var stamina_node = get_node_or_null("%staminabar")
	if stamina_node:
		if "value" in stamina_node:
			stamina_node.value = stamina


func _iniciar_invulnerabilidade() -> void:
	esta_invulneravel = true
	anim.modulate = Color(1, 0.3, 0.3)
	await get_tree().create_timer(0.5).timeout
	anim.modulate = Color.WHITE
	esta_invulneravel = false


func criar_fantasma() -> void:
	var fantasma = Sprite2D.new()
	fantasma.texture = anim.sprite_frames.get_frame_texture(anim.animation, anim.frame)
	fantasma.global_position = anim.global_position
	fantasma.flip_h = false
	fantasma.scale = anim.scale
	fantasma.modulate = Color(0.17, 0.31, 0.69, 0.7)
	get_parent().add_child(fantasma)
	
	var tween = get_tree().create_tween()
	tween.tween_property(fantasma, "modulate:a", 0.0, 0.3)
	tween.finished.connect(fantasma.queue_free)


func _on_animacao_terminou() -> void:
	if anim.animation.begins_with("attack"):
		ataque_id += 1
		_finalizar_ataque()


func _on_timer_rastro_timeout() -> void:
	if is_dashing:
		criar_fantasma()
