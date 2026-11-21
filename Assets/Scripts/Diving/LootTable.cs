using System;
using UnityEngine;

public static class LootTable
{
    /// <summary>Rolls a valid weighted item, returning null for empty or invalid tables.</summary>
    public static SO_ItemData Roll(LootDrop[] table)
    {
        return TryRoll(table, out var drop) ? drop.Item : null;
    }
    /// <summary>Rolls a complete loot entry, ignoring null items and nonpositive weights or amounts.</summary>
    public static bool TryRoll(LootDrop[] table, out LootDrop drop)
    {
        drop = default;
        if (table == null || table.Length == 0) return false;
        Span<int> weights = table.Length <= 256 ? stackalloc int[table.Length] : new int[table.Length];
        for (int i = 0; i < table.Length; i++)
            weights[i] = table[i].Item != null && table[i].MaxAmount > 0 ? table[i].Weight : 0;
        int index = WeightedSelection.SelectIndex(weights, Math.Min(UnityEngine.Random.value, 0.99999994f));
        if (index < 0) return false;
        drop = table[index];
        return true;
    }
}
