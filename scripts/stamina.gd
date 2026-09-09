extends TextureProgressBar

var bury: CharacterBody2D 
var stamina_anterior: float = 100.0 # Ajuda a barra a lembrar quanto ela tinha antes

func _ready() -> void:
	max_value = 100.0
	value = 100.0
	if texture_under == null:
		texture_under = texture_progress

	tint_under = Color.BLACK

	bury = get_tree().get_first_node_in_group("Jogador")
	
	if bury == null:
		print("Aviso: Jogador não encontrado no grupo 'Jogador'!")

func _process(_delta: float) -> void:
	if bury:
		value = bury.stamina
		
		# A MÁGICA DO GAME FEEL AQUI:
		# Se antes não tava cheia, e agora bateu 100, pisca!
		if stamina_anterior < 100.0 and bury.stamina >= 100.0:
			_piscar_brilho()
			
		# Atualiza a memória da barra pro próximo frame
		stamina_anterior = bury.stamina

func _piscar_brilho() -> void:
	# Cria a animação de brilho na própria barra
	var tween = get_tree().create_tween()
	# Fica super branca/brilhante (o 2.5 faz a cor estourar)
	tween.tween_property(self, "modulate", Color(2.5, 2.5, 2.5, 1.0), 0.05)
	# Volta pra cor normal
	tween.tween_property(self, "modulate", Color.WHITE, 0.2)
