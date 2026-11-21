using UnityEngine;
using UnityEngine.UI;

public sealed class QuickSlotUI : MonoBehaviour
{
    [SerializeField] private SO_InventoryAccess inventory;
    [SerializeField] private int index;
    [SerializeField] private Image icon;
    [SerializeField] private Text label;
    /// <summary>Uses the indexed consumable, if any.</summary>
    public void Use() { inventory.Use(inventory.ConsumableIndex(index)); }
    private void LateUpdate()
    {
        int slot = inventory.ConsumableIndex(index); var item = inventory.Item(slot);
        icon.enabled = item != null; icon.sprite = item != null ? item.Icon : null;
        label.text = "F" + (index + 1) + (item == null ? "" : " ×" + inventory.Quantity(slot));
    }
}
