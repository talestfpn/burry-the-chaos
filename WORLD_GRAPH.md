# Etapa 3 — Mundo da descida

## Grafo implementado

```text
Quarto ⇄ RuaCasa
           │ queda / escorregamento
           ▼
       Topo do Prédio ⇄ Terraço secreto
           │ espada + entrada do 7º
           ▼
       7º — Átrio ───── Arquivo [insígnia do esgoto]
           │                  └ núcleo de serviço
           ▼
       6º — Manutenção ─ Abrigo / zelador [núcleo]
           │ confronto             └ passagem reparada → 5º
           ▼
       5º — Condenado ─ Sala inundada [fusível do subsolo]
           │ confronto + queda
           ▼
       Subsolo — Casa de Bombas
           │ confronto
           ▼
       Esgoto — Coletor / Portão
           │ confronto
           ▼
       Arena do Rei dos Ratos [preparada; boss não implementado]

Elevador central: Topo ⇄ 7º ⇄ 6º ⇄ 5º ⇄ Subsolo ⇄ Esgoto
                 Somente destinos visitados ficam disponíveis.
Cada região grande: galerias superiores + corredor de serviço com
escotilhas destravadas pelo lado inferior + pontos de descanso.
```

O acesso externo do topo retorna à RuaCasa; a arena retorna ao coletor. A queda do 5º não exige refazer o precipício para voltar: há elevador central nas profundezas. Uma queda antecipada, antes do confronto do 5º, tem retorno de manutenção na plataforma inferior.

## Escala e composição

Medidas em pixels do mundo, não da janela. Viewport 320×180, janela inicial 960×540, escala de janela inteira. As novas câmeras usam zoom 1,5 e limites próprios; o moveset não foi ampliado para compensar distâncias.

| Cena / região | Dimensões | Estrutura / marco |
|---|---:|---|
| Quarto | layout original | Cama, despertar, objetos interativos e escada; sem combate, pulo ou dash. |
| RuaCasa | original (~1030×520) | Percurso doméstico e queda pela água; mesmas restrições do quarto. |
| Topo | 1600×640 | Chegada por queda, degrau de pulo, passagem baixa de dash, travessia combinada, espada e piso de recuperação com escadas. |
| 7º / bury_the_chaos | 1920×800 | Três corredores, átrio das antenas, galerias e arquivo lacrado; ~5.320 px entre marcadores do percurso principal. |
| 6º / predio | 2560×896 | Três níveis, elevador interrompido, oficinas, abrigo e zelador; percurso marcado ~7.373 px. |
| 5º / quinto_andar | 2560×1120 | Três níveis, moradias abandonadas, infiltração e fratura final até a cota 1000; ~7.221 px antes da queda. |
| Profundezas | 2304×1376 | Cinco níveis, poço de bombas, dois circuitos de serviço, condutos e reservatórios; ~11.341 px. |
| Portão / esgoto | 2816×1120 | Quatro níveis, ninhos, solo, água contaminada, raízes e limiar monumental; ~11.134 px. |
| Arena final | 960×400 | Entrada, espaço de confronto, trono e marcador KingRatSpawn; sem boss final ativo. |
| Arquivo | 640×256 | Recompensa que alimenta a missão da manutenção. |
| Abrigo | 480×256 | Área lateral segura, mobiliário e respiro. |
| Sala inundada | 768×256 | Sala opcional aberta pelo fusível; recompensa de exploração. |
| Terraço secreto | 640×180 | Antena, moedas e plataforma opcional; retorna ao topo, sem pular o 7º. |

As distâncias são geométricas, não duração de uma partida. Não foi estabelecida uma estimativa de duração humana.

## Progressão, gates e backtracking

- A queda da RuaCasa chega ao topo, nunca diretamente ao 7º.
- No topo, `dash_lesson` libera dash; a espada define `sword`. O ataque acompanha essa flag também no terraço secreto.
- As saídas do 6º, 5º, subsolo e esgoto dependem de `encounter_6`, `encounter_5`, `encounter_depths` e `encounter_sewer`. São confrontos com inimigos existentes, não bosses novos.
- `visit_<cena>` habilita o respectivo botão do elevador. Não existe seleção irrestrita de fases.
- `sigil_sewer`: galeria opcional do coletor; abre o arquivo visto no 7º.
- `lift_core`: recompensa do arquivo; entregue ao zelador do 6º, define `workshop_repaired` e libera a passagem para o 5º.
- `pump_fuse`: galeria das bombas; permite entrar na sala inundada do 5º e obter `drained_room`.
- `shortcut_<cena>`: destrava escotilhas físicas e escada de serviço pelo lado inferior. As duas últimas regiões têm circuitos intermediários adicionais.
- Descansar recupera vida/stamina e registra um checkpoint de recarga da sala.
- Itens-chave, encontros vencidos, visitas, atalhos e checkpoints persistem na sessão e entre recargas. **Ainda não há gravação em disco.** Novo jogo limpa o estado; inimigos comuns e moedas reaparecem ao recarregar a sala.

Degraus, antenas, moedas e nichos dão pistas para as galerias. Gatos repetidos, uma cama deslocada no coletor e falas curtas no quarto mantêm a estranheza sem explicar sua natureza.

## Dificuldade e degradação

