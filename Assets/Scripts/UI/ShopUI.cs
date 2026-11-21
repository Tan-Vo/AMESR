using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopSystem shop;
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_UpgradeData[] upgrades;
    [SerializeField] private Text[] cards;
    [SerializeField] private Button[] buttons;
    /// <summary>Purchases a selected card through the shop transaction endpoint.</summary>
    public void Purchase(int index) { if (index >= 0 && index < upgrades.Length) shop.PurchaseUpgrade(upgrades[index]); }
    private void LateUpdate()
    {
        for (int i = 0; i < upgrades.Length; i++)
        {
            var upgrade = upgrades[i]; session.Data.UpgradeLevels.TryGetValue(upgrade.name, out int level);
            bool max = level >= upgrade.MaxLevel; int price = shop.GetUpgradeCost(upgrade);
            float bonus = upgrade.BonusPerLevel != null && level < upgrade.BonusPerLevel.Length ? upgrade.BonusPerLevel[level] : 0f;
            cards[i].text = upgrade.UpgradeName + "\nLevel " + level + "/" + upgrade.MaxLevel + "\n" + (max ? "MAX" : "₡" + price + "   +" + bonus + " next level");
            buttons[i].interactable = !max && session.Data.Credits >= price;
        }
    }
}
