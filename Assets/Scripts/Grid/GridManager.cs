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