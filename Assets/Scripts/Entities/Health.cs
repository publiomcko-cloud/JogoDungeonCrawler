using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 10;
    public int currentHP;

    public bool IsDead => currentHP <= 0;

    public GameObject healthBarPrefab;
    private GameObject spawnedBar;
    

    private void Awake()
    {
        currentHP = maxHP;

        if (healthBarPrefab != null)
        {
            spawnedBar = Instantiate(healthBarPrefab, transform);
            spawnedBar.transform.localPosition = new Vector3(0, 2f, 0);

            HealthBar bar = spawnedBar.GetComponent<HealthBar>();
            bar.targetHealth = this;
        }
    }
public void TakeDamage(DamageData damage)
{
    int finalDamage = damage.FinalDamage();

    currentHP -= finalDamage;

    Debug.Log($"{gameObject.name} tomou {finalDamage} dano {(damage.isCritical ? "CRÍTICO!" : "")} HP atual: {currentHP}");

    if (currentHP <= 0)
    {
        EnemyController enemy = GetComponent<EnemyController>();

        if (enemy != null)
        {
            enemy.Die();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

    void Die()
    {
        Debug.Log($"{gameObject.name} morreu.");

        Destroy(gameObject);
    }
}   