using UnityEngine;

// Usamos struct em vez de class para ser mais leve na memória
public struct DamageData
{
    public int damageAmount;
    public bool isCrit;
    public bool isMiss;
    public bool isBlocked;

    // O construtor que o CombatStats estava a pedir
    public DamageData(int damageAmount, bool isCrit, bool isMiss, bool isBlocked)
    {
        this.damageAmount = damageAmount;
        this.isCrit = isCrit;
        this.isMiss = isMiss;
        this.isBlocked = isBlocked;
    }

    // Calcula o valor final real que vai ser subtraído da barra de vida
    public int FinalDamage()
    {
        if (isMiss) return 0; // Se falhou, zero dano
        
        int final = damageAmount;
        
        // Se bloqueou, corta o dano pela metade (exemplo de mecânica)
        if (isBlocked) final /= 2; 
        
        return final;
    }
}