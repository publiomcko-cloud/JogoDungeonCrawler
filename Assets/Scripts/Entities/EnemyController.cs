using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class EnemyController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("AI Movement Settings")]
    public float moveInterval = 0.5f;
    private float moveTimer = 0f;
    public int visionRange = 8;

    [Header("AI Combat Settings")]
    public float attackInterval = 1.0f;
    private float attackTimer = 0f;
    
    [Header("Visuals")]
    public float moveSpeed = 10f;
    
    [Header("Fog of War")]
    [Tooltip("Distância máxima em tiles para o inimigo aparecer na tela")]
    public int visibleToPlayerRange = 7; 
    private bool isVisible = true;

    private Health health;
    private CombatStats combatStats;

    private enum State { Idle, Chase }
    private State currentState = State.Idle;

    private class PathNode
    {
        public Vector2Int Position;
        public int GCost; 
        public int HCost; 
        public int FCost => GCost + HCost;
        public PathNode Parent;

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
        UpdateFogOfWarVisibility(); // NOVO: Checa se deve renderizar o inimigo

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

    // --- NOVA FUNÇÃO: FOG OF WAR ---
    void UpdateFogOfWarVisibility()
    {
        // Calcula a distância real no grid até o jogador
        int dist = ManhattanDistance(gridPosition, GridManager.Instance.PlayerPosition);
        bool shouldBeVisible = dist <= visibleToPlayerRange;

        // Só faz a operação pesada de ligar/desligar componentes se o estado mudar
        if (shouldBeVisible != isVisible)
        {
            isVisible = shouldBeVisible;

            // Liga ou desliga todos os renderizadores 3D/2D do inimigo
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach(var r in renderers)
            {
                r.enabled = isVisible;
            }

            // Liga ou desliga as UIs (como a Barra de Vida)
            Canvas[] canvases = GetComponentsInChildren<Canvas>();
            foreach(var c in canvases)
            {
                c.enabled = isVisible;
            }
        }
    }
    // --------------------------------

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
        List<Vector2Int> path = FindPath(gridPosition, playerPos);

        if (path != null && path.Count > 0)
        {
            Vector2Int nextStep = path[0];

            if (nextStep != playerPos && GridManager.Instance.IsWalkable(nextStep.x, nextStep.y))
            {
                MoveTo(nextStep);
            }
        }
        else
        {
            RandomMove();
        }
    }

    List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode(startPos) { GCost = 0, HCost = ManhattanDistance(startPos, targetPos) };
        openList.Add(startNode);

        int iterations = 0;
        int maxIterations = 300; 

        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

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

            if (currentNode.Position == targetPos)
            {
                return RetracePath(startNode, currentNode);
            }

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentNode.Position + dir;

                if (!GridManager.Instance.IsInsideGrid(neighborPos.x, neighborPos.y)) continue;
                if (closedList.Contains(neighborPos)) continue;

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
        
        return null; 
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
        path.Reverse(); 
        return path;
    }

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