using UnityEngine;
using CosmicDiver.Core;

namespace CosmicDiver.Narrative
{
    [CreateAssetMenu(fileName = "NewQuestData", menuName = "Cosmic Diver/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Thông Tin Cơ Bản")]
        public string questId;
        public string title;
        public QuestCategory category;
        [TextArea(2, 4)]
        public string description;

        [Header("Điều Kiện Kích Hoạt")]
        public int unlockDay = 1;
        public DayPhase unlockPhase = DayPhase.MorningDive;
        public QuestData prerequisiteQuest;

        [Header("Mục Tiêu Nhiệm Vụ")]
        public string targetIdentifier; // Tên cá, tên quặng hoặc mã khu vực
        public int requiredAmount = 1;
        public float requiredDepth = 0f; // Nếu nhiệm vụ yêu cầu đạt độ sâu nhất định

        [Header("Phần Thưởng Hoàn Thành")]
        public int rewardCredits = 100;
        public string rewardUnlockId; // Mở khóa súng mới, món ăn mới, hoặc biome mới

        [Header("Hội Thoại Bộ Đàm")]
        public string speakerName = "Dr. Sarah Vance";
        [TextArea(2, 3)]
        public string startRadioDialogue;
        [TextArea(2, 3)]
        public string completeRadioDialogue;
    }
}
