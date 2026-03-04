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