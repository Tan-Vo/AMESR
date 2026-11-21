using UnityEngine;

public sealed class ShopSystem : MonoBehaviour
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_UpgradeData[] availableUpgrades;
    [SerializeField] private SO_IntEvent onCreditsChanged;
    [SerializeField] private SO_IntEvent onUpgradeChanged;
    /// <summary>Returns the exponential price for the next upgrade level.</summary>
    public int GetUpgradeCost(SO_UpgradeData upgrade)
    {
        if (upgrade == null || session == null) return int.MaxValue;
        session.Data.UpgradeLevels.TryGetValue(upgrade.name, out int level);
        return UpgradeCostCurve.Calculate(upgrade.BaseCost, upgrade.CostMultiplier, level);
    }
    /// <summary>Purchases an offered upgrade atomically and publishes its category.</summary>
    public bool PurchaseUpgrade(SO_UpgradeData upgrade)
    {
        if (session == null || upgrade == null || System.Array.IndexOf(availableUpgrades, upgrade) < 0) return false;
        session.Data.UpgradeLevels.TryGetValue(upgrade.name, out int level);
        int cost = GetUpgradeCost(upgrade);
        if (level >= upgrade.MaxLevel || session.Data.Credits < cost) return false;
        session.Data.Credits -= cost;
        session.Data.UpgradeLevels[upgrade.name] = level + 1;
        onCreditsChanged?.Raise(session.Data.Credits);
        onUpgradeChanged?.Raise((int)upgrade.Category);
        return true;
    }
    /// <summary>Sells owned units of an item without allowing negative inventory.</summary>
    public bool SellItem(SO_ItemData item, int quantity)
    {
        if (session == null || item == null || quantity <= 0) return false;
        long count = 0;
        foreach (var slot in session.Data.Inventory) if (slot.ItemId == item.name) count += slot.Quantity;
        if (count < quantity) return false;
        int remaining = quantity;
        for (int i = session.Data.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = session.Data.Inventory[i];
            if (slot.ItemId != item.name) continue;
            int take = Mathf.Min(remaining, slot.Quantity);
            remaining -= take; slot.Quantity -= take;
            if (slot.Quantity == 0) session.Data.Inventory.RemoveAt(i);
        }
        session.Data.Credits = (int)System.Math.Min(int.MaxValue, session.Data.Credits + (long)Mathf.Max(0, item.BaseSellPrice) * quantity);
        onCreditsChanged?.Raise(session.Data.Credits);
        return true;
    }
}
