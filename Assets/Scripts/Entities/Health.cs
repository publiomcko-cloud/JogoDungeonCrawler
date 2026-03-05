using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP;
    public bool IsDead => currentHP <= 0;

    [Header("UI Reference")]
    [Tooltip("Prefab da barra de vida (CanvasHealthBar)")]
    public GameObject healthBarPrefab; 

    [Header("Visual Feedback")]
    [Tooltip("Arraste o Prefab do circulo vermelho de dano aqui")]
    public GameObject damageSplashPrefab;
    [Tooltip("Altura para o círculo não ficar afundado no chão")]
    public Vector3 splashOffset = new Vector3(0, 1.5f, 0);

    void Awake()
    {
        // Garante que a vida inicie cheia
        if (maxHP <= 0) maxHP = 1;
        if (currentHP <= 0) currentHP = maxHP;
        
        // A SOLUÇÃO: Cria a barra de vida fisicamente no mundo 3D
        if (healthBarPrefab != null)
        {
            // Instancia a barra de vida como "filha" do Player/Inimigo para que ela ande junto com ele
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            
            // Procura o script HealthBar dentro da barra que acabou de nascer e liga ele a esta vida
            HealthBar barScript = healthBarInstance.GetComponentInChildren<HealthBar>();
            if (barScript != null)
            {
                barScript.targetHealth = this; 
            }
        }
    }

    public void TakeDamage(DamageData damage)
    {
        if (IsDead) return;

        int finalDamage = damage.FinalDamage();
        currentHP -= finalDamage;
        
        // Trava a vida no mínimo zero para não bugar a UI
        if (currentHP < 0) currentHP = 0;
        
        Debug.Log($"[{gameObject.name}] tomou {finalDamage} de dano. (HP: {currentHP})");

        // Instancia o feedback visual de forma segura
        if (damageSplashPrefab != null)
        {
            Instantiate(damageSplashPrefab, transform.position + splashOffset, Quaternion.identity);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Die();
        }
        else
        {
            PlayerController player = GetComponent<PlayerController>();
            if (player != null)
            {
                // Libera o grid antes de destruir o player
                GridManager.Instance.SetTile(player.gridPosition.x, player.gridPosition.y, TileType.Empty);
            }
            Destroy(gameObject);
        }
    }
}