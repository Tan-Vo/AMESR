using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private InventoryUI owner;
    [SerializeField] private int index;
    [SerializeField] private Image icon;
    [SerializeField] private Image border;
    [SerializeField] private Text label;
    private static readonly Color[] Rarities = { Color.white, Color.green, Color.cyan, new Color(0.7f, 0.3f, 1f), new Color(1f, 0.8f, 0.1f) };
    /// <summary>Updates an occupied or empty slot.</summary>
    public void Refresh(SO_InventoryAccess inventory)
    {
        var item = inventory.Item(index); icon.enabled = item != null; icon.sprite = item != null ? item.Icon : null;
        label.text = item != null ? inventory.Quantity(index).ToString() : "";
        border.color = item != null ? Rarities[Mathf.Clamp((int)item.Rarity, 0, Rarities.Length - 1)] : new Color(0.2f, 0.3f, 0.4f);
    }
    /// <summary>Begins a drag using a shared ghost icon.</summary>
    public void OnBeginDrag(PointerEventData data) { owner.BeginDrag(index, data.position); }
    /// <summary>Moves the shared ghost.</summary>
    public void OnDrag(PointerEventData data) { owner.Drag(data.position); }
    /// <summary>Completes a drag only on another slot in this inventory.</summary>
    public void OnEndDrag(PointerEventData data)
    {
        var target = data.pointerEnter != null ? data.pointerEnter.GetComponentInParent<InventorySlotUI>() : null;
        owner.EndDrag(target != null && target.owner == owner ? target.index : -1);
    }
    /// <summary>Shows item details.</summary>
    public void OnPointerEnter(PointerEventData data) { owner.Inspect(index); }
    /// <summary>Clears hover details.</summary>
    public void OnPointerExit(PointerEventData data) { owner.Inspect(-1); }
    /// <summary>Opens the contextual Use/Sell/Info actions.</summary>
    public void OnPointerClick(PointerEventData data) { if (data.button == PointerEventData.InputButton.Right) owner.Context(index); }
}
