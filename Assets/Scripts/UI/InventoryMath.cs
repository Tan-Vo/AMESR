using System;
using System.Collections.Generic;

public static class InventoryMath
{
    /// <summary>Resolves the original stack, never a different stack shifted into its old index.</summary>
    public static int Resolve(List<InventorySlotData> slots, InventorySlotData identity) => identity == null || slots == null ? -1 : slots.IndexOf(identity);
    /// <summary>Admits a complete quantity atomically, respecting both stack and slot limits.</summary>
    public static bool TryAdd(List<InventorySlotData> slots, string itemId, int quantity, int maximum, int capacity)
    {
        if (slots == null || string.IsNullOrEmpty(itemId) || quantity <= 0 || maximum <= 0 || capacity <= 0) return false;
        long room = (long)Math.Max(0, capacity - slots.Count) * maximum;
        foreach (var slot in slots) if (slot.ItemId == itemId) room += Math.Max(0, maximum - slot.Quantity);
        if (room < quantity) return false;
        foreach (var slot in slots)
        {
            if (slot.ItemId != itemId) continue;
            int add = Transfer(quantity, slot.Quantity, maximum); slot.Quantity += add; quantity -= add;
        }
        while (quantity > 0)
        {
            int add = Math.Min(quantity, maximum); slots.Add(new InventorySlotData { ItemId = itemId, Quantity = add }); quantity -= add;
        }
        return true;
    }
    /// <summary>Returns how many units fit into an existing stack.</summary>
    public static int Transfer(int source, int destination, int maximum)
    {
        if (source < 0 || destination < 0 || maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
        return Math.Min(source, Math.Max(0, maximum - destination));
    }
}
