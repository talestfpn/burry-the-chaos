extends Control

@onready var label_moedas: Label = $LabelMoedas
@onready var icone_moeda: AnimatedSprite2D = $IconeMoedaAnimada

func _ready() -> void:
	icone_moeda.play("girar")
	
	Global.moedas_alteradas.connect(_atualizar_moedas)
	_atualizar_moedas(Global.moedas)

func _atualizar_moedas(total: int) -> void:
	label_moedas.text = "x " + str(total)
	_animar_contador()

func _animar_contador() -> void:
	var tween = get_tree().create_tween()
	
	scale = Vector2(1.15, 1.15)
	tween.tween_property(self, "scale", Vector2.ONE, 0.12)
