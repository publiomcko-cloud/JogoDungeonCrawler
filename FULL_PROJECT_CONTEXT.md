# Jogo Dungeon Crawler - Full Project Context

## Project Overview
A grid-based dungeon crawler game built in Unity with procedurally generated maps, turn-based combat system, enemy AI with pathfinding, and player character control. The game uses a tile-based grid system with depth-based 3D visualization.

**Current Working File:** `CameraFollow.cs` (Core system for camera tracking)

---

## Project Structure
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── CameraFollow.cs      ← CURRENT FILE
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
│   └── Grid/
│       └── GridManager.cs
│   └── TileType.cs (enum)
├── Scenes/
├── Prefabs/
├── Materials/
├── Enemys/
└── Settings/
```

---

## Core Systems Architecture

### 1. Grid System (GridManager.cs)
**Singleton that manages the game world**

**Key Features:**
- **Grid Dimensions:** 100x100 tiles (configurable)
- **Tile Types:** Empty, Wall, Player, Enemy, Item, Water, Lava
- **Coordinate System:** Uses Vector2Int for grid positions, Vector3 for world positions
- **Procedural Generation:** Random walls with safe zone around center (radius = 3 tiles)

**Key Methods:**
```csharp
// Position conversion
WorldToGrid(Vector3 position) → Vector2Int
GridToWorld(int x, int z) → Vector3

// Tile management
SetTile(int x, int z, TileType type)
GetTile(int x, int z) → TileType
IsWalkable(int x, int z) → bool
IsInsideGrid(int x, int z) → bool

// Player tracking
SetPlayerPosition(Vector2Int pos)
PlayerPosition { get; } → Vector2Int
```

**Map Generation:**
- **Borders:** Walls on all edges
- **Internal Walls:** 20% random placement outside 3-tile safe zone
- **Visual:** Instantiates wall prefabs in 3D space

---

### 2. CameraFollow.cs (Current File)
**Smooth third-person camera following the player**

```csharp
public class CameraFollow : MonoBehaviour
{
    public Transform target;                    // Player transform
    public Vector3 offset = new Vector3(0, 20, -15);  // Camera offset
    public float smoothSpeed = 5f;             // Lerp smoothing factor

    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        transform.position = smoothedPosition;
    }
}
```

**Purpose:**
- Follows player from elevated isometric perspective (Y=20, Z=-15)
- Uses LateUpdate for smooth camera tracking
- Linear interpolation with Time.deltaTime for frame-rate independence

**Current Issues/Improvements Possible:**
- Linear Lerp might feel jerky at high smoothSpeed values
- No bounds checking (camera can follow beyond grid edges)
- No collision avoidance (can clip through walls/scenery)
- Static offset angle (no rotation/zoom)

---

### 3. Player System (PlayerController.cs)
**Handles player movement and combat**

**Key Features:**
- **Input:** WASD keys for movement (up/down/left/right)
- **Grid-Based:** Moves one tile per input
- **Combat:** Melee attack on adjacent enemies
- **Position Tracking:** Updates GridManager with player location

**Movement Logic:**
1. Player presses WASD
2. Calculate target tile
3. Check if inside grid
4. Check tile type:
   - Enemy → Attack
   - Walkable (Empty/Item) → Move
   - Wall/Blocked → No movement

**Attack Mechanics:**
- Uses Physics.OverlapSphere to detect enemies
- Retrieves Health and CombatStats components
- Generates damage data and applies it
- Only hits first enemy in radius

---

### 4. Enemy System (EnemyController.cs)
**AI-driven hostile entities with vision, pathfinding, and combat**

**Key Features:**
- **Vision Range:** 8 tiles (configurable)
- **State Machine:** Idle and Chase states
- **Movement:** Every 0.5s (configurable moveInterval)
- **AI Behaviors:** Line-of-sight detection, A* pathfinding, random idle movement

**Vision System:**
```csharp
CanSeePlayer() → bool
// Checks distance AND line-of-sight (Bresenham line algorithm)
// Blocked by walls
```

**Pathfinding:**
```csharp
AStar(Vector2Int start, Vector2Int goal) → List<Vector2Int>
// Complete A* implementation with:
// - gScore tracking
// - heuristic function (Manhattan distance)
// - neighbor detection (4-direction cardinal)
// - path reconstruction
```

**Movement Behavior:**
- **Chase:** Uses A* to pathfind to player, moves to next tile in path
- **Idle:** Random movement in cardinal directions
- **Attack:** Direct attack if adjacent to player

**Statistics:**
- Spawned by EnemySpawner with configurable stats per type
- Health system with health bar UI
- Combat stats (strength, minDamage, maxDamage, crit chance)

---

### 5. Combat System

#### CombatStats.cs
**Character base stats and damage generation**

```csharp
public class CombatStats : MonoBehaviour
{
    public int strength = 5;              // Base stat bonus
    public int minDamage = 2;             // Damage range
    public int maxDamage = 5;
    