O 7º apresenta encontros espaçados, dano baixo e maior vulnerabilidade. O 6º aumenta a densidade e termina com dois guardas. O 5º combina obstáculos, espinhos e inimigo aéreo no confronto. Subsolo e coletor aumentam extensão, pressão e combinações. As caveiras mantêm 3 de vida; ataque e vulnerabilidade são definidos por instância, sem aumento generalizado de HP.

Os ratos existentes são fauna ambiental, não um novo inimigo de combate. Suas instâncias não bloqueiam o deslocamento de Bury.

A composição passa de janelas e superfícies regulares para mobiliário abandonado, infiltração, luzes com falhas, condutos, névoa, detritos, água e vegetação deteriorada. Fundos subterrâneos não mostram céu externo; a névoa preserva a leitura das bordas.

## Arquitetura e arquivos

Superfícies, colisões, portas, plataformas, inimigos, props, luzes, partículas e controles do elevador são nós configurados em cenas. O C# não cria a geometria jogável em `_Ready()`.

- `DescentRoom`: spawn, descoberta, habilidades e recuperação de queda fora dos limites.
- `WorldInteraction`: requisito/recompensa, transição, descanso e deslocamento pela escada.
- `WorldFlagBarrier`: escotilhas físicas ligadas a flags.
- `WorldEncounter`: libera saída após a remoção de todos os inimigos do grupo.
- `WorldElevator`: opera botões existentes na cena, limitados aos destinos descobertos.
- `Global`: estado da sessão, transições e checkpoints.
- `WorldRoomController` / `Player`: checkpoint e descanso; moveset original preservado.
- `descent_interior`, `descent_mist`, `descent_fault_light`: fundo interno, névoa e luz com AnimationPlayer.
- `tiles/sewer_soil.tres`: recorte do atlas existente, não uma imagem gerada.

Reaproveitados: terrain, tiles de prédios/bosque, janelas, objetos, antenas, torre, elevador, cama, computador, gatos, avô, escada, água, céu/nuvens, partículas e fonte. Nenhum asset ou GDScript foi apagado.

Versões anteriores das cinco cenas substituídas e dos testes legados estão em `.source-backup/pre-premium-20260909/`. Os quatro pontos de entrada de testes antigos agora verificam o novo mundo, não o antigo grafo de oito salas.

## Validação e limites

Logs: `artifacts/premium-*.log`. Capturas reais: `artifacts/premium-review/`.

- Build: última execução sem erros ou avisos.
- Grafo: 75 verificações aprovadas — carregamento, scripts, destinos, spawns e restrições.
- Navegação: seis percursos principais aprovados com inputs normais e física ativa, isolando combate.
- Prólogo: caminhada, escada, transição do quarto, escorregamento e chegada ao topo aprovados sem habilidades proibidas.
- Progressão: 42 verificações aprovadas, incluindo espada, encontros, queda física, retorno da queda antecipada, gates, elevador, missão, atalhos, descanso e checkpoint. O teste posiciona o jogador nas interações e aplica dano aos grupos; **não é um playthrough de combate**. Log: `artifacts/premium-progression-verified.log`.
- Player: regressão em tempo real aprovada.
- Visual: 20 capturas reais revisadas em D3D12/Mobile; execução visual separada do teste estrutural encerrada com código 0. Misturar instanciações descartadas do teste de grafo com capturas provocava falha no encerramento; os modos foram separados.
- Segredos: o teste percorreu as galerias com movimento real. As recompensas dos três andares foram coletadas; o teste chegou aos itens das profundezas/esgoto, mas acionava F antes de entrar inteiramente na área. A tolerância do teste foi corrigida; a repetição ainda não foi executada.
- Combate: teste com inputs, inimigos e dano reais disponível. Ensaios concluíram topo, 7º e 6º, mas **não há aprovação integral de combate**. O controlador automatizado morreu nas regiões mais difíceis; seus resultados variam conforme a estratégia de luta/retorno. Não se atribui toda falha ao jogo nem se declara balanceamento aprovado. Logs exploratórios: `artifacts/premium-combat-*.log`.
- Última rodada: o Windows cancelou a inicialização de três processos paralelos (grafo, navegação e segredos); apenas a progressão foi reexecutada e aprovada. As últimas aprovações anteriores de grafo/navegação continuam identificadas acima, sem inventar uma nova execução.

Comandos de testes (usar o executável Godot Mono disponível e iniciar um de cada vez):

```text
dotnet build --no-restore
Godot --headless --path . --scene res://tests/premium_world_review.tscn
Godot --headless --fixed-fps 60 --path . --scene res://tests/premium_prologue.tscn
Godot --headless --fixed-fps 60 --path . --scene res://tests/premium_traversal.tscn
Godot --headless --fixed-fps 60 --path . --scene res://tests/premium_traversal.tscn -- secrets
Godot --headless --fixed-fps 60 --path . --scene res://tests/premium_progression.tscn
Godot --headless --path . --scene res://tests/player_polish_smoke_test.tscn
Godot --path . --scene res://tests/premium_world_review.tscn -- visual
Godot --headless --path . --scene res://tests/premium_traversal.tscn -- combat
```

Bosses dedicados, história definitiva e save em disco ficam fora da entrega. Playtest humano completo, duração e balanceamento final ainda precisam de avaliação. Os testes automatizados não justificam declarar ausência absoluta de regressões.
