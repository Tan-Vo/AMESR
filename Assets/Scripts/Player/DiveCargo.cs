using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class DiveCargo : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_FloatEvent onLootPenalty;
    [SerializeField] private SO_ItemCollectionEvent onItemCollectionRequested;
    private readonly Dictionary<string, int> startingAmounts = new();
    private readonly Dictionary<string, int> currentAmounts = new();
    private readonly Dictionary<string, int> losses = new();
    private PlayerStats stats;
    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        if (session == null) return;
        foreach (var slot in session.Data.Inventory)
        {
            startingAmounts.TryGetValue(slot.ItemId, out int amount);
            startingAmounts[slot.ItemId] = amount + slot.Quantity;
        }
    }
    private void OnEnable() { onLootPenalty?.RegisterListener(ApplyPenalty); onItemCollectionRequested?.RegisterListener(TryAdd); }
    private void OnDisable() { onLootPenalty?.UnregisterListener(ApplyPenalty); onItemCollectionRequested?.UnregisterListener(TryAdd); }
    /// <summary>Stacks a pickup atomically, returning false when the cargo cannot hold the full amount.</summary>
    public bool TryAdd(SO_ItemData item, int quantity)
    {
        if (session == null || item == null || quantity <= 0 || stats.IsDead()) return false;
        var inventory = session.Data.Inventory;
        int stack = Mathf.Max(1, item.MaxStack);
        long room = (long)Mathf.Max(0, stats.InventoryCapacity - inventory.Count) * stack;
        foreach (var slot in inventory) if (slot.ItemId == item.name) room += Mathf.Max(0, stack - slot.Quantity);
        if (room < quantity) return false;
        foreach (var slot in inventory)
        {
            if (slot.ItemId != item.name) continue;
            int add = Mathf.Min(quantity, Mathf.Max(0, stack - slot.Quantity));
            slot.Quantity += add;
            quantity -= add;
        }
        while (quantity > 0)
        {
            int add = Mathf.Min(quantity, stack);
            inventory.Add(new InventorySlotData { ItemId = item.name, Quantity = add });
            quantity -= add;
        }
        return true;
    }
    private void ApplyPenalty(float fraction)
    {
        if (session == null) return;
        losses.Clear();
        currentAmounts.Clear();
        foreach (var slot in session.Data.Inventory)
        {
            currentAmounts.TryGetValue(slot.ItemId, out int amount);
            currentAmounts[slot.ItemId] = amount + slot.Quantity;
        }
        foreach (var pair in currentAmounts)
        {
            startingAmounts.TryGetValue(pair.Key, out int original);
            losses[pair.Key] = Mathf.CeilToInt(Mathf.Max(0, pair.Value - original) * Mathf.Clamp01(fraction));
        }
        for (int i = session.Data.Inventory.Count - 1; i >= 0; i--)
        {
            var slot = session.Data.Inventory[i];
            int remove = Mathf.Min(losses[slot.ItemId], slot.Quantity);
            slot.Quantity -= remove;
            losses[slot.ItemId] -= remove;
            if (slot.Quantity == 0) session.Data.Inventory.RemoveAt(i);
        }
    }
}
