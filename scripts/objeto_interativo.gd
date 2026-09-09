extends Area2D

# --- Configurações que aparecem no Inspetor ---
@export_group("Textos e Animações")
@export_multiline var texto_diálogo: String = "Lorem ipsum dolor sit amet..."
@export var animacao_parado: String = "desligado" 
@export var animacao_interagindo: String = "ligado" 

@export_group("Comportamento")
@export var tipo_interruptor: bool = false 
@export var interacao_unica: bool = false 
@export var dar_passinho: bool = true 

@export_group("Nós Filhos (Arraste aqui)")
@export var sprite_objeto: AnimatedSprite2D
@export var chat_box: Control # <-- Arraste o nó PANEL (que agora tá no CanvasLayer) pra cá!
@export var label_texto: Label

# --- Variáveis de Controle ---
var player_perto: bool = false
var player_node: CharacterBody2D = null
var interagindo: bool = false
var estado_ligado: bool = false 
var tween_dialogo: Tween 

# Posições para o efeito de deslize
var posicao_visivel: float
var posicao_escondida: float

func _ready() -> void:
	if chat_box:
		# Salva a posição perfeita que você deixou no editor
		posicao_visivel = chat_box.position.y
		# Calcula a posição fora da tela (empurrando para baixo)
		posicao_escondida = posicao_visivel + chat_box.size.y + 50.0
		
		# Joga a caixa lá pra baixo da tela logo no início
		chat_box.position.y = posicao_escondida
		chat_box.show() # Mantém "visível", mas escondido fora da tela
	
	if sprite_objeto and animacao_parado != "":
		sprite_objeto.play(animacao_parado)
	
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)

func _on_body_entered(body: Node2D) -> void:
	if body.name == "bury":
		player_perto = true
		player_node = body

func _on_body_exited(body: Node2D) -> void:
	if body.name == "bury":
		if tipo_interruptor:
			player_perto = false
			player_node = null
			return
			
		if interagindo:
			return
			
		player_perto = false
		player_node = null

func _input(event: InputEvent) -> void:
	# Quando apertar o botão de interação...
	if event.is_action_pressed("interact") and player_perto and player_node != null:
		
		if interacao_unica and estado_ligado:
			return 
		
		if tipo_interruptor:
			acionar_interruptor()
		else:
			# Se JÁ estiver interagindo, o botão serve para FECHAR!
			if interagindo:
				encerrar_interacao()
			# Se não estiver, o botão serve para ABRIR!
			elif player_node.is_physics_processing():
				iniciar_interacao()

func acionar_interruptor() -> void:
	if interacao_unica:
		estado_ligado = true
	else:
		estado_ligado = !estado_ligado 
	
	if estado_ligado:
		if sprite_objeto and animacao_interagindo != "":
			sprite_objeto.play(animacao_interagindo)
	else:
		if sprite_objeto and animacao_parado != "":
			sprite_objeto.play(animacao_parado)
			
	var direcao_olhar = -1 if player_node.global_position.x > global_position.x else 1
	player_node.facing_direction = direcao_olhar
	if player_node.has_node("AnimatedSprite2D"):
		player_node.get_node("AnimatedSprite2D").flip_h = (direcao_olhar < 0)

func iniciar_interacao() -> void:
	estado_ligado = true 
	
	var player_atual = player_node 
	if player_atual == null: return 
	
	interagindo = true
	
	player_atual.set_physics_process(false)
	player_atual.velocity = Vector2.ZERO
	var anim_player = player_atual.get_node("AnimatedSprite2D")
	
	# --- CONTROLE DO PASSINHO ---
	if dar_passinho:
		var direcao_passo = -player_atual.facing_direction 
		var distancia = 24.0 
		var posicao_alvo = player_atual.global_position.x + (direcao_passo * distancia)
		
		anim_player.play("walk")
		var tween = get_tree().create_tween()
		tween.tween_property(player_atual, "global_position:x", posicao_alvo, 0.25)
		await tween.finished
		
		anim_player.play("idle")
		player_atual.facing_direction = -direcao_passo 
		anim_player.flip_h = (player_atual.facing_direction < 0)
	else:
		var direcao_olhar = -1 if player_atual.global_position.x > global_position.x else 1
		anim_player.play("idle")
		player_atual.facing_direction = direcao_olhar
		anim_player.flip_h = (direcao_olhar < 0)

	if label_texto:
		label_texto.text = texto_diálogo
		
	# --- ANIMAÇÃO DE DESLIZE (SLIDE UP - ELASTIC) ---
	if chat_box:
		if tween_dialogo and tween_dialogo.is_valid():
			tween_dialogo.kill()
			
		# Usa a curva elástica para dar aquele efeitinho de mola legal
		tween_dialogo = get_tree().create_tween().set_trans(Tween.TRANS_SPRING).set_ease(Tween.EASE_OUT)
		tween_dialogo.tween_property(chat_box, "position:y", posicao_visivel, 0.5)
		
	if sprite_objeto and animacao_interagindo != "":
		sprite_objeto.play(animacao_interagindo)

func encerrar_interacao() -> void:
	interagindo = false
	
	# --- ANIMAÇÃO DE FECHAR (SLIDE DOWN) ---
	if chat_box:
		if tween_dialogo and tween_dialogo.is_valid():
			tween_dialogo.kill()
			
		tween_dialogo = get_tree().create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
		tween_dialogo.tween_property(chat_box, "position:y", posicao_escondida, 0.25)
		
	if sprite_objeto and animacao_parado != "" and not interacao_unica:
		sprite_objeto.play(animacao_parado)
		
	if player_node != null:
		player_node.set_physics_process(true)
		
		if not overlaps_body(player_node):
			player_perto = false
			player_node = null
