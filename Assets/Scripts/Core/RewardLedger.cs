using System;

public static class RewardLedger
{
    /// <summary>Finds an unclaimed reward by its persistent one-time receipt.</summary>
    public static PendingRewardData Find(SaveData data, string id)
    {
        foreach (var reward in data.PendingRewards) if (reward.RewardId == id) return reward;
        return null;
    }
    /// <summary>Stores a victory reward independently of cargo admission and scene lifetime.</summary>
    public static bool Store(SaveData data, string id, string itemId, int quantity)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(itemId) || quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (Find(data, id) != null) return false;
        data.PendingRewards.Add(new PendingRewardData { RewardId = id, ItemId = itemId, Quantity = quantity });
        return true;
    }
    /// <summary>Closes a receipt only after its physical pickup successfully collected.</summary>
    public static void Collected(SaveData data, string id)
    {
        var reward = Find(data, id); if (reward != null) data.PendingRewards.Remove(reward);
    }
    /// <summary>Claims a pending reward atomically into bounded hub inventory.</summary>
    public static bool TryClaim(SaveData data, string id, int maximumStack, int capacity)
    {
        var reward = Find(data, id);
        if (reward == null || !InventoryMath.TryAdd(data.Inventory, reward.ItemId, reward.Quantity, maximumStack, capacity)) return false;
        data.PendingRewards.Remove(reward); return true;
    }
}
