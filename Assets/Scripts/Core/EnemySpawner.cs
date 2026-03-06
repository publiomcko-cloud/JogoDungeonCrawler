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

        // CORREÇÃO: Atualizado para a nova arquitetura de Status Base + Bónus
        if (hp != null)
        {
            hp.baseMaxHP = selected.maxHP; // Usa a nova variável "natural"
            hp.UpdateBonusHP(0);           // Recalcula o maxHP total (Base + 0 de armadura)
            hp.currentHP = hp.maxHP;       // Garante que o inimigo nasce com a vida cheia
        }

        if (stats != null)
        {
            stats.baseStrength = selected.strength;   // Atualizado para "base"
            stats.baseMinDamage = selected.minDamage; // Atualizado para "base"
            stats.baseMaxDamage = selected.maxDamage; // Atualizado para "base"
            
            stats.RecalculateStats(); // Força a matemática a rodar para preencher os valores Totais!
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