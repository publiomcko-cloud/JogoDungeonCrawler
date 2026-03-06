using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image icon;
    public Button button;

    [Header("Slot Info")]
    [Tooltip("Deixe como 'None' se este for um slot da mochila. Mude apenas para os slots do corpo.")]
    public EquipSlot equipSlot = EquipSlot.None; 
    
    private ItemData currentItem;
    private InventoryUI parentUI;

    public void Setup(ItemData item, InventoryUI ui)
    {
        currentItem = item;
        parentUI = ui;

        if (currentItem != null)
        {
            icon.sprite = currentItem.icon;
            icon.enabled = true; // Mostra a imagem do item
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false; // Esconde a imagem se estiver vazio
        }

        // Garante que o botão tem apenas um evento de clique
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (parentUI != null)
        {
            parentUI.SlotClicked(this, currentItem);
        }
    }
}