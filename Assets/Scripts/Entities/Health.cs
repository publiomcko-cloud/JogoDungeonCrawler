using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 10;
    public int currentHP;

    public bool IsDead => currentHP <= 0;

    private void Awake()
    {
        currentHP = maxHP;
    }

public void TakeDamage(DamageData damage)
{
    int finalDamage = damage.FinalDamage();

    currentHP -= finalDamage;

    Debug.Log($"{gameObject.name} tomou {finalDamage} dano {(damage.isCritical ? "CRÍTICO!" : "")} HP atual: {currentHP}");

    if (currentHP <= 0)
    {
        Die();
    }
}

    void Die()
    {
        Debug.Log($"{gameObject.name} morreu.");

        Destroy(gameObject);
    }
}   