    public float critChance = 0.1f;       // 10% critical chance
    public float critMultiplier = 2f;     // Critical multiplier
    
    public DamageData GenerateDamage()    // Random damage per attack
}
```

#### DamageData.cs
**Immutable damage information structure**

```csharp
[System.Serializable]
public class DamageData
{
    public int baseDamage;
    public bool isCritical;
    public float criticalMultiplier;
    
    public int FinalDamage()
    {
        return isCritical ? 
            Mathf.RoundToInt(baseDamage * criticalMultiplier) : 
            baseDamage;
    }
}
```

#### Health.cs
**Enemy/Player health tracking and death**

```csharp
public int maxHP = 10;
public int currentHP;
public bool IsDead => currentHP <= 0;

public void TakeDamage(DamageData damage)
{
    int finalDamage = damage.FinalDamage();
    currentHP -= finalDamage;
    
    // Log damage
    Debug.Log($"[{gameObject.name}] took {finalDamage} damage " + 
              $"{(damage.isCritical ? "CRITICAL!" : "")} (HP: {currentHP})");
    
    // Death handling
    if (currentHP <= 0)
    {
        // Enemy: Call Die()
        // Player: Destroy()
    }
}

void Die()
{
    // Call EnemyController.Die()
    Destroy(gameObject);
}
```

#### HealthBar.cs
**UI health bar visualization**

```csharp
public Health targetHealth;      // Reference to Health component
public Image fillImage;           // UI Image for fill display

void Update()
{
    float percent = (float)targetHealth.currentHP / targetHealth.maxHP;
    fillImage.fillAmount = percent;
}
```

---

### 6. Enemy Spawning & Data

#### EnemyData.cs
**Scriptable Object defining enemy types**

```csharp
[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;
    
    public int spawnWeight = 1;    // Weighted random spawn
    
    // Stats
    public int maxHP = 5;
    public int strength = 2;
    public int minDamage = 1;
    public int maxDamage = 3;
}
```

#### EnemySpawner.cs
**Spawns configured number of weighted-random enemies**

```csharp
public List<EnemyData> enemyTypes;      // Available enemy types
public int enemyCount = 20;             // Total enemies to spawn

void SpawnEnemies()               // Called on Start
void SpawnSingleEnemy()
// 1. Find random free tile (100 attempts)
// 2. Pick enemy by weighted probability
// 3. Instantiate prefab
// 4. Apply EnemyData stats to Health/CombatStats

EnemyData GetWeightedRandomEnemy()
// Cumulative probability selection
```

---

### 7. Turn System (TurnManager.cs)
**Manages player vs enemy turn flow**

```csharp
public static TurnManager Instance;
public bool playerTurn = true;

public delegate void OnEnemyTurn();
public event OnEnemyTurn EnemyTurnEvent;

public void EndPlayerTurn()
{
    playerTurn = false;
    EnemyTurnEvent?.Invoke();           // Signal enemies to move
    Invoke(nameof(StartPlayerTurn), 0.2f);  // Delay before next turn
}

void StartPlayerTurn()
{
    playerTurn = true;
}
```

**Current State:**
- Turn system defined but integration with movement appears incomplete
- EnemyController has moveInterval timer rather than listening to turn events
- Could be improved for true turn-based gameplay

---

### 8. Game Data & Enums

#### TileType.cs
```csharp
public enum TileType
{
    Empty = 0,    // Walkable empty space
    Wall = 1,     // Collider blocking
    Player = 2,   // Player character
    Enemy = 3,    // Enemy character
    Item = 4,     // Pickup item (future)
    Water = 5,    // Hazard (future)
    Lava = 6      // Hazard (future)
}
```

---

## Key Architectural Patterns Used

### 1. Singleton Pattern
```csharp
public static GridManager Instance;
public static TurnManager Instance;
```
Global access to critical systems

### 2. Component-Based Design
- Health.cs
- CombatStats.cs
- EnemyController.cs
- PlayerController.cs
All attached to GameObjects and communicate via GetComponent()

### 3. Events/Delegates
```csharp
public event OnEnemyTurn EnemyTurnEvent;  // TurnManager
```

### 4. Scriptable Objects
```csharp
EnemyData.cs    // Defines enemy configurations
```

### 5. State Machine
```csharp
private enum State { Idle, Chase }  // EnemyController
```

---

## Coordinate Systems

### Grid Coordinates (Vector2Int)
- **Type:** Integer grid position
- **Range:** (0,0) to (width-1, height-1)
- **Usage:** GridManager, movement calculation, tile tracking
- **Axes:** x (horizontal), z (vertical/depth)

### World Coordinates (Vector3)
- **Type:** Unity 3D world space floats
- **Conversion:** GridToWorld(x, z) = Vector3(x, 1, z)
- **Y-Axis:** Always 1.0 (2D top-down with visual height)
- **Camera Offset:** (0, 20, -15) for isometric view

### Important:
- GridToWorld uses `z` parameter but positions it as Z-axis in 3D
- WorldToGrid rounds X and Z components from Vector3
- Player.gridPosition tracked separately from transform.position

---

## Current Game Flow

1. **Start:**
   - GridManager initializes 100x100 grid
   - Procedural map generation (borders + internal walls)
   - Wall prefabs instantiated visually
   - Player spawned at center, registers with GridManager
   - Camera assigned target = player.transform
   - EnemySpawner creates 20 weighted-random enemies at free tiles

2. **Update (Each Frame):**
   - Camera smoothly follows player 5 units/sec
   - Enemy movement timer accumulates
   - Every 0.5s: Enemy updates AI state and moves (if visible)
   - Player input checks for WASD keys

3. **Player Action:**
   - Player presses movement key
   - Check target tile validity
   - If enemy: call Attack() (generate damage → TakeDamage)
   - If walkable: move, update GridManager, update world position
   - Camera follows player

4. **Enemy Action:**
   - Check if player visible (distance + LoS)
   - If visible: A* pathfind to player, move to next step
   - If not visible: random movement
   - Can attack player if adjacent

5. **Combat Resolution:**
   - CombatStats.GenerateDamage() → random roll + strength + crit check
   - Health.TakeDamage(damage) → reduce HP, log damage
   - If HP ≤ 0: call Die() → Destroy gameobject

---

## Known Issues & Improvement Opportunities

### CameraFollow.cs Specific:
1. **No Bounds Checking** - Camera follows beyond grid edges
2. **Linear Lerp Only** - No easing functions for different feel
3. **Static Offset** - Always same angle, no rotation/zoom
4. **No Collision** - Can clip through walls
5. **Hard-coded Values** - Offset and speed not designer-friendly

### System-Wide:
1. **Turn System Not Integrated** - Enemies use timer, not turn events
2. **No Fog of War** - All map visible regardless of player position
3. **A* Not Optimized** - Full search each frame could lag with many enemies
4. **Movement Feels Instant** - No walking animation/transition
5. **No Inventory/Items** - Item tiles defined but unused
6. **No Hazard Tiles** - Water/Lava defined but not implemented

---

## Code Quality Notes

- **Consistent Naming:** PascalCase for classes/methods, camelCase for fields
- **Comment Organization:** Section dividers (// SECTION NAME)
- **Error Handling:** Null checks present (GridManager.Instance checks)
- **Serialization:** Public fields for inspector tweaking
- **Structure:** Logical method grouping by functionality
- **Performance:** Mostly efficient, some A* optimization possible

---

## Next Steps for Development

1. **Immediate:** Fix turn-based system integration
2. **Short-term:** Improve camera with bounds + easing
3. **Medium-term:** Add player stats/inventory system
4. **Long-term:** Procedural dungeon generation, multiple levels, boss fights

---

## File Reference Quick Links

| File | Purpose | Key Classes |
|------|---------|-------------|
| GridManager.cs | World state & tile management | GridManager(Singleton) |
| CameraFollow.cs | Player tracking camera | CameraFollow |
| PlayerController.cs | Player input & movement | PlayerController |
| EnemyController.cs | AI pathfinding & combat | EnemyController |
| TurnManager.cs | Turn flow coordination | TurnManager(Singleton) |
| EnemySpawner.cs | Enemy instantiation | EnemySpawner |
| Health.cs | HP tracking | Health |
| CombatStats.cs | Damage generation | CombatStats |
| HealthBar.cs | HP UI visualization | HealthBar |
| EnemyData.cs | Enemy configuration | EnemyData(ScriptableObject) |
| TileType.cs | Tile enumeration | TileType(Enum) |

---

## Example Game Object Hierarchy (Expected)

```
Scene
├── GridManager (with EnemySpawner child)
├── TurnManager (Singleton)
├── MainCamera (with CameraFollow script)
├── Player (PlayerController, Health, CombatStats, GameObects)
│   └── HealthBar (UI Canvas)
├── Enemy_1 (EnemyController, Health, CombatStats)
│   └── HealthBar
├── Enemy_2 (EnemyController, Health, CombatStats)
│   └── HealthBar
├── ... (20 total enemies)
└── Visual Walls (Instantiated from wallPrefab)
```

---

**Document Version:** 1.0 - Complete project context snapshot
**Generated:** March 4, 2026
**Project:** Jogo Dungeon Crawler (Dungeon Crawler Game)
