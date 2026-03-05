# Jogo Dungeon Crawler - Full Project Context

## Project Overview
Um jogo dungeon crawler baseado em grid construído em Unity com mapas gerados proceduralmente, sistema de combate automático baseado em turnos/tempo real, IA de inimigos com pathfinding avançado e controlo de personagem do jogador. O jogo utiliza um sistema de grelha (grid) baseado em tiles com visualização 3D baseada em profundidade.

**Estado Atual:** Refatoração do núcleo (Core) concluída (Câmera, Combate Automático e IA de Inimigos).

---

## Project Structure
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── CameraFollow.cs

│   │   ├── TurnManager.cs
│   │   └── EnemySpawner.cs
│   ├── Entities/
│   │   ├── PlayerController.cs
│   │   ├── EnemyController.cs
│   │   ├── Health.cs
│   │   ├── CombatStats.cs
│   │   ├── DamageData.cs
│   │   ├── HealthBar.cs
│   │   ├── EnemyData.cs
│   │   └── SimpleSplash.cs      ← NOVO (Feedback visual de dano)
│   └── Grid/
│       └── GridManager.cs
│   └── TileType.cs (enum)
├── Scenes/
├── Prefabs/
├── Materials/
├── Enemys/
└── Settings/


---

## Core Systems Architecture

### 1. Grid System (GridManager.cs)
**Singleton que gere o estado do mundo do jogo**

**Key Features:**
- **Grid Dimensions:** 100x100 tiles (configurável)
- **Tile Types:** Empty, Wall, Player, Enemy, Item, Water, Lava
- **Coordinate System:** Usa Vector2Int para posições lógicas na grelha, Vector3 para posições no mundo
- **Procedural Generation:** Paredes aleatórias com zona segura no centro (raio = 3 tiles)

**Regra de Ouro do Movimento:**
Toda a entidade deve primeiro limpar o seu tile atual, atualizar a sua posição na grelha, reclamar o novo tile e só depois atualizar a sua posição visual no mundo 3D.

---

### 2. CameraFollow.cs 
**Câmera suave na terceira pessoa que segue o jogador com proteção de limites e colisões.**

**Key Features:**
- **SmoothDamp Movement:** Substituiu o `Lerp` linear por `Vector3.SmoothDamp` para movimentos orgânicos com aceleração e desaceleração.
- **Grid Bounds:** A câmera respeita os limites da grelha (ex: min 0,0 e max 99,99) e não mostra o vazio.
- **Collision Avoidance:** Usa `Physics.Linecast` para detetar a Layer "Wall", impedindo que a câmera atravesse o cenário.
- **Isometric Offset:** Mantém o desvio padrão de (0, 20, -15).

---

### 3. Player System (PlayerController.cs)
**Gere o movimento do jogador e o sistema de combate automático corpo a corpo.**

**Key Features:**
- **Input:** Teclas WASD para movimento (cima/baixo/esquerda/direita).
- **Auto-Combat System:** Se o jogador esbarrar num inimigo, entra em estado de `Engage`, atacando-o repetidamente com base num intervalo de tempo (`attackInterval`).
- **Disengage:** O jogador sai automaticamente do combate se recuar para um espaço livre (`TileType.Empty`) ou se a distância para o alvo (Manhattan) for maior que 1.
- **Grid-Based Targeting:** Substituiu verificações baseadas em colisores físicos (`Physics.OverlapSphere`) por lógica pura de adjacência na grelha.

---

### 4. Enemy System (EnemyController.cs)
**Entidades hostis movidas por IA com visão, pathfinding A* genuíno e combates independentes.**

**Key Features:**
- **Separated Timers:** O intervalo de movimento (`moveInterval`) é independente do intervalo de ataque (`attackInterval`), permitindo variações estratégicas (ex: inimigos rápidos a andar, mas lentos a atacar).
- **Vision Range:** 8 tiles (configurável).
- **A* Pathfinding:** Algoritmo A* completo, utilizando Manhattan Distance. Os inimigos sabem contornar obstáculos. Inclui um limite máximo de iterações por frame para prevenir estrangulamentos na performance (lag) caso o jogador se encurrale.
- **Auto-Retaliation:** Quando um inimigo ataca o jogador, ele sinaliza o jogador (`player.OnAttackedBy()`), permitindo que o jogador riposte automaticamente se estiver inativo.

---

### 5. Combat System & Visual Feedback

