extends Sprite2D

func _ready():
	# No Godot 4, usamos apenas create_tween()
	var tween = create_tween()
	
	# Faz o rastro ficar transparente (alpha 0.0) em 0.5 segundos
	tween.tween_property(self, "modulate:a", 0.0, 0.5)
	
	# Quando o rastro sumir totalmente, ele se deleta automaticamente
	tween.finished.connect(queue_free)
