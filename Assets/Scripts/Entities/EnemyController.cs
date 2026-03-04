using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public int visionRange = 8;
    public float moveInterval = 0.5f;

    private Vector2Int gridPosition;
    private float moveTimer;

    private enum State { Idle, Chase }
    private State currentState = State.Idle;

    

    private void Start()
    {
        gridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);

        UpdateWorldPosition();
    }

    private void Update()
    {
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            UpdateAI();
        }
    }

    void UpdateAI()
    {
        if (CanSeePlayer())
            currentState = State.Chase;
        else
            currentState = State.Idle;

        if (currentState == State.Chase)
            MoveAlongPath();
        else
            MoveRandom();
    }

    // =========================
    // VISION SYSTEM
    // =========================

    bool CanSeePlayer()
    {
        Vector2Int playerPos = GridManager.Instance.PlayerPosition;

        if (Vector2Int.Distance(gridPosition, playerPos) > visionRange)
            return false;

        return HasLineOfSight(playerPos);
    }

    bool HasLineOfSight(Vector2Int target)
    {
        List<Vector2Int> line = GetLine(gridPosition, target);

        foreach (var point in line)
        {
            if (GridManager.Instance.GetTile(point.x, point.y) == TileType.Wall)
                return false;
        }

        return true;
    }

    // Bresenham
    List<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> points = new List<Vector2Int>();

        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;

        int err = dx - dy;

        int x = start.x;
        int y = start.y;

        while (true)
        {
            points.Add(new Vector2Int(x, y));

            if (x == end.x && y == end.y)
                break;

            int e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return points;
    }

    // =========================
    // PATHFINDING (A*)
    // =========================

    void MoveAlongPath()
    {
        Vector2Int playerPos = GridManager.Instance.PlayerPosition;

        List<Vector2Int> path = AStar(gridPosition, playerPos);

        if (path != null && path.Count > 1)
        {
            TryMove(path[1] - gridPosition);
        }
    }

    List<Vector2Int> AStar(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> openSet = new List<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();

        openSet.Add(start);
        gScore[start] = 0;

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet[0];

            foreach (var node in openSet)
                if (gScore[node] + Heuristic(node, goal) < gScore[current] + Heuristic(current, goal))
                    current = node;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                if (!GridManager.Instance.IsWalkable(neighbor.x, neighbor.y) &&
                    neighbor != goal)
                    continue;

                int tentativeG = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (tentativeG >= gScore.GetValueOrDefault(neighbor, int.MaxValue))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
            }
        }

        return null;
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> totalPath = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> GetNeighbors(Vector2Int node)
    {
        return new List<Vector2Int>
        {
            node + Vector2Int.up,
            node + Vector2Int.down,
            node + Vector2Int.left,
            node + Vector2Int.right
        };
    }

    // =========================
    // MOVEMENT
    // =========================

    void MoveRandom()
    {
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        TryMove(directions[Random.Range(0, directions.Length)]);
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int target = gridPosition + direction;

        if (!GridManager.Instance.IsInsideGrid(target.x, target.y))
            return;

        TileType tile = GridManager.Instance.GetTile(target.x, target.y);

        // SE FOR PLAYER → ATACA
        if (tile == TileType.Player)
        {
            AttackPlayer(target);
            return;
        }

        if (GridManager.Instance.IsWalkable(target.x, target.y))
        {
            GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);

            gridPosition = target;

            GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Enemy);

            UpdateWorldPosition();
        }
    }
    void AttackPlayer(Vector2Int playerPos)
    {
        Collider[] hits = Physics.OverlapSphere(
            GridManager.Instance.GridToWorld(playerPos.x, playerPos.y),
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

    public void Die()
    {
        // Libera o tile lógico
        GridManager.Instance.SetTile(gridPosition.x, gridPosition.y, TileType.Empty);

        Destroy(gameObject);
    }
}