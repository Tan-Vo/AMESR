using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InventorySlotData
{
    [SerializeField] private string itemId;
    [SerializeField] private int quantity;
    public string ItemId { get => itemId; set => itemId = value; }
    public int Quantity { get => quantity; set => quantity = value; }
}

[Serializable]
public sealed class QuestProgressData
{
    [SerializeField] private string key;
    [SerializeField] private int amount;
    public string Key { get => key; set => key = value; }
    public int Amount { get => amount; set => amount = value; }
}

[Serializable]
public sealed class PendingRewardData
{
    [SerializeField] private string rewardId, itemId;
    [SerializeField] private int quantity;
    public string RewardId { get => rewardId; set => rewardId = value; }
    public string ItemId { get => itemId; set => itemId = value; }
    public int Quantity { get => quantity; set => quantity = value; }
}

[Serializable]
public sealed class SaveData : ISerializationCallbackReceiver
{
    [SerializeField] private int version = 1;
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int credits = 500;
    [SerializeField] private int reputationStars = 1;
    [SerializeField] private int reputationPoints;
    [SerializeField] private List<InventorySlotData> inventory = new();
    [SerializeField] private List<string> unlockedRecipeIds = new();
    [SerializeField] private List<string> completedQuestIds = new();
    [SerializeField] private List<string> defeatedBossIds = new();
    [SerializeField] private List<PendingRewardData> pendingRewards = new();
    [SerializeField] private List<QuestProgressData> questProgress = new();
    [SerializeField] private int tutorialStep;
    [SerializeField] private float totalPlayTime;
    [SerializeField] private List<string> upgradeKeys = new();
    [SerializeField] private List<int> upgradeValues = new();
    private Dictionary<string, int> upgradeLevels = new();

    public int CurrentDay { get => currentDay; set => currentDay = value; }
    public int Credits { get => credits; set => credits = value; }
    public int ReputationStars { get => reputationStars; set => reputationStars = value; }
    public int ReputationPoints { get => reputationPoints; set => reputationPoints = value; }
    public List<InventorySlotData> Inventory => inventory;
    public List<string> UnlockedRecipeIds => unlockedRecipeIds;
    public List<string> CompletedQuestIds => completedQuestIds;
    public List<string> DefeatedBossIds => defeatedBossIds;
    public List<PendingRewardData> PendingRewards => pendingRewards ??= new();
    public List<QuestProgressData> QuestProgress => questProgress ??= new();
    public int TutorialStep { get => tutorialStep; set => tutorialStep = value; }
    public Dictionary<string, int> UpgradeLevels => upgradeLevels;
    public float TotalPlayTime { get => totalPlayTime; set => totalPlayTime = value; }

    /// <summary>Serializes dictionary entries into Unity-compatible lists.</summary>
    public void OnBeforeSerialize()
    {
        upgradeKeys.Clear();
        upgradeValues.Clear();
        foreach (var pair in upgradeLevels) { upgradeKeys.Add(pair.Key); upgradeValues.Add(pair.Value); }
    }
    /// <summary>Reconstructs the upgrade dictionary after JSON deserialization.</summary>
    public void OnAfterDeserialize()
    {
        upgradeLevels = new Dictionary<string, int>();
        if (upgradeKeys == null || upgradeValues == null) return;
        for (int i = 0; i < Math.Min(upgradeKeys.Count, upgradeValues.Count); i++)
            if (!string.IsNullOrEmpty(upgradeKeys[i])) upgradeLevels[upgradeKeys[i]] = upgradeValues[i];
    }
    /// <summary>Checks save integrity before allowing it to replace the active session.</summary>
    public bool IsValid()
    {
        if (version != 1 || currentDay < 1 || credits < 0 || reputationPoints < 0 || reputationStars < 1 || reputationStars > 4 ||
            float.IsNaN(totalPlayTime) || float.IsInfinity(totalPlayTime) || totalPlayTime < 0 ||
            inventory == null || unlockedRecipeIds == null || completedQuestIds == null || defeatedBossIds == null ||
            upgradeKeys == null || upgradeValues == null || upgradeKeys.Count != upgradeValues.Count) return false;
        if (tutorialStep < 0 || tutorialStep > 10) return false;
        if (pendingRewards != null)
        {
            var ids = new HashSet<string>();
            foreach (var reward in pendingRewards)
                if (reward == null || string.IsNullOrEmpty(reward.RewardId) || string.IsNullOrEmpty(reward.ItemId) || reward.Quantity <= 0 || !ids.Add(reward.RewardId)) return false;
        }
        if (questProgress != null) foreach (var entry in questProgress)
            if (entry == null || string.IsNullOrEmpty(entry.Key) || entry.Amount < 0) return false;
        foreach (var slot in inventory)
            if (slot == null || string.IsNullOrEmpty(slot.ItemId) || slot.Quantity <= 0) return false;
        foreach (var pair in upgradeLevels)
            if (string.IsNullOrEmpty(pair.Key) || pair.Value < 0) return false;
        return true;
    }
}
