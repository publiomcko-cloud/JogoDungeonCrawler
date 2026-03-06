using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int baseMaxHP = 100; // Vida natural do personagem (sem armadura)
    public int maxHP;           // Vida total (Base + Bónus dos Itens)
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    [Header("UI Reference")]
    public GameObject healthBarPrefab; 

    [Header("Visual Feedback")]
    public GameObject damageSplashPrefab;
    public Vector3 splashOffset = new Vector3(0, 1.5f, 0);

    void Awake()
    {
        if (baseMaxHP <= 0) baseMaxHP = 1;
        maxHP = baseMaxHP;
        if (currentHP <= 0) currentHP = maxHP;
        
        if (healthBarPrefab != null)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            HealthBar barScript = healthBarInstance.GetComponentInChildren<HealthBar>();
            if (barScript != null) barScript.targetHealth = this; 
        }
    }

    // --- NOVA FUNÇÃO PARA O INVENTÁRIO ---
    public void UpdateBonusHP(int bonusHP)
    {
        // Recalcula a vida máxima (Vida do corpo + Vida da armadura)
        maxHP = baseMaxHP + bonusHP;
        
        // Se a vida atual ficar maior que o novo máximo (ex: tirou o elmo), ela desce para o máximo
        if (currentHP > maxHP) currentHP = maxHP;
    }

    public void TakeDamage(DamageData damage)
    {
        if (IsDead) return;

        int finalDamage = damage.FinalDamage();
        currentHP -= finalDamage;
        if (currentHP < 0) currentHP = 0;
        
        Debug.Log($"[{gameObject.name}] tomou {finalDamage} de dano. (HP: {currentHP}/{maxHP})");

        if (damageSplashPrefab != null)
        {
            Instantiate(damageSplashPrefab, transform.position + splashOffset, Quaternion.identity);
        }

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy != null) enemy.Die();
        else
        {
            PlayerController player = GetComponent<PlayerController>();
            if (player != null) GridManager.Instance.SetTile(player.gridPosition.x, player.gridPosition.y, TileType.Empty);
            Destroy(gameObject);
        }
    }
}