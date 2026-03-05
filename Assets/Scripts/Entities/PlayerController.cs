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