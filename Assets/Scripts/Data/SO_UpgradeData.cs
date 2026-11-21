using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/UpgradeData")]
public class SO_UpgradeData : ScriptableObject
{
    [SerializeField] private string upgradeName;
    public string UpgradeName => upgradeName;
    [SerializeField] private string description;
    public string Description => description;
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    [SerializeField] private UpgradeCategory category;
    public UpgradeCategory Category => category;
    [SerializeField] private int maxLevel = 10;
    public int MaxLevel => maxLevel;
    [SerializeField] private int baseCost = 100;
    public int BaseCost => baseCost;
    [SerializeField] private float costMultiplier = 1.618f;
    public float CostMultiplier => costMultiplier;
    [SerializeField] private float[] bonusPerLevel;
    public float[] BonusPerLevel => bonusPerLevel;
}
