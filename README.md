# Burry The Chaos

Projeto Godot 4 Mono/C# de `Burry The Chaos`.

## Abrir e executar

1. Abra a pasta no Godot 4.7.1 Mono.
2. Aguarde a importação dos assets.
3. Execute `scene/main_menu.tscn`.

O projeto preserva a composição visual em cenas, nós, TileSets, Resources e sinais. A lógica de comportamento está nos scripts C# em `scripts/`.

## Build

```text
dotnet build --no-restore
```

Os caches, imports gerados, logs e capturas locais ficam fora do controle de versão.
