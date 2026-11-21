using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/UI/Inventory Access")]
public sealed class SO_InventoryAccess : ScriptableObject
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_ItemData[] catalog;
    [SerializeField] private SO_IntEvent onCreditsChanged;
    [SerializeField] private SO_IntEvent onHealRequested;
    public int Count => session != null ? session.Data.Inventory.Count : 0;
    /// <summary>Resolves the nth consumable without allocating a filtered list.</summary>
    public int ConsumableIndex(int ordinal)
    {
        if (ordinal < 0) return -1;
        for (int i = 0; i < Count; i++) if (Item(i)?.Category == ItemCategory.Consumable && ordinal-- == 0) return i;
        return -1;
    }
    /// <summary>Resolves an owned slot to its item definition.</summary>
    public SO_ItemData Item(int index)
    {
        if (index < 0 || index >= Count) return null;
        string id = session.Data.Inventory[index].ItemId;
        foreach (var item in catalog) if (item != null && item.name == id) return item;
        return null;
    }
    /// <summary>Returns the quantity in an occupied slot.</summary>
    public int Quantity(int index) => index >= 0 && index < Count ? session.Data.Inventory[index].Quantity : 0;
    /// <summary>Merges compatible stacks, swaps occupied slots, or moves an item to the end.</summary>
    public void Move(int source, int destination)
    {
        if (source < 0 || source >= Count || destination < 0 || destination == source) return;
        var slots = session.Data.Inventory;
        if (destination >= Count) { var entry = slots[source]; slots.RemoveAt(source); slots.Add(entry); return; }
        var item = Item(source);
        if (item != null && slots[source].ItemId == slots[destination].ItemId)
        {
            int amount = InventoryMath.Transfer(slots[source].Quantity, slots[destination].Quantity, item.MaxStack);
            slots[source].Quantity -= amount; slots[destination].Quantity += amount;
            if (slots[source].Quantity == 0) slots.RemoveAt(source);
        }
        else { var entry = slots[source]; slots[source] = slots[destination]; slots[destination] = entry; }
    }
    /// <summary>Uses one healing consumable when an active player can receive it.</summary>
    public bool Use(int index)
    {
        var item = Item(index);
        if (item == null || item.Category != ItemCategory.Consumable || onHealRequested == null || !onHealRequested.HasListeners) return false;
        onHealRequested.Raise(25); RemoveOne(index); return true;
    }
    /// <summary>Sells one unit while in a hub or cantina shop, saturating the credit balance.</summary>
    public bool Sell(int index)
    {
        var item = Item(index);
        if (item == null) return false;
        session.Data.Credits = (int)System.Math.Min(int.MaxValue, session.Data.Credits + (long)Mathf.Max(0, item.BaseSellPrice));
        RemoveOne(index); onCreditsChanged?.Raise(session.Data.Credits); return true;
    }
    private void RemoveOne(int index)
    {
        var slot = session.Data.Inventory[index];
        if (--slot.Quantity == 0) session.Data.Inventory.RemoveAt(index);
    }
}
