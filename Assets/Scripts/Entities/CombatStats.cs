using UnityEngine;
using System;

[RequireComponent(typeof(Health))]
public class CombatStats : MonoBehaviour
{
    [Header("Base Stats (Naturais)")]
    public int baseStrength = 5;
    public int baseMinDamage = 1;
    public int baseMaxDamage = 3;
    public int baseCritChance = 5;

    [Header("Total Stats (Base + Itens) - READ ONLY")]
    public int totalStrength;
    public int totalMinDamage;
    public int totalMaxDamage;
    public int currentBonusHP; // Guardado apenas para visualização no Inspector

    private Inventory inventory;
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        inventory = GetComponent<Inventory>(); // Tenta achar o inventário (Inimigos não terão, e tudo bem!)
    }

    void Start()
    {
        // Se for o Player (tem inventário), avisa que toda vez que trocar de roupa, deve recalcular os status
        if (inventory != null)
        {
            inventory.OnEquipmentChanged += RecalculateStats;
        }
        
        RecalculateStats(); // Faz o primeiro cálculo ao nascer
    }

    void OnDestroy()
    {
        // Limpeza de segurança para evitar erros se o objeto for destruído
        if (inventory != null)
        {
            inventory.OnEquipmentChanged -= RecalculateStats;
        }
    }

    // Função mágica que varre o corpo do jogador e soma os atributos
    public void RecalculateStats()
    {
        int bonusStr = 0;
        int bonusMin = 0;
        int bonusMax = 0;
        int bonusHP = 0;

        // Se este objeto tiver um inventário (Player), soma os itens
        if (inventory != null)
        {
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                if (slot == EquipSlot.None) continue;

                ItemData item = inventory.GetEquippedItem(slot);
                if (item != null)
                {
                    bonusStr += item.bonusStrength;
                    bonusMin += item.bonusMinDamage;
                    bonusMax += item.bonusMaxDamage;
                    bonusHP += item.bonusMaxHP;
                }
            }
        }

        // Aplica as somatórias matemáticas
        totalStrength = baseStrength + bonusStr;
        totalMinDamage = baseMinDamage + bonusMin;
        totalMaxDamage = baseMaxDamage + bonusMax;
        currentBonusHP = bonusHP;

        // Manda a nova Vida Máxima para o script Health
        if (health != null)
        {
            health.UpdateBonusHP(bonusHP);
        }
        
        Debug.Log($"Status Atualizados! Força Total: {totalStrength} | Dano: {totalMinDamage}-{totalMaxDamage}");
    }

    public DamageData GenerateDamage()
    {
        // Agora o dano é gerado usando os TOTAIS (Força + Arma), e não mais apenas a base!
        int damage = UnityEngine.Random.Range(totalMinDamage, totalMaxDamage + 1) + totalStrength;
        
        bool isCrit = UnityEngine.Random.Range(0, 100) < baseCritChance; 
        if (isCrit) damage *= 2; // Crítico simples: dano em dobro

        return new DamageData(damage, isCrit, false, false);
    }
}