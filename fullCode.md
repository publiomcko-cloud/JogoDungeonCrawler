// --- TileType.cs ---
public enum TileType
{
    Empty = 0,
    Wall = 1,
    Player = 2,
    Enemy = 3,
    Item = 4,
    Water = 5,
    Lava = 6
}

// --- GridManager.cs ---
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public Vector2Int PlayerPosition { get; private set; }

    [Header("Grid Size")]
    public int width = 100;
    public int height = 100;

    [Header("Procedural Settings")]
    [Range(0f, 1f)]
    public float wallChance = 0.2f;
    public int safeZoneRadius = 3;

    [Header("Prefabs")]
    public GameObject wallPrefab;

    private TileType[,] grid;
    public EnemySpawner spawner;

    private void Awake()
    {
        Instance = this;
        grid = new TileType[width, height];
    }

    private void Start()
    {
        InitializeGrid();
        GenerateMap();
        BuildVisual();
        spawner.SpawnEnemies();
    }

    // =========================
    // GRID CORE
    // =========================

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                grid[x, z] = TileType.Empty;
            }
        }
    }

    public void SetPlayerPosition(Vector2Int pos)
        {
            PlayerPosition = pos;
        }

    public bool IsInsideGrid(int x, int z)
    {
        return x >= 0 && x < width &&
               z >= 0 && z < height;
    }

    public TileType GetTile(int x, int z)
    {
        if (!IsInsideGrid(x, z)) return TileType.Wall;
        return grid[x, z];
    }

    public void SetTile(int x, int z, TileType type)
    {
        if (!IsInsideGrid(x, z)) return;
        grid[x, z] = type;
    }

    public Vector2Int WorldToGrid(Vector3 position)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.z)
        );
    }

    public Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(x, 1, z);
    }

    public bool IsWalkable(int x, int z)
    {
        TileType t = GetTile(x, z);
        return t == TileType.Empty || t == TileType.Item;
    }

    // =========================
    // MAP GENERATION
    // =========================

    void GenerateMap()
    {
        GenerateBorders();
        GenerateInternalWalls();
    }

    void GenerateBorders()
    {
        for (int x = 0; x < width; x++)
        {
            SetTile(x, 0, TileType.Wall);
            SetTile(x, height - 1, TileType.Wall);
        }

        for (int z = 0; z < height; z++)
        {
            SetTile(0, z, TileType.Wall);
            SetTile(width - 1, z, TileType.Wall);
        }
    }

    void GenerateInternalWalls()
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                if (Vector2Int.Distance(new Vector2Int(x, z), center) <= safeZoneRadius)
                    continue;

                if (Random.value < wallChance)
                {
                    SetTile(x, z, TileType.Wall);
                }
            }
        }
    }

    // =========================
    // VISUAL BUILD
    // =========================

    void BuildVisual()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                TileType tile = grid[x, z];

                if (tile == TileType.Wall && wallPrefab != null)
                {
                    Instantiate(wallPrefab, GridToWorld(x, z), Quaternion.identity);
                }
            }
        }
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, 0, height));
        }

        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(new Vector3(0, 0, z), new Vector3(width, 0, z));
        }
    }
}

// --- SimpleSplash.cs ---
using UnityEngine;

public class SimpleSplash : MonoBehaviour
{
    public float lifetime = 0.4f;
    public Vector3 maxScale = new Vector3(1.5f, 1.5f, 1.5f);
    
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        // Fica de frente para a inclinação da câmera isométrica
        transform.rotation = Quaternion.Euler(53f, 0f, 0f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.Lerp(originalScale, maxScale, progress);

        if (spriteRenderer != null)
        {
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = newColor;
        }
    }
}

// --- PlayerController.cs ---
using UnityEngine;

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("Visuals")]
    [Tooltip("Velocidade da animação de movimento entre tiles")]
    public float moveSpeed = 10f;

    [Header("Auto-Combat Settings")]
    [Tooltip("Intervalo em segundos entre cada ataque automático")]
    public float attackInterval = 0.5f;
    private float attackTimer = 0f;
    public GameObject combatTarget;

    private Health health;
    private CombatStats combatStats;

    void Start()
    {
        health = GetComponent<Health>();
        combatStats = GetComponent<CombatStats>();
        
        // Sincronização inicial estrita com as regras do GridManager
        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetPlayerPosition(gridPosition);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        HandleInput();
        HandleAutoCombat();
        UpdateVisualPosition();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = gridPosition + direction;

        if (!GridManager.Instance.IsInsideGrid(targetPos.x, targetPos.y)) return;

        TileType targetTile = GridManager.Instance.GetTile(targetPos.x, targetPos.y);

        if (targetTile == TileType.Enemy)
        {
            // Bateu no inimigo: Inicia combate automático
            GameObject enemyObj = FindEnemyAt(targetPos);
            if (enemyObj != null)
            {
                EngageTarget(enemyObj);
            }
        }
        else if (targetTile == TileType.Empty || targetTile == TileType.Item)
        {
            // Moveu para espaço vazio: Foge do combate e anda
            DisengageCombat();
            MoveTo(targetPos);
        }
    }

    void MoveTo(Vector2Int newPos)
    {
        // Regra Crítica de Movimento: Grid antigo -> gridPosition -> Grid novo
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        
        gridPosition = newPos;
        GridManager.Instance.SetPlayerPosition(gridPosition);

        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
    }

    void UpdateVisualPosition()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
    }

    public void EngageTarget(GameObject target)
    {
        combatTarget = target;
        attackTimer = attackInterval; // Força o primeiro ataque a acontecer imediatamente
    }

    public void DisengageCombat()
    {
        combatTarget = null;
        attackTimer = 0f;
    }

    void HandleAutoCombat()
    {
        if (combatTarget == null) return;

        Health targetHealth = combatTarget.GetComponent<Health>();
        if (targetHealth == null || targetHealth.IsDead)
        {
            DisengageCombat(); // Inimigo morreu, limpa o alvo
            return;
        }

        EnemyController enemy = combatTarget.GetComponent<EnemyController>();
        if (enemy == null) return;

        // Checa distância Manhattan. Se for maior que 1, o inimigo não está mais adjacente.
        int distance = Mathf.Abs(gridPosition.x - enemy.gridPosition.x) + Mathf.Abs(gridPosition.y - enemy.gridPosition.y);
        
        if (distance > 1)
        {
            DisengageCombat(); // Inimigo saiu de perto
            return;
        }

        // Loop de dano por tempo
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            DamageData damage = combatStats.GenerateDamage();
            targetHealth.TakeDamage(damage);
        }
    }

    // Busca lógica via Grid, removendo a necessidade de Physics.OverlapSphere
    GameObject FindEnemyAt(Vector2Int pos)
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy.gridPosition == pos) return enemy.gameObject;
        }
        return null;
    }

    public void OnAttackedBy(GameObject attacker)
    {
        // Se o inimigo bater no player primeiro e o player estiver parado, ele revida automaticamente
        if (combatTarget == null)
        {
            EngageTarget(attacker);
        }
    }
}

// --- HealthBar.cs ---
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health targetHealth;
    public Image fillImage;

    private void Update()
    {
        if (targetHealth == null) return;

        float percent = (float)targetHealth.currentHP / targetHealth.maxHP;
        fillImage.fillAmount = percent;
    }
}

// --- Health.cs ---
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    [Header("UI Reference")]
    [Tooltip("Prefab da barra de vida (CanvasHealthBar)")]
    public GameObject healthBarPrefab; 

    [Header("Visual Feedback")]
    [Tooltip("Arraste o Prefab do circulo vermelho de dano aqui")]
    public GameObject damageSplashPrefab;
    [Tooltip("Altura para o círculo não ficar afundado no chão")]
    public Vector3 splashOffset = new Vector3(0, 1.5f, 0);

    void Awake()
    {
        // Garante que a vida inicie cheia
        if (maxHP <= 0) maxHP = 1;
        if (currentHP <= 0) currentHP = maxHP;
        
        // A SOLUÇÃO: Cria a barra de vida fisicamente no mundo 3D
        if (healthBarPrefab != null)
        {
            // Instancia a barra de vida como "filha" do Player/Inimigo para que ela ande junto com ele
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            
            // Procura o script HealthBar dentro da barra que acabou de nascer e liga ele a esta vida
            HealthBar barScript = healthBarInstance.GetComponentInChildren<HealthBar>();
            if (barScript != null)
            {
                barScript.targetHealth = this; 
            }
        }
    }

    public void TakeDamage(DamageData damage)
    {
        if (IsDead) return;

        int finalDamage = damage.FinalDamage();
        currentHP -= finalDamage;
        
        // Trava a vida no mínimo zero para não bugar a UI
        if (currentHP < 0) currentHP = 0;
        
        Debug.Log($"[{gameObject.name}] tomou {finalDamage} de dano. (HP: {currentHP})");

        // Instancia o feedback visual de forma segura
        if (damageSplashPrefab != null)
        {
            Instantiate(damageSplashPrefab, transform.position + splashOffset, Quaternion.identity);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Die();
        }
        else
        {
            PlayerController player = GetComponent<PlayerController>();
            if (player != null)
            {
                // Libera o grid antes de destruir o player
                GridManager.Instance.SetTile(player.gridPosition.x, player.gridPosition.y, TileType.Empty);
            }
            Destroy(gameObject);
        }
    }
}

// --- EnemyData.cs ---
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;

    [Header("Spawn Settings")]
    public int spawnWeight = 1; // chance relativa

    [Header("Stats")]
    public int maxHP = 5;
    public int strength = 2;
    public int minDamage = 1;
    public int maxDamage = 3;
}

// --- EnemyController.cs ---
using UnityEngine;
using System.Collections.Generic; // Necessário para List e HashSet usados no A*

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class EnemyController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("AI Movement Settings")]
    [Tooltip("Intervalo em segundos entre cada passo do inimigo.")]
    public float moveInterval = 0.5f;
    private float moveTimer = 0f;
    [Tooltip("Distância máxima que o inimigo detecta o player.")]
    public int visionRange = 8;

    [Header("AI Combat Settings")]
    [Tooltip("Intervalo em segundos entre cada ataque do inimigo.")]
    public float attackInterval = 1.0f;
    private float attackTimer = 0f;
    
    [Header("Visuals")]
    public float moveSpeed = 10f;

    private Health health;
    private CombatStats combatStats;

    private enum State { Idle, Chase }
    private State currentState = State.Idle;

    // Classe interna para os cálculos do algoritmo A*
    private class PathNode
    {
        public Vector2Int Position;
        public int GCost; // Custo do início até aqui
        public int HCost; // Custo estimado (Manhattan) daqui até o alvo
        public int FCost => GCost + HCost;
        public PathNode Parent; // Usado para refazer o caminho de volta

        public PathNode(Vector2Int pos) { Position = pos; }
    }

    void Start()
    {
        health = GetComponent<Health>();
        combatStats = GetComponent<CombatStats>();

        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);

        attackTimer = attackInterval; 
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        UpdateVisualPosition();

        Vector2Int playerPos = GridManager.Instance.PlayerPosition;
        int distToPlayer = ManhattanDistance(gridPosition, playerPos);

        // 1. Loop de Combate
        if (distToPlayer == 1)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f; 
                AttackPlayer();
            }
        }
        else
        {
            attackTimer = attackInterval;
        }

        // 2. Loop de Movimento
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            
            if (distToPlayer > 1) 
            {
                PerformMovement(playerPos, distToPlayer);
            }
        }
    }

    void PerformMovement(Vector2Int playerPos, int distToPlayer)
    {
        if (distToPlayer <= visionRange)
        {
            currentState = State.Chase;
            ChasePlayer(playerPos);
        }
        else
        {
            currentState = State.Idle;
            RandomMove();
        }
    }

    void AttackPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                DamageData damage = combatStats.GenerateDamage();
                playerHealth.TakeDamage(damage);
                player.OnAttackedBy(gameObject);
            }
        }
    }

    void ChasePlayer(Vector2Int playerPos)
    {
        // Usa o algoritmo A* para achar o melhor caminho contornando paredes
        List<Vector2Int> path = FindPath(gridPosition, playerPos);

        if (path != null && path.Count > 0)
        {
            Vector2Int nextStep = path[0];

            // Garante que não vai tentar pisar em cima do player (o combate lida com a colisão lógica)
            if (nextStep != playerPos && GridManager.Instance.IsWalkable(nextStep.x, nextStep.y))
            {
                MoveTo(nextStep);
            }
        }
        else
        {
            // Se não há caminho possível (player cercado de paredes), o inimigo tenta pelo menos andar aleatório
            RandomMove();
        }
    }

    // --- ALGORITMO A* (A-STAR) ---
    List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode(startPos) { GCost = 0, HCost = ManhattanDistance(startPos, targetPos) };
        openList.Add(startNode);

        int iterations = 0;
        int maxIterations = 300; // Proteção contra loops infinitos em mapas muito complexos

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Pega o nó com o menor custo F
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost || (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            // Se achou o alvo, reconstrói o caminho
            if (currentNode.Position == targetPos)
            {
                return RetracePath(startNode, currentNode);
            }

            // Checa vizinhos nas 4 direções
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentNode.Position + dir;

                if (!GridManager.Instance.IsInsideGrid(neighborPos.x, neighborPos.y)) continue;
                if (closedList.Contains(neighborPos)) continue;

                // O vizinho precisa ser "caminhável" OU ser a posição do próprio alvo (para o caminho poder chegar até ele)
                bool isWalkable = GridManager.Instance.IsWalkable(neighborPos.x, neighborPos.y) || neighborPos == targetPos;
                if (!isWalkable) continue;

                int tentativeGCost = currentNode.GCost + 1;

                PathNode neighborNode = openList.Find(n => n.Position == neighborPos);
                if (neighborNode == null)
                {
                    neighborNode = new PathNode(neighborPos);
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = ManhattanDistance(neighborPos, targetPos);
                    neighborNode.Parent = currentNode;
                    openList.Add(neighborNode);
                }
                else if (tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.Parent = currentNode;
                }
            }
        }
        
        return null; // Caminho não encontrado
    }

    List<Vector2Int> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Reverse(); // Inverte para que o primeiro item da lista seja o próximo passo
        return path;
    }
    // -----------------------------

    void RandomMove()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int randomDir = dirs[Random.Range(0, dirs.Length)];
        Vector2Int targetPos = gridPosition + randomDir;

        if (GridManager.Instance.IsInsideGrid(targetPos.x, targetPos.y) && 
            GridManager.Instance.IsWalkable(targetPos.x, targetPos.y))
        {
            MoveTo(targetPos);
        }
    }

    void MoveTo(Vector2Int newPos)
    {
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        gridPosition = newPos;
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);
    }

    void UpdateVisualPosition()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
    }

    public void Die()
    {
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        Destroy(gameObject);
    }

    int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}

// --- DamageData.cs ---
using UnityEngine;

[System.Serializable]
public class DamageData
{
    public int baseDamage;
    public bool isCritical;
    public float criticalMultiplier;

    public int FinalDamage()
    {
        if (isCritical)
            return Mathf.RoundToInt(baseDamage * criticalMultiplier);

        return baseDamage;
    }
}

// --- CombatStats.cs ---
using UnityEngine;

public class CombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int strength = 5;
    public int minDamage = 2;
    public int maxDamage = 5;

    [Header("Crit Settings")]
    [Range(0f, 1f)]
    public float critChance = 0.1f;
    public float critMultiplier = 2f;

    public DamageData GenerateDamage()
    {
        DamageData dmg = new DamageData();

        int roll = Random.Range(minDamage, maxDamage + 1);
        dmg.baseDamage = roll + strength;

        dmg.isCritical = Random.value < critChance;
        dmg.criticalMultiplier = critMultiplier;

        return dmg;
    }
}

// --- TurnManager.cs ---
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public bool playerTurn = true;

    public delegate void OnEnemyTurn();
    public event OnEnemyTurn EnemyTurnEvent;

    private void Awake()
    {
        Instance = this;
    }

    public void EndPlayerTurn()
    {
        playerTurn = false;
        EnemyTurnEvent?.Invoke();
        Invoke(nameof(StartPlayerTurn), 0.2f);
    }

    void StartPlayerTurn()
    {
        playerTurn = true;
    }
}

// --- EnemySpawner.cs ---
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<EnemyData> enemyTypes;
    public int enemyCount = 20;

    public void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnSingleEnemy();
        }
    }

    void SpawnSingleEnemy()
    {
        Vector2Int pos = GetRandomFreeTile();

        if (pos == Vector2Int.zero)
            return;

        EnemyData selected = GetWeightedRandomEnemy();

        GameObject enemyObj = Instantiate(
            selected.prefab,
            GridManager.Instance.GridToWorld(pos.x, pos.y),
            Quaternion.identity
            
        );

        GridManager.Instance.SetTile(pos.x, pos.y, TileType.Enemy);

        // Aplicar stats
        Health hp = enemyObj.GetComponent<Health>();
        CombatStats stats = enemyObj.GetComponent<CombatStats>();

        if (hp != null)
            hp.maxHP = selected.maxHP;

        if (stats != null)
        {
            stats.strength = selected.strength;
            stats.minDamage = selected.minDamage;
            stats.maxDamage = selected.maxDamage;
        }
    }

    Vector2Int GetRandomFreeTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(1, GridManager.Instance.width - 1);
            int y = Random.Range(1, GridManager.Instance.height - 1);

            if (GridManager.Instance.IsWalkable(x, y))
                return new Vector2Int(x, y);
        }

        return Vector2Int.zero;
    }

    EnemyData GetWeightedRandomEnemy()
    {
        int totalWeight = 0;

        foreach (var e in enemyTypes)
            totalWeight += e.spawnWeight;

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var e in enemyTypes)
        {
            cumulative += e.spawnWeight;

            if (roll < cumulative)
                return e;
        }

        return enemyTypes[0];
    }
}

// --- CameraFollow.cs ---
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("O Transform do Player que a câmera deve seguir.")]
    public Transform target;
    
    [Tooltip("Posição da câmera em relação ao alvo (Padrão: 0, 20, -15).")]
    public Vector3 offset = new Vector3(0, 20f, -15f);

    [Header("Movement Settings")]
    [Tooltip("Tempo aproximado para a câmera alcançar o alvo. Valores menores deixam a câmera mais ágil.")]
    public float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero; // Usado internamente pelo SmoothDamp

    [Header("Grid Bounds")]
    [Tooltip("Ative para impedir que a câmera siga o jogador para fora dos limites do grid.")]
    public bool useGridBounds = true;
    
    [Tooltip("Limites mínimos do grid no mundo (X, Z).")]
    public Vector2 minBounds = new Vector2(0, 0);
    
    [Tooltip("Limites máximos do grid no mundo (X, Z). Para um grid 100x100, use 99, 99.")]
    public Vector2 maxBounds = new Vector2(99, 99);

    [Header("Collision Handling")]
    [Tooltip("Ative para impedir que a câmera atravesse paredes ou cenários.")]
    public bool avoidClipping = true;
    
    [Tooltip("A Layer que representa os obstáculos (ex: Walls).")]
    public LayerMask obstacleLayer;
    
    [Tooltip("Distância mínima que a câmera deve manter da parede ao colidir.")]
    public float collisionOffset = 0.5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Pega a posição base do alvo (Player)
        Vector3 focusPosition = target.position;

        // 2. Aplica os limites do Grid (Bounds) para a posição de foco
        if (useGridBounds)
        {
            focusPosition.x = Mathf.Clamp(focusPosition.x, minBounds.x, maxBounds.x);
            focusPosition.z = Mathf.Clamp(focusPosition.z, minBounds.y, maxBounds.y);
        }

        // 3. Calcula a posição ideal da câmera com o offset
        Vector3 desiredPosition = focusPosition + offset;

        // 4. Prevenção de Clipping (Colisão) com obstáculos
        if (avoidClipping)
        {
            RaycastHit hit;
            // Lança um raio do jogador (foco) até onde a câmera quer ir
            if (Physics.Linecast(focusPosition, desiredPosition, out hit, obstacleLayer))
            {
                // Se bater em uma parede, a posição desejada passa a ser o ponto de impacto,
                // afastado levemente na direção do jogador para não entrar na malha do 3D
                desiredPosition = hit.point + (focusPosition - desiredPosition).normalized * collisionOffset;
            }
        }

        // 5. Move a câmera suavemente para a posição final usando SmoothDamp
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothTime
        );
    }
}
``` 

Salve este conteúdo em qualquer local (por exemplo `e:\unity\projetos\jogoDungeonCrawler\fullCode.cs`) e terá uma visão contínua de todos os scripts usados no projecto, com comentários de separação.// filepath: e:\unity\projetos\jogoDungeonCrawler\fullCode.cs
// --- TileType.cs ---
public enum TileType
{
    Empty = 0,
    Wall = 1,
    Player = 2,
    Enemy = 3,
    Item = 4,
    Water = 5,
    Lava = 6
}

// --- GridManager.cs ---
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public Vector2Int PlayerPosition { get; private set; }

    [Header("Grid Size")]
    public int width = 100;
    public int height = 100;

    [Header("Procedural Settings")]
    [Range(0f, 1f)]
    public float wallChance = 0.2f;
    public int safeZoneRadius = 3;

    [Header("Prefabs")]
    public GameObject wallPrefab;

    private TileType[,] grid;
    public EnemySpawner spawner;

    private void Awake()
    {
        Instance = this;
        grid = new TileType[width, height];
    }

    private void Start()
    {
        InitializeGrid();
        GenerateMap();
        BuildVisual();
        spawner.SpawnEnemies();
    }

    // =========================
    // GRID CORE
    // =========================

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                grid[x, z] = TileType.Empty;
            }
        }
    }

    public void SetPlayerPosition(Vector2Int pos)
        {
            PlayerPosition = pos;
        }

    public bool IsInsideGrid(int x, int z)
    {
        return x >= 0 && x < width &&
               z >= 0 && z < height;
    }

    public TileType GetTile(int x, int z)
    {
        if (!IsInsideGrid(x, z)) return TileType.Wall;
        return grid[x, z];
    }

    public void SetTile(int x, int z, TileType type)
    {
        if (!IsInsideGrid(x, z)) return;
        grid[x, z] = type;
    }

    public Vector2Int WorldToGrid(Vector3 position)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.z)
        );
    }

    public Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(x, 1, z);
    }

    public bool IsWalkable(int x, int z)
    {
        TileType t = GetTile(x, z);
        return t == TileType.Empty || t == TileType.Item;
    }

    // =========================
    // MAP GENERATION
    // =========================

    void GenerateMap()
    {
        GenerateBorders();
        GenerateInternalWalls();
    }

    void GenerateBorders()
    {
        for (int x = 0; x < width; x++)
        {
            SetTile(x, 0, TileType.Wall);
            SetTile(x, height - 1, TileType.Wall);
        }

        for (int z = 0; z < height; z++)
        {
            SetTile(0, z, TileType.Wall);
            SetTile(width - 1, z, TileType.Wall);
        }
    }

    void GenerateInternalWalls()
    {
        Vector2Int center = new Vector2Int(width / 2, height / 2);

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                if (Vector2Int.Distance(new Vector2Int(x, z), center) <= safeZoneRadius)
                    continue;

                if (Random.value < wallChance)
                {
                    SetTile(x, z, TileType.Wall);
                }
            }
        }
    }

    // =========================
    // VISUAL BUILD
    // =========================

    void BuildVisual()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                TileType tile = grid[x, z];

                if (tile == TileType.Wall && wallPrefab != null)
                {
                    Instantiate(wallPrefab, GridToWorld(x, z), Quaternion.identity);
                }
            }
        }
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, 0, height));
        }

        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(new Vector3(0, 0, z), new Vector3(width, 0, z));
        }
    }
}

// --- SimpleSplash.cs ---
using UnityEngine;

public class SimpleSplash : MonoBehaviour
{
    public float lifetime = 0.4f;
    public Vector3 maxScale = new Vector3(1.5f, 1.5f, 1.5f);
    
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        // Fica de frente para a inclinação da câmera isométrica
        transform.rotation = Quaternion.Euler(53f, 0f, 0f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.Lerp(originalScale, maxScale, progress);

        if (spriteRenderer != null)
        {
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = newColor;
        }
    }
}

// --- PlayerController.cs ---
using UnityEngine;

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("Visuals")]
    [Tooltip("Velocidade da animação de movimento entre tiles")]
    public float moveSpeed = 10f;

    [Header("Auto-Combat Settings")]
    [Tooltip("Intervalo em segundos entre cada ataque automático")]
    public float attackInterval = 0.5f;
    private float attackTimer = 0f;
    public GameObject combatTarget;

    private Health health;
    private CombatStats combatStats;

    void Start()
    {
        health = GetComponent<Health>();
        combatStats = GetComponent<CombatStats>();
        
        // Sincronização inicial estrita com as regras do GridManager
        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetPlayerPosition(gridPosition);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        HandleInput();
        HandleAutoCombat();
        UpdateVisualPosition();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int targetPos = gridPosition + direction;

        if (!GridManager.Instance.IsInsideGrid(targetPos.x, targetPos.y)) return;

        TileType targetTile = GridManager.Instance.GetTile(targetPos.x, targetPos.y);

        if (targetTile == TileType.Enemy)
        {
            // Bateu no inimigo: Inicia combate automático
            GameObject enemyObj = FindEnemyAt(targetPos);
            if (enemyObj != null)
            {
                EngageTarget(enemyObj);
            }
        }
        else if (targetTile == TileType.Empty || targetTile == TileType.Item)
        {
            // Moveu para espaço vazio: Foge do combate e anda
            DisengageCombat();
            MoveTo(targetPos);
        }
    }

    void MoveTo(Vector2Int newPos)
    {
        // Regra Crítica de Movimento: Grid antigo -> gridPosition -> Grid novo
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        
        gridPosition = newPos;
        GridManager.Instance.SetPlayerPosition(gridPosition);

        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
    }

    void UpdateVisualPosition()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
    }

    public void EngageTarget(GameObject target)
    {
        combatTarget = target;
        attackTimer = attackInterval; // Força o primeiro ataque a acontecer imediatamente
    }

    public void DisengageCombat()
    {
        combatTarget = null;
        attackTimer = 0f;
    }

    void HandleAutoCombat()
    {
        if (combatTarget == null) return;

        Health targetHealth = combatTarget.GetComponent<Health>();
        if (targetHealth == null || targetHealth.IsDead)
        {
            DisengageCombat(); // Inimigo morreu, limpa o alvo
            return;
        }

        EnemyController enemy = combatTarget.GetComponent<EnemyController>();
        if (enemy == null) return;

        // Checa distância Manhattan. Se for maior que 1, o inimigo não está mais adjacente.
        int distance = Mathf.Abs(gridPosition.x - enemy.gridPosition.x) + Mathf.Abs(gridPosition.y - enemy.gridPosition.y);
        
        if (distance > 1)
        {
            DisengageCombat(); // Inimigo saiu de perto
            return;
        }

        // Loop de dano por tempo
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            DamageData damage = combatStats.GenerateDamage();
            targetHealth.TakeDamage(damage);
        }
    }

    // Busca lógica via Grid, removendo a necessidade de Physics.OverlapSphere
    GameObject FindEnemyAt(Vector2Int pos)
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy.gridPosition == pos) return enemy.gameObject;
        }
        return null;
    }

    public void OnAttackedBy(GameObject attacker)
    {
        // Se o inimigo bater no player primeiro e o player estiver parado, ele revida automaticamente
        if (combatTarget == null)
        {
            EngageTarget(attacker);
        }
    }
}

// --- HealthBar.cs ---
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Health targetHealth;
    public Image fillImage;

    private void Update()
    {
        if (targetHealth == null) return;

        float percent = (float)targetHealth.currentHP / targetHealth.maxHP;
        fillImage.fillAmount = percent;
    }
}

// --- Health.cs ---
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    [Header("UI Reference")]
    [Tooltip("Prefab da barra de vida (CanvasHealthBar)")]
    public GameObject healthBarPrefab; 

    [Header("Visual Feedback")]
    [Tooltip("Arraste o Prefab do circulo vermelho de dano aqui")]
    public GameObject damageSplashPrefab;
    [Tooltip("Altura para o círculo não ficar afundado no chão")]
    public Vector3 splashOffset = new Vector3(0, 1.5f, 0);

    void Awake()
    {
        // Garante que a vida inicie cheia
        if (maxHP <= 0) maxHP = 1;
        if (currentHP <= 0) currentHP = maxHP;
        
        // A SOLUÇÃO: Cria a barra de vida fisicamente no mundo 3D
        if (healthBarPrefab != null)
        {
            // Instancia a barra de vida como "filha" do Player/Inimigo para que ela ande junto com ele
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            
            // Procura o script HealthBar dentro da barra que acabou de nascer e liga ele a esta vida
            HealthBar barScript = healthBarInstance.GetComponentInChildren<HealthBar>();
            if (barScript != null)
            {
                barScript.targetHealth = this; 
            }
        }
    }

    public void TakeDamage(DamageData damage)
    {
        if (IsDead) return;

        int finalDamage = damage.FinalDamage();
        currentHP -= finalDamage;
        
        // Trava a vida no mínimo zero para não bugar a UI
        if (currentHP < 0) currentHP = 0;
        
        Debug.Log($"[{gameObject.name}] tomou {finalDamage} de dano. (HP: {currentHP})");

        // Instancia o feedback visual de forma segura
        if (damageSplashPrefab != null)
        {
            Instantiate(damageSplashPrefab, transform.position + splashOffset, Quaternion.identity);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Die();
        }
        else
        {
            PlayerController player = GetComponent<PlayerController>();
            if (player != null)
            {
                // Libera o grid antes de destruir o player
                GridManager.Instance.SetTile(player.gridPosition.x, player.gridPosition.y, TileType.Empty);
            }
            Destroy(gameObject);
        }
    }
}

// --- EnemyData.cs ---
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public GameObject prefab;

    [Header("Spawn Settings")]
    public int spawnWeight = 1; // chance relativa

    [Header("Stats")]
    public int maxHP = 5;
    public int strength = 2;
    public int minDamage = 1;
    public int maxDamage = 3;
}

// --- EnemyController.cs ---
using UnityEngine;
using System.Collections.Generic; // Necessário para List e HashSet usados no A*

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class EnemyController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("AI Movement Settings")]
    [Tooltip("Intervalo em segundos entre cada passo do inimigo.")]
    public float moveInterval = 0.5f;
    private float moveTimer = 0f;
    [Tooltip("Distância máxima que o inimigo detecta o player.")]
    public int visionRange = 8;

    [Header("AI Combat Settings")]
    [Tooltip("Intervalo em segundos entre cada ataque do inimigo.")]
    public float attackInterval = 1.0f;
    private float attackTimer = 0f;
    
    [Header("Visuals")]
    public float moveSpeed = 10f;

    private Health health;
    private CombatStats combatStats;

    private enum State { Idle, Chase }
    private State currentState = State.Idle;

    // Classe interna para os cálculos do algoritmo A*
    private class PathNode
    {
        public Vector2Int Position;
        public int GCost; // Custo do início até aqui
        public int HCost; // Custo estimado (Manhattan) daqui até o alvo
        public int FCost => GCost + HCost;
        public PathNode Parent; // Usado para refazer o caminho de volta

        public PathNode(Vector2Int pos) { Position = pos; }
    }

    void Start()
    {
        health = GetComponent<Health>();
        combatStats = GetComponent<CombatStats>();

        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);

        attackTimer = attackInterval; 
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        UpdateVisualPosition();

        Vector2Int playerPos = GridManager.Instance.PlayerPosition;
        int distToPlayer = ManhattanDistance(gridPosition, playerPos);

        // 1. Loop de Combate
        if (distToPlayer == 1)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f; 
                AttackPlayer();
            }
        }
        else
        {
            attackTimer = attackInterval;
        }

        // 2. Loop de Movimento
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            
            if (distToPlayer > 1) 
            {
                PerformMovement(playerPos, distToPlayer);
            }
        }
    }

    void PerformMovement(Vector2Int playerPos, int distToPlayer)
    {
        if (distToPlayer <= visionRange)
        {
            currentState = State.Chase;
            ChasePlayer(playerPos);
        }
        else
        {
            currentState = State.Idle;
            RandomMove();
        }
    }

    void AttackPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                DamageData damage = combatStats.GenerateDamage();
                playerHealth.TakeDamage(damage);
                player.OnAttackedBy(gameObject);
            }
        }
    }

    void ChasePlayer(Vector2Int playerPos)
    {
        // Usa o algoritmo A* para achar o melhor caminho contornando paredes
        List<Vector2Int> path = FindPath(gridPosition, playerPos);

        if (path != null && path.Count > 0)
        {
            Vector2Int nextStep = path[0];

            // Garante que não vai tentar pisar em cima do player (o combate lida com a colisão lógica)
            if (nextStep != playerPos && GridManager.Instance.IsWalkable(nextStep.x, nextStep.y))
            {
                MoveTo(nextStep);
            }
        }
        else
        {
            // Se não há caminho possível (player cercado de paredes), o inimigo tenta pelo menos andar aleatório
            RandomMove();
        }
    }

    // --- ALGORITMO A* (A-STAR) ---
    List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode(startPos) { GCost = 0, HCost = ManhattanDistance(startPos, targetPos) };
        openList.Add(startNode);

        int iterations = 0;
        int maxIterations = 300; // Proteção contra loops infinitos em mapas muito complexos

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Pega o nó com o menor custo F
            PathNode currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost || (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            // Se achou o alvo, reconstrói o caminho
            if (currentNode.Position == targetPos)
            {
                return RetracePath(startNode, currentNode);
            }

            // Checa vizinhos nas 4 direções
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentNode.Position + dir;

                if (!GridManager.Instance.IsInsideGrid(neighborPos.x, neighborPos.y)) continue;
                if (closedList.Contains(neighborPos)) continue;

                // O vizinho precisa ser "caminhável" OU ser a posição do próprio alvo (para o caminho poder chegar até ele)
                bool isWalkable = GridManager.Instance.IsWalkable(neighborPos.x, neighborPos.y) || neighborPos == targetPos;
                if (!isWalkable) continue;

                int tentativeGCost = currentNode.GCost + 1;

                PathNode neighborNode = openList.Find(n => n.Position == neighborPos);
                if (neighborNode == null)
                {
                    neighborNode = new PathNode(neighborPos);
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = ManhattanDistance(neighborPos, targetPos);
                    neighborNode.Parent = currentNode;
                    openList.Add(neighborNode);
                }
                else if (tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.Parent = currentNode;
                }
            }
        }
        
        return null; // Caminho não encontrado
    }

    List<Vector2Int> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Reverse(); // Inverte para que o primeiro item da lista seja o próximo passo
        return path;
    }
    // -----------------------------

    void RandomMove()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Vector2Int randomDir = dirs[Random.Range(0, dirs.Length)];
        Vector2Int targetPos = gridPosition + randomDir;

        if (GridManager.Instance.IsInsideGrid(targetPos.x, targetPos.y) && 
            GridManager.Instance.IsWalkable(targetPos.x, targetPos.y))
        {
            MoveTo(targetPos);
        }
    }

    void MoveTo(Vector2Int newPos)
    {
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        gridPosition = newPos;
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);
    }

    void UpdateVisualPosition()
    {
        Vector3 targetWorldPos = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
    }

    public void Die()
    {
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        Destroy(gameObject);
    }

    int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}

// --- DamageData.cs ---
using UnityEngine;

[System.Serializable]
public class DamageData
{
    public int baseDamage;
    public bool isCritical;
    public float criticalMultiplier;

    public int FinalDamage()
    {
        if (isCritical)
            return Mathf.RoundToInt(baseDamage * criticalMultiplier);

        return baseDamage;
    }
}

// --- CombatStats.cs ---
using UnityEngine;

public class CombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int strength = 5;
    public int minDamage = 2;
    public int maxDamage = 5;

    [Header("Crit Settings")]
    [Range(0f, 1f)]
    public float critChance = 0.1f;
    public float critMultiplier = 2f;

    public DamageData GenerateDamage()
    {
        DamageData dmg = new DamageData();

        int roll = Random.Range(minDamage, maxDamage + 1);
        dmg.baseDamage = roll + strength;

        dmg.isCritical = Random.value < critChance;
        dmg.criticalMultiplier = critMultiplier;

        return dmg;
    }
}

// --- TurnManager.cs ---
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public bool playerTurn = true;

    public delegate void OnEnemyTurn();
    public event OnEnemyTurn EnemyTurnEvent;

    private void Awake()
    {
        Instance = this;
    }

    public void EndPlayerTurn()
    {
        playerTurn = false;
        EnemyTurnEvent?.Invoke();
        Invoke(nameof(StartPlayerTurn), 0.2f);
    }

    void StartPlayerTurn()
    {
        playerTurn = true;
    }
}

// --- EnemySpawner.cs ---
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<EnemyData> enemyTypes;
    public int enemyCount = 20;

    public void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnSingleEnemy();
        }
    }

    void SpawnSingleEnemy()
    {
        Vector2Int pos = GetRandomFreeTile();

        if (pos == Vector2Int.zero)
            return;

        EnemyData selected = GetWeightedRandomEnemy();

        GameObject enemyObj = Instantiate(
            selected.prefab,
            GridManager.Instance.GridToWorld(pos.x, pos.y),
            Quaternion.identity
            
        );

        GridManager.Instance.SetTile(pos.x, pos.y, TileType.Enemy);

        // Aplicar stats
        Health hp = enemyObj.GetComponent<Health>();
        CombatStats stats = enemyObj.GetComponent<CombatStats>();

        if (hp != null)
            hp.maxHP = selected.maxHP;

        if (stats != null)
        {
            stats.strength = selected.strength;
            stats.minDamage = selected.minDamage;
            stats.maxDamage = selected.maxDamage;
        }
    }

    Vector2Int GetRandomFreeTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(1, GridManager.Instance.width - 1);
            int y = Random.Range(1, GridManager.Instance.height - 1);

            if (GridManager.Instance.IsWalkable(x, y))
                return new Vector2Int(x, y);
        }

        return Vector2Int.zero;
    }

    EnemyData GetWeightedRandomEnemy()
    {
        int totalWeight = 0;

        foreach (var e in enemyTypes)
            totalWeight += e.spawnWeight;

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var e in enemyTypes)
        {
            cumulative += e.spawnWeight;

            if (roll < cumulative)
                return e;
        }

        return enemyTypes[0];
    }
}

// --- CameraFollow.cs ---
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("O Transform do Player que a câmera deve seguir.")]
    public Transform target;
    
    [Tooltip("Posição da câmera em relação ao alvo (Padrão: 0, 20, -15).")]
    public Vector3 offset = new Vector3(0, 20f, -15f);

    [Header("Movement Settings")]
    [Tooltip("Tempo aproximado para a câmera alcançar o alvo. Valores menores deixam a câmera mais ágil.")]
    public float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero; // Usado internamente pelo SmoothDamp

    [Header("Grid Bounds")]
    [Tooltip("Ative para impedir que a câmera siga o jogador para fora dos limites do grid.")]
    public bool useGridBounds = true;
    
    [Tooltip("Limites mínimos do grid no mundo (X, Z).")]
    public Vector2 minBounds = new Vector2(0, 0);
    
    [Tooltip("Limites máximos do grid no mundo (X, Z). Para um grid 100x100, use 99, 99.")]
    public Vector2 maxBounds = new Vector2(99, 99);

    [Header("Collision Handling")]
    [Tooltip("Ative para impedir que a câmera atravesse paredes ou cenários.")]
    public bool avoidClipping = true;
    
    [Tooltip("A Layer que representa os obstáculos (ex: Walls).")]
    public LayerMask obstacleLayer;
    
    [Tooltip("Distância mínima que a câmera deve manter da parede ao colidir.")]
    public float collisionOffset = 0.5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Pega a posição base do alvo (Player)
        Vector3 focusPosition = target.position;

        // 2. Aplica os limites do Grid (Bounds) para a posição de foco
        if (useGridBounds)
        {
            focusPosition.x = Mathf.Clamp(focusPosition.x, minBounds.x, maxBounds.x);
            focusPosition.z = Mathf.Clamp(focusPosition.z, minBounds.y, maxBounds.y);
        }

        // 3. Calcula a posição ideal da câmera com o offset
        Vector3 desiredPosition = focusPosition + offset;

        // 4. Prevenção de Clipping (Colisão) com obstáculos
        if (avoidClipping)
        {
            RaycastHit hit;
            // Lança um raio do jogador (foco) até onde a câmera quer ir
            if (Physics.Linecast(focusPosition, desiredPosition, out hit, obstacleLayer))
            {
                // Se bater em uma parede, a posição desejada passa a ser o ponto de impacto,
                // afastado levemente na direção do jogador para não entrar na malha do 3D
                desiredPosition = hit.point + (focusPosition - desiredPosition).normalized * collisionOffset;
            }
        }

        // 5. Move a câmera suavemente para a posição final usando SmoothDamp
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothTime
        );
    }
}
``` 

Salve este conteúdo em qualquer local (por exemplo `e:\unity\projetos\jogoDungeonCrawler\fullCode.cs`) e terá uma visão contínua de todos os scripts usados no projecto, com comentários de separação.