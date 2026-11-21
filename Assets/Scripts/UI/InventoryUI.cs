using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryUI : MonoBehaviour
{
    [SerializeField] private SO_InventoryAccess inventory;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject context;
    [SerializeField] private Text tooltip;
    [SerializeField] private Image dragIcon;
    [SerializeField] private InventorySlotUI[] slots;
    [SerializeField] private bool allowSelling;
    private InventorySlotData selected, dragging;
    private void Update() { if (Input.GetKeyDown(KeyCode.I)) { panel.SetActive(!panel.activeSelf); context.SetActive(false); } }
    private void LateUpdate() { if (panel.activeSelf) foreach (var slot in slots) slot.Refresh(inventory); }
    /// <summary>Shows item information or clears the tooltip.</summary>
    public void Inspect(int index)
    {
        var item = inventory.Item(index);
        tooltip.text = item == null ? "" : item.ItemName + "\n" + item.Description + "\nSell: ₡" + item.BaseSellPrice + " • " + item.Rarity;
    }
    /// <summary>Opens actions for one occupied slot.</summary>
    public void Context(int index) { selected = inventory.Slot(index); context.SetActive(inventory.Item(index) != null); Inspect(index); }
    /// <summary>Consumes the selected usable item.</summary>
    public void Use() { if (!inventory.Use(inventory.IndexOf(selected))) tooltip.text = "This item cannot be used here."; context.SetActive(false); }
    /// <summary>Sells one selected item in safe scenes.</summary>
    public void Sell() { if (!allowSelling || !inventory.Sell(inventory.IndexOf(selected))) tooltip.text = "Sell items at the mothership or cantina."; context.SetActive(false); }
    /// <summary>Inspects the selected item.</summary>
    public void Info() { Inspect(inventory.IndexOf(selected)); context.SetActive(false); }
    /// <summary>Begins a bounded inventory drag.</summary>
    public void BeginDrag(int index, Vector2 position) { dragging = inventory.Slot(index); dragIcon.sprite = inventory.Item(index)?.Icon; dragIcon.enabled = dragIcon.sprite != null; Drag(position); }
    /// <summary>Positions the non-raycasting ghost in screen space.</summary>
    public void Drag(Vector2 position) { dragIcon.transform.position = position; }
    /// <summary>Commits a valid drop or cancels it.</summary>
    public void EndDrag(int destination) { if (destination >= 0) inventory.Move(inventory.IndexOf(dragging), destination); dragging = null; dragIcon.enabled = false; }
}
