using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Referências Core")]
    public Inventory playerInventory;
    public GameObject inventoryPanel; // A janela inteira do inventário

    [Header("Mochila (Backpack)")]
    public Transform backpackParent;  // Onde os slots da mochila vão nascer
    public GameObject slotPrefab;     // O quadradinho vazio salvo como Prefab

    [Header("Corpo (Equipamentos)")]
    public ItemSlotUI helmetSlot;
    public ItemSlotUI chestSlot;
    public ItemSlotUI pantsSlot;
    public ItemSlotUI bootsSlot;
    public ItemSlotUI glovesSlot;
    public ItemSlotUI ring1Slot;
    public ItemSlotUI ring2Slot;
    public ItemSlotUI weaponSlot;

    void Start()
    {
        if (playerInventory != null)
        {
            // Ouve os eventos do jogador: sempre que o inventário mudar, atualiza a UI
            playerInventory.OnBackpackChanged += UpdateUI;
            playerInventory.OnEquipmentChanged += UpdateUI;
        }

        InitializeEquipmentSlots();
        inventoryPanel.SetActive(false); // Começa o jogo com o inventário fechado
    }

    void Update()
    {
        // Abre/Fecha o inventário com a tecla 'I'
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            if (inventoryPanel.activeSelf)
            {
                UpdateUI();
            }
        }
    }

    void InitializeEquipmentSlots()
    {
        // Ensina a cada slot fixo qual parte do corpo ele representa
        if (helmetSlot != null) helmetSlot.equipSlot = EquipSlot.Helmet;
        if (chestSlot != null) chestSlot.equipSlot = EquipSlot.Chest;
        if (pantsSlot != null) pantsSlot.equipSlot = EquipSlot.Pants;
        if (bootsSlot != null) bootsSlot.equipSlot = EquipSlot.Boots;
        if (glovesSlot != null) glovesSlot.equipSlot = EquipSlot.Gloves;
        if (ring1Slot != null) ring1Slot.equipSlot = EquipSlot.Ring1;
        if (ring2Slot != null) ring2Slot.equipSlot = EquipSlot.Ring2;
        if (weaponSlot != null) weaponSlot.equipSlot = EquipSlot.Weapon;
    }

    public void UpdateUI()
    {
        // 1. LIMPA E ATUALIZA A MOCHILA
        foreach (Transform child in backpackParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < playerInventory.backpack.Count; i++)
        {
            GameObject obj = Instantiate(slotPrefab, backpackParent);
            ItemSlotUI slotUI = obj.GetComponent<ItemSlotUI>();
            slotUI.Setup(playerInventory.backpack[i], this);
        }

        // 2. ATUALIZA O CORPO
        UpdateEquipSlot(helmetSlot, EquipSlot.Helmet);
        UpdateEquipSlot(chestSlot, EquipSlot.Chest);
        UpdateEquipSlot(pantsSlot, EquipSlot.Pants);
        UpdateEquipSlot(bootsSlot, EquipSlot.Boots);
        UpdateEquipSlot(glovesSlot, EquipSlot.Gloves);
        UpdateEquipSlot(ring1Slot, EquipSlot.Ring1);
        UpdateEquipSlot(ring2Slot, EquipSlot.Ring2);
        UpdateEquipSlot(weaponSlot, EquipSlot.Weapon);
    }

    void UpdateEquipSlot(ItemSlotUI slotUI, EquipSlot slotType)
    {
        if (slotUI != null)
        {
            ItemData equippedItem = playerInventory.GetEquippedItem(slotType);
            slotUI.Setup(equippedItem, this);
        }
    }

    // Chamado quando o jogador clica em qualquer quadradinho (mochila ou corpo)
    public void SlotClicked(ItemSlotUI slotUI, ItemData item)
    {
        if (item == null) return; // Clicou num espaço vazio

        if (slotUI.equipSlot == EquipSlot.None)
        {
            // Se clicou na mochila, TENTA EQUIPAR
            if (item.itemType == ItemType.Equipment || item.itemType == ItemType.Weapon)
            {
                playerInventory.EquipItem(item);
            }
            else if (item.itemType == ItemType.Consumable)
            {
                Debug.Log($"Consumiu poção: {item.itemName}");
                // No futuro, ligamos a cura do Health aqui
                playerInventory.RemoveItem(item);
            }
        }
        else
        {
            // Se clicou no corpo, DESEQUIPA (volta para a mochila)
            playerInventory.UnequipItem(slotUI.equipSlot);
        }
    }
}