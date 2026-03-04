using UnityEngine;

public class CombatStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int strength = 5;
    public int minDamage = 2;
    public int maxDamage = 5;

    [Header("Crit Settings")]
    [Range(0f, 1f)]
    public float critChance = 0.1f;
    public float critMultiplier = 2f;

    public DamageData GenerateDamage()
    {
        DamageData dmg = new DamageData();

        int roll = Random.Range(minDamage, maxDamage + 1);
        dmg.baseDamage = roll + strength;

        dmg.isCritical = Random.value < critChance;
        dmg.criticalMultiplier = critMultiplier;

        return dmg;
    }
}