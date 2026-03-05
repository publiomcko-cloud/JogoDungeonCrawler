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