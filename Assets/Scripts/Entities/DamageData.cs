using UnityEngine;

[System.Serializable]
public class DamageData
{
    public int baseDamage;
    public bool isCritical;
    public float criticalMultiplier;

    public int FinalDamage()
    {
        if (isCritical)
            return Mathf.RoundToInt(baseDamage * criticalMultiplier);

        return baseDamage;
    }
}