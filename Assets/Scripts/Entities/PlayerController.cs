using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector2Int gridPosition;

    private void Start()
    {
        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
        GridManager.Instance.SetPlayerPosition(gridPosition);

        UpdateWorldPosition();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
    }

    void TryMove(Vector2Int direction)
{
    Vector2Int target = gridPosition + direction;

    if (!GridManager.Instance.IsInsideGrid(target.x, target.y))
        return;

    TileType tile = GridManager.Instance.GetTile(target.x, target.y);

    // SE FOR INIMIGO → ATACA
    if (tile == TileType.Enemy)
    {
        Attack(target);
        return;
    }

    // SE FOR CAMINHÁVEL → MOVE
    if (GridManager.Instance.IsWalkable(target.x, target.y))
    {
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);

        gridPosition = target;

        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Player);
        GridManager.Instance.SetPlayerPosition(gridPosition);

        UpdateWorldPosition();
    }
}

    void Attack(Vector2Int targetPos)
    {
        Collider[] hits = Physics.OverlapSphere(
            GridManager.Instance.GridToWorld(targetPos.x, targetPos.y),
            0.1f
        );

        foreach (var hit in hits)
        {
            Health hp = hit.GetComponent<Health>();
            CombatStats myStats = GetComponent<CombatStats>();

            if (hp != null && myStats != null)
            {
                DamageData damage = myStats.GenerateDamage();
                hp.TakeDamage(damage);
                break;
            }
        }
    }

    void UpdateWorldPosition()
    {
        transform.position = GridManager.Instance.GridToWorld(gridPosition.x, gridPosition.y);
    }
}