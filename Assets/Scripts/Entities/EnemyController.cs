using UnityEngine;

[RequireComponent(typeof(Health), typeof(CombatStats))]
public class EnemyController : MonoBehaviour
{
    [Header("Grid Logic")]
    public Vector2Int gridPosition;
    
    [Header("AI Movement Settings")]
    [Tooltip("Intervalo em segundos entre cada passo do inimigo.")]
    public float moveInterval = 0.5f;
    private float moveTimer = 0f;
    public int visionRange = 8;

    [Header("AI Combat Settings")]
    [Tooltip("Intervalo em segundos entre cada ataque do inimigo. Futuramente afetado por stats.")]
    public float attackInterval = 1.0f;
    private float attackTimer = 0f;
    
    [Header("Visuals")]
    public float moveSpeed = 10f;

    private Health health;
    private CombatStats combatStats;

    private enum State { Idle, Chase }
    private State currentState = State.Idle;

    void Start()
    {
        health = GetComponent<Health>();
        combatStats = GetComponent<CombatStats>();

        // Força a posição base no Grid
        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);

        // Prepara o ataque para que o primeiro hit seja imediato ao colar no player
        attackTimer = attackInterval; 
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        UpdateVisualPosition();

        Vector2Int playerPos = GridManager.Instance.PlayerPosition;
        int distToPlayer = ManhattanDistance(gridPosition, playerPos);

        // 1. Loop de Combate (Totalmente independente do movimento)
        if (distToPlayer == 1)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f; // Reseta o timer após bater
                AttackPlayer();
            }
        }
        else
        {
            // Se o player se afastar, o inimigo "segura" o ataque para bater assim que chegar perto de novo
            attackTimer = attackInterval;
        }

        // 2. Loop de Movimento (Tempo Real)
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            
            // Só caminha se não estiver ativamente em combate corpo a corpo
            if (distToPlayer > 1) 
            {
                PerformMovement(playerPos, distToPlayer);
            }
        }
    }

    void PerformMovement(Vector2Int playerPos, int distToPlayer)
    {
        // Lógica de Estado (Chase ou Idle)
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
                
                // Gatilho crucial: avisa o player que foi atacado
                player.OnAttackedBy(gameObject);
            }
        }
    }

    void ChasePlayer(Vector2Int playerPos)
    {
        // Fallback guloso direcional
        Vector2Int nextStep = gridPosition;
        if (Mathf.Abs(playerPos.x - gridPosition.x) > Mathf.Abs(playerPos.y - gridPosition.y))
        {
            nextStep.x += (playerPos.x > gridPosition.x) ? 1 : -1;
        }
        else
        {
            nextStep.y += (playerPos.y > gridPosition.y) ? 1 : -1;
        }

        if (GridManager.Instance.IsWalkable(nextStep.x, nextStep.y))
        {
            MoveTo(nextStep);
        }
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
        // Regra 6 de Morte: Libera o tile ANTES de destruir
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);
        Destroy(gameObject);
    }

    int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}