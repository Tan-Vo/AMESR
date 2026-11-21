using UnityEngine;

[CreateAssetMenu(menuName = "Cosmic Horizon/Data/QuestData")]
public class SO_QuestData : ScriptableObject
{
    [SerializeField] private string questName;
    public string QuestName => questName;
    [SerializeField, TextArea] private string description;
    public string Description => description;
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    [SerializeField] private QuestType questType;
    public QuestType QuestType => questType;
    [SerializeField] private QuestObjective[] objectives;
    public QuestObjective[] Objectives => objectives;
    [SerializeField] private SO_ItemData[] rewardItems;
    public SO_ItemData[] RewardItems => rewardItems;
    [SerializeField] private int[] rewardAmounts;
    public int[] RewardAmounts => rewardAmounts;
    [SerializeField] private int creditReward;
    public int CreditReward => creditReward;
    [SerializeField] private SO_QuestData nextQuest;
    public SO_QuestData NextQuest => nextQuest;
}
