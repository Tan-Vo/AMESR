using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/ItemData")]
public class SO_ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    public string ItemName => itemName;
    [SerializeField] private string description;
    public string Description => description;
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    [SerializeField] private ItemCategory category;
    public ItemCategory Category => category;
    [SerializeField] private int maxStack = 99;
    public int MaxStack => maxStack;
    [SerializeField] private int baseSellPrice = 10;
    public int BaseSellPrice => baseSellPrice;
    [SerializeField] private ItemRarity rarity;
    public ItemRarity Rarity => rarity;
}
