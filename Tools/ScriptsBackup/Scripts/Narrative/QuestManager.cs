using System;
using System.Collections.Generic;
using UnityEngine;
using CosmicDiver.Core;

namespace CosmicDiver.Narrative
{
    [System.Serializable]
    public class ActiveQuestProgress
    {
        public QuestData data;
        public int currentAmount;
        public bool isCompleted;

        public ActiveQuestProgress(QuestData data)
        {
            this.data = data;
            this.currentAmount = 0;
            this.isCompleted = false;
        }
    }

    /// <summary>
    /// Quản lý trạng thái, tiến độ và phần thưởng của mọi nhiệm vụ trong game
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Kho Dữ Liệu Nhiệm Vụ Toàn Game")]
        [SerializeField] private List<QuestData> allDatabaseQuests = new List<QuestData>();

        [Header("Nhiệm Vụ Đang Thực Hiện")]
        [SerializeField] private List<ActiveQuestProgress> activeQuests = new List<ActiveQuestProgress>();
        [SerializeField] private List<string> completedQuestIds = new List<string>();

        // Events
        public event Action<QuestData> OnQuestUnlocked;
        public event Action<ActiveQuestProgress> OnQuestProgressUpdated;
        public event Action<QuestData> OnQuestCompleted;

        public IReadOnlyList<ActiveQuestProgress> ActiveQuests => activeQuests;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (CosmicCalendarManager.Instance != null)
            {
                CosmicCalendarManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
            CheckAndUnlockAvailableQuests(1, DayPhase.MorningDive);
        }

        private void OnDestroy()
        {
            if (CosmicCalendarManager.Instance != null)
            {
                CosmicCalendarManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(int day, DayPhase phase)
        {
            CheckAndUnlockAvailableQuests(day, phase);
        }

        /// <summary>
        /// Quét kho nhiệm vụ để kích hoạt các nhiệm vụ đủ điều kiện ngày/ca
        /// </summary>
        public void CheckAndUnlockAvailableQuests(int currentDay, DayPhase currentPhase)
        {
            foreach (var q in allDatabaseQuests)
            {
                if (q == null) continue;
                if (completedQuestIds.Contains(q.questId)) continue;
                if (activeQuests.Exists(x => x.data.questId == q.questId)) continue;

                // Kiểm tra điều kiện tiền đề
                if (q.prerequisiteQuest != null && !completedQuestIds.Contains(q.prerequisiteQuest.questId))
                {
                    continue;
                }

                // Kiểm tra ngày và ca kích hoạt
                if (currentDay >= q.unlockDay && currentPhase >= q.unlockPhase)
                {
                    UnlockQuest(q);
                }
            }
        }

        private void UnlockQuest(QuestData quest)
        {
            ActiveQuestProgress newProgress = new ActiveQuestProgress(quest);
            activeQuests.Add(newProgress);

            Debug.Log($"[QuestManager] 📜 NHẬN NHIỆM VỤ MỚI: [{quest.title}] - {quest.description}");
            OnQuestUnlocked?.Invoke(quest);

            // Gửi tin nhắn bộ đàm mở màn nhiệm vụ
            if (!string.IsNullOrEmpty(quest.startRadioDialogue) && RadioDialogueManager.Instance != null)
            {
                RadioDialogueManager.Instance.SendRadioMessage(quest.speakerName, null, quest.startRadioDialogue);
            }
        }

        /// <summary>
        /// Báo cáo tiến độ khi người chơi săn bắt được cá hoặc thu thập vật phẩm
        /// </summary>
        public void ReportItemCollected(string itemIdentifier, int amount = 1)
        {
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                var progress = activeQuests[i];
                if (progress.isCompleted) continue;

                if (progress.data.targetIdentifier.Equals(itemIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    progress.currentAmount = Mathf.Min(progress.data.requiredAmount, progress.currentAmount + amount);
                    OnQuestProgressUpdated?.Invoke(progress);
                    Debug.Log($"[QuestManager] Cập nhật tiến độ [{progress.data.title}]: {progress.currentAmount}/{progress.data.requiredAmount}");

                    if (progress.currentAmount >= progress.data.requiredAmount)
                    {
                        CompleteQuest(progress);
                    }
                }
            }
        }

        /// <summary>
        /// Báo cáo tiến độ khi người chơi lặn đạt mốc độ sâu
        /// </summary>
        public void ReportDepthReached(float depth)
        {
            for (int i = activeQuests.Count - 1; i >= 0; i--)
            {
                var progress = activeQuests[i];
                if (progress.isCompleted) continue;

                if (progress.data.requiredDepth > 0f && depth >= progress.data.requiredDepth)
                {
                    CompleteQuest(progress);
                }
            }
        }

        private void CompleteQuest(ActiveQuestProgress progress)
        {
            progress.isCompleted = true;
            completedQuestIds.Add(progress.data.questId);
            activeQuests.Remove(progress);

            Debug.Log($"[QuestManager] 🎉 HOÀN THÀNH NHIỆM VỤ: [{progress.data.title}] - Nhận +{progress.data.rewardCredits} Credits!");
            OnQuestCompleted?.Invoke(progress.data);

            // Gửi tin nhắn bộ đàm chúc mừng
            if (!string.IsNullOrEmpty(progress.data.completeRadioDialogue) && RadioDialogueManager.Instance != null)
            {
                RadioDialogueManager.Instance.SendRadioMessage(progress.data.speakerName, null, progress.data.completeRadioDialogue);
            }
        }
    }
}
