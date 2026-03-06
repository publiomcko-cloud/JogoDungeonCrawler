using System.Collections.Generic;
using UnityEngine;
using System; // Necessário para os Eventos (Action)

public class Inventory : MonoBehaviour
{
    [Header("Mochila (Backpack)")]
    public List<ItemData> backpack = new List<ItemData>();
    public int maxBackpackSize = 20; // Limite de itens na mochila

    [Header("Equipamentos (Equipped)")]
    // Dicionário que liga o Slot (ex: Helmet) ao Item vestido (ex: IronHelmet)
    private Dictionary<EquipSlot, ItemData> equippedItems = new Dictionary<EquipSlot, ItemData>();

    // Evento que vai avisar outros scripts (como o CombatStats e a UI) que os equipamentos mudaram
    public event Action OnEquipmentChanged;
    public event Action OnBackpackChanged;

    void Awake()
    {
        // Inicializa o dicionário de equipamentos com todos os slots vazios
        foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
        {
            if (slot != EquipSlot.None)
            {
                equippedItems.Add(slot, null);
            }
        }
    }

    // --- LÓGICA DA MOCHILA ---

    public bool AddItem(ItemData item)
    {
        if (backpack.Count >= maxBackpackSize)
        {
            Debug.Log("Inventário cheio!");
            return false;
        }

        backpack.Add(item);
        Debug.Log($"Adicionado: {item.itemName} à mochila.");
        OnBackpackChanged?.Invoke();
        return true;
    }

    public void RemoveItem(ItemData item)
    {
        if (backpack.Contains(item))
        {
            backpack.Remove(item);
            OnBackpackChanged?.Invoke();
        }
    }

    // --- LÓGICA DE EQUIPAR/DESEQUIPAR ---

    public void EquipItem(ItemData itemToEquip)
    {
        if (itemToEquip.itemType != ItemType.Equipment && itemToEquip.itemType != ItemType.Weapon)
        {
            Debug.LogWarning("Este item não pode ser equipado!");
            return;
        }

        EquipSlot slot = itemToEquip.equipSlot;

        // Se já houver algo vestido nesse slot, desequipa e devolve para a mochila
        if (equippedItems[slot] != null)
        {
            UnequipItem(slot);
        }

        // Tira o novo item da mochila e veste no corpo
        RemoveItem(itemToEquip);
        equippedItems[slot] = itemToEquip;
        
        Debug.Log($"Equipado: {itemToEquip.itemName} no slot {slot}");
        
        // Avisa o jogo que os atributos devem ser recalculados
        OnEquipmentChanged?.Invoke(); 
    }

    public void UnequipItem(EquipSlot slot)
    {
        ItemData itemToUnequip = equippedItems[slot];

        if (itemToUnequip != null)
        {
            // Tenta devolver para a mochila
            if (backpack.Count < maxBackpackSize)
            {
                backpack.Add(itemToUnequip);
                equippedItems[slot] = null; // Esvazia o slot do corpo
                
                Debug.Log($"Desequipado: {itemToUnequip.itemName} do slot {slot}");
                
                OnEquipmentChanged?.Invoke();
                OnBackpackChanged?.Invoke();
            }
            else
            {
                Debug.Log("Não há espaço na mochila para desequipar isso!");
                // Futuramente, podemos fazer o item cair no chão (Drops) se a mochila estiver cheia
            }
        }
    }

    // Função auxiliar para os outros scripts (como os Status e a UI) lerem o que está vestido
    public ItemData GetEquippedItem(EquipSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            return equippedItems[slot];
        }
        return null;
    }
}