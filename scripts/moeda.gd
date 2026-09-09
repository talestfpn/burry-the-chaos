extends Area2D

@export var valor: int = 1

var coletada: bool = false

func _ready() -> void:
	body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
	if coletada:
		return
	
	if not body.is_in_group("Jogador"):
		return
	
	coletada = true
	
	Global.adicionar_moedas(valor)
	
	monitoring = false
	set_deferred("monitorable", false)
	
	_animar_coleta()

func _animar_coleta() -> void:
	var tween = get_tree().create_tween()
	
	tween.parallel().tween_property(self, "position:y", position.y - 10, 0.18)
	tween.parallel().tween_property(self, "scale", Vector2(1.3, 1.3), 0.08)
	tween.parallel().tween_property(self, "modulate:a", 0.0, 0.18)
	
	await tween.finished
	queue_free()
