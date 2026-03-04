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