extends Area2D

@export var dano: int = 1

func _on_body_entered(body: Node2D) -> void:
	if body.name == "bury":
		# Verificamos se o player tem a função tomar_dano
		if body.has_method("tomar_dano"):
			# Aplicamos o dano passando 'self' (o próprio espinho)
			# O script do Player agora resolve TUDO sozinho (morte, pulo, invulnerabilidade)
			body.tomar_dano(dano, self)
