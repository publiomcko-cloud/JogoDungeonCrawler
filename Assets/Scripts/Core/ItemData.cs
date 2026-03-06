using UnityEngine;

// Define as categorias de itens
public enum ItemType 
{ 
    Consumable, // Poções, comidas
    Equipment,  // Armaduras
    Weapon      // Espadas, arcos
}

// Define os slots exatos que você solicitou
public enum EquipSlot 
{ 
    None,       // Para itens consumíveis ou lixo
    Helmet,     // Elmo
    Chest,      // Peito
    Pants,      // Calças
    Boots,      // Botas
    Gloves,     // Luvas
    Ring1,      // Anel 1
    Ring2,      // Anel 2
    Weapon      // Arma Principal
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName = "New Item";
    [TextArea(2, 4)]
    public string description = "Descrição do item.";
    public Sprite icon; // A imagem que vai aparecer no inventário
    public ItemType itemType;

    [Header("Equipment Settings (Only for Equip/Weapon)")]
    public EquipSlot equipSlot;

    [Header("Stat Bonuses")]
    [Tooltip("Bónus de força adicionado ao CombatStats")]
    public int bonusStrength = 0;
    
    [Tooltip("Vida máxima extra adicionada ao Health")]
    public int bonusMaxHP = 0;
    
    [Tooltip("Bónus no dano mínimo")]
    public int bonusMinDamage = 0;
    
    [Tooltip("Bónus no dano máximo")]
    public int bonusMaxDamage = 0;

    [Header("Consumable Settings")]
    [Tooltip("Quantidade de HP recuperada (se for poção)")]
    public int healAmount = 0;
}