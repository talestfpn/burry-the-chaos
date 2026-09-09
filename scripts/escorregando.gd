extends Area2D

@export var direcao_da_descida: float = 1.0 # 1.0 pra direita, -1.0 pra esquerda

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if body.name == "bury":
		# Confere se ele tem a nova função que a gente criou
		if body.has_method("forcar_escorregamento"):
			# Manda o Bury jogar o teclado pela janela e descer!
			body.forcar_escorregamento(direcao_da_descida)
