using System.Collections.Generic;
using UnityEngine;

public sealed class QuestManager : Singleton<QuestManager>
{
    [SerializeField] private SO_SaveSession session;
    [SerializeField] private SO_QuestData[] quests;
    [SerializeField] private SO_QuestProgressEvent onQuestProgressRequested;
    [SerializeField] private SO_StringEvent onQuestFailureRequested;
    [SerializeField] private SO_FloatEvent onDepthChanged;
    [SerializeField] private SO_IntEvent onCreditsChanged;
    [SerializeField] private SO_IntEvent onStateEntered;
    [SerializeField] private SO_GameEvent onSaveLoaded;
    private readonly List<SO_QuestData> active = new(10);
    private bool nightStarted;
    private bool serviceFinished;
    public IReadOnlyList<SO_QuestData> ActiveQuests => active;
    private void OnEnable()
    {
        onQuestProgressRequested?.RegisterListener(UpdateProgress); onDepthChanged?.RegisterListener(Depth);
        onSaveLoaded?.RegisterListener(Synchronize); onStateEntered?.RegisterListener(State); onQuestFailureRequested?.RegisterListener(Failure);
    }
    private void OnDisable()
    {
        onQuestProgressRequested?.UnregisterListener(UpdateProgress); onDepthChanged?.UnregisterListener(Depth);
        onSaveLoaded?.UnregisterListener(Synchronize); onStateEntered?.UnregisterListener(State); onQuestFailureRequested?.UnregisterListener(Failure);
    }
    private void Start() { Synchronize(); }
    private void Synchronize() { active.Clear(); foreach (var quest in quests) AcceptQuest(quest); }
    private void State(int state)
    {
        if ((GameState)state == GameState.MorningBrief) Synchronize();
        if ((GameState)state == GameState.CantinaPrep) { nightStarted = false; serviceFinished = false; }
        if ((GameState)state == GameState.NightShop)
        {
            serviceFinished = true;
            for (int i = active.Count - 1; i >= 0; i--) if (IsQuestComplete(active[i])) CompleteQuest(active[i]);
        }
        if ((GameState)state != GameState.CantinaOpen || nightStarted) return;
        nightStarted = true;
        foreach (var quest in active)
            for (int i = 0; i < quest.Objectives.Length; i++)
                if (quest.Objectives[i].Type == ObjectiveType.ServeCustomers || quest.Objectives[i].Type == ObjectiveType.EarnCredits) Progress(quest, i).Amount = 0;
    }
    private void Failure(string target)
    {
        foreach (var quest in active)
            for (int i = 0; i < quest.Objectives.Length; i++) if (quest.Objectives[i].TargetId == target) Progress(quest, i).Amount = 0;
    }
    private void Depth(float depth) { UpdateProgress(ObjectiveType.ReachDepth, "", Mathf.FloorToInt(depth)); }
    /// <summary>Accepts an available quest once.</summary>
    public void AcceptQuest(SO_QuestData quest)
    { if (quest != null && !active.Contains(quest) && !session.Data.CompletedQuestIds.Contains(quest.name)) active.Add(quest); }
    /// <summary>Updates matching objectives and grants each completed quest exactly once.</summary>
    public void UpdateProgress(ObjectiveType type, string targetId, int amount)
    {
        if (amount < 0) return;
        for (int q = active.Count - 1; q >= 0; q--)
        {
            var quest = active[q];
            for (int i = 0; i < quest.Objectives.Length; i++)
            {
                var objective = quest.Objectives[i];
                if (objective.Type != type || !string.IsNullOrEmpty(objective.TargetId) && objective.TargetId != targetId) continue;
                var progress = Progress(quest, i);
                progress.Amount = QuestProgressMath.Advance(progress.Amount, amount, Mathf.Max(1, objective.RequiredAmount), type == ObjectiveType.ReachDepth || type == ObjectiveType.UpgradeGear);
            }
            if (IsQuestComplete(quest)) CompleteQuest(quest);
        }
    }
    private QuestProgressData Progress(SO_QuestData quest, int index)
    {
        string key = quest.name + ":" + index;
        foreach (var progress in session.Data.QuestProgress) if (progress.Key == key) return progress;
        var entry = new QuestProgressData { Key = key }; session.Data.QuestProgress.Add(entry); return entry;
    }
    /// <summary>Checks every objective, rejecting empty definitions.</summary>
    public bool IsQuestComplete(SO_QuestData quest)
    {
        if (quest == null || quest.Objectives == null || quest.Objectives.Length == 0) return false;
        for (int i = 0; i < quest.Objectives.Length; i++) if (Progress(quest, i).Amount < quest.Objectives[i].RequiredAmount) return false;
        return true;
    }
    /// <summary>Awards credits and optional item rewards without duplicating completion.</summary>
    public void CompleteQuest(SO_QuestData quest)
    {
        if (!active.Contains(quest) || !IsQuestComplete(quest) || session.Data.CompletedQuestIds.Contains(quest.name)) return;
        foreach (var objective in quest.Objectives) if (objective.TargetId == "NoAngry" && !serviceFinished) return;
        var inventory = new List<InventorySlotData>(20);
        foreach (var slot in session.Data.Inventory) inventory.Add(new InventorySlotData { ItemId = slot.ItemId, Quantity = slot.Quantity });
        if (quest.RewardItems != null && quest.RewardAmounts != null)
            for (int i = 0; i < Mathf.Min(quest.RewardItems.Length, quest.RewardAmounts.Length); i++)
            {
                var item = quest.RewardItems[i]; int quantity = quest.RewardAmounts[i];
                if (item == null || quantity <= 0) continue;
                int maximum = Mathf.Max(1, item.MaxStack);
                foreach (var slot in inventory)
                    if (slot.ItemId == item.name) { int add = Mathf.Min(quantity, Mathf.Max(0, maximum - slot.Quantity)); slot.Quantity += add; quantity -= add; }
                while (quantity > 0)
                {
                    if (inventory.Count >= 20) return;
                    int add = Mathf.Min(quantity, maximum); inventory.Add(new InventorySlotData { ItemId = item.name, Quantity = add }); quantity -= add;
                }
            }
        session.Data.CompletedQuestIds.Add(quest.name); active.Remove(quest);
        session.Data.Inventory.Clear(); session.Data.Inventory.AddRange(inventory);
        session.Data.Credits = (int)System.Math.Min(int.MaxValue, session.Data.Credits + (long)Mathf.Max(0, quest.CreditReward));
        onCreditsChanged?.Raise(session.Data.Credits);
        AcceptQuest(quest.NextQuest);
    }
}