#### CombatStats.cs & DamageData.cs
Define estatísticas base (força, dano mínimo/máximo, chance de crítico) e gera o pacote imutável `DamageData`.

#### Health.cs
**O pilar da vitalidade e inicialização da UI.**
- Inicializa de forma segura no `Awake()` para prevenir problemas matemáticos na UI de frames iniciais.
- **Dynamic UI Instantiation:** Instancia automaticamente o `healthBarPrefab` como elemento filho e estabelece a ligação.
- **Visual Feedback:** Instancia o `damageSplashPrefab` (círculo vermelho de dano) de forma segura ("fire and forget") através da classe `SimpleSplash.cs`.
- Tranca os valores de vida num mínimo de 0 para prevenir problemas de renderização da interface.

#### SimpleSplash.cs (Feedback Visual Minimalista)
Script independente que faz um sprite crescer e desvanecer rapidamente no espaço 3D (orientado pela perspetiva isométrica) após um impacto.

#### HealthBar.cs
Utiliza um sistema defensivo com `Mathf.Clamp01` para preencher com precisão as percentagens na imagem da UI sem quebrar quando ocorrem valores de HP negativos ou excessivos.

---

### 6. Enemy Spawning & Data
- **EnemyData (Scriptable Object):** Define propriedades do inimigo, modelos e pesos probabilísticos para instanciação.
- **EnemySpawner:** Gera 20 inimigos de forma aleatória em posições livres na grelha no início do jogo.

---

### 7. Turn System (TurnManager.cs)
**Coordenador do fluxo de turnos (Atualmente Híbrido).**
- O jogo evoluiu de turnos rígidos para uma estrutura de grelha em tempo real com tempos de arrefecimento (cooldowns) no combate e no movimento (A* e ataques automáticos utilizam contadores dinâmicos).

---

## Coordinate Systems

### Grid Coordinates (Vector2Int)
- **Type:** Posição lógica em números inteiros.
- **Range:** (0,0) a (width-1, height-1).
- **Usage:** GridManager, cálculos de A*, rastreio de alvos.

### World Coordinates (Vector3)
- **Type:** Espaço 3D flutuante do Unity.
- **Conversion:** `GridToWorld(x, z)` e `WorldToGrid(position)`.
- Apenas atualizado *após* as validações de movimento da grelha.

---

## Known Issues & Improvement Opportunities

### System-Wide:
1. **No Fog of War:** Todo o mapa é visível de momento. Explorar a grelha por salas e ocultar as não descobertas seria benéfico.
2. **No Drops/Loot System:** Inimigos desaparecem sem largar itens (embora o `TileType.Item` já esteja preparado na arquitetura).
3. **Turn System Purgatory:** O script TurnManager.cs precisa de ser adaptado formalmente ou removido para acomodar perfeitamente este modelo de combate de ação com tempo tático.
4. **No Hazard Tiles:** Water/Lava estão definidos na grelha, mas não implementados logicamente ou visualmente no terreno procedural.

---

## File Reference Quick Links

| File | Purpose | Key Classes |
|------|---------|-------------|
| GridManager.cs | World state & tile management | GridManager(Singleton) |
| CameraFollow.cs | Player tracking with bounds | CameraFollow |
| PlayerController.cs | Player auto-combat & grid movement | PlayerController |
| EnemyController.cs | A* pathfinding & split combat logic | EnemyController |
| SimpleSplash.cs | Visual damage feedback ("fire & forget") | SimpleSplash |
| Health.cs | HP tracking, UI & Death spawning | Health |
| CombatStats.cs | Damage generation logic | CombatStats |
| HealthBar.cs | Protected HP UI visualization | HealthBar |
| EnemyData.cs | Enemy configuration | EnemyData(ScriptableObject) |
| EnemySpawner.cs | Procedural enemy instantiation | EnemySpawner |

---

## Example Game Object Hierarchy (Expected)

Scene
├── GridManager (with EnemySpawner child)
├── TurnManager (Singleton)
├── MainCamera (with CameraFollow script)
├── Player (PlayerController, Health, CombatStats)
│   └── CanvasHealthBar(Clone) (Instanciado via Health.cs)
├── Enemy_1 (EnemyController, Health, CombatStats)
│   └── CanvasHealthBar(Clone) (Instanciado via Health.cs)
├── ... (20 total enemies)
└── Visual Walls (Instantiated from wallPrefab)