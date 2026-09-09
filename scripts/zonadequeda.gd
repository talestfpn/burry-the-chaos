extends Area2D

func _on_body_entered(body: Node2D) -> void:
	if body.name == "bury":
		body.global_position = Vector2(1429.0, 100.0)
		
		# Zera a velocidade atual
		if "velocity" in body:
			body.velocity = Vector2.ZERO
			
			# Aplica uma leve força para cima (efeito paraqueda/balão)
			# para a gravidade demorar mais a puxar ele de volta
			body.velocity.y = -100.0
		
		if body.has_node("AnimatedSprite2D"):
			body.get_node("AnimatedSprite2D").play("jump")
