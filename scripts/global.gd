extends Node

signal moedas_alteradas(total: int)

var moedas: int = 0

func adicionar_moedas(quantidade: int = 1) -> void:
	moedas += quantidade
	moedas_alteradas.emit(moedas)

func gastar_moedas(quantidade: int) -> bool:
	if moedas >= quantidade:
		moedas -= quantidade
		moedas_alteradas.emit(moedas)
		return true
	
	return false

func resetar_moedas() -> void:
	moedas = 0
	moedas_alteradas.emit(moedas)
