using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicDiver.Narrative
{
    [System.Serializable]
    public struct RadioMessage
    {
        public string speakerName;
        public Sprite speakerPortrait;
        [TextArea(2, 4)]
        public string messageText;
        public float duration;

        public RadioMessage(string speaker, Sprite portrait, string text, float duration = 4f)
        {
            this.speakerName = speaker;
            this.speakerPortrait = portrait;
            this.messageText = text;
            this.duration = duration;
        }
    }

    /// <summary>
    /// Hệ thống phát thông điệp Bộ Đàm Không Xâm Lấn (Non-Intrusive Radio HUD)
    /// Thông điệp hiển thị ở góc màn hình khi đang lặn mà KHÔNG tạm dừng game!
    /// </summary>
    public class RadioDialogueManager : MonoBehaviour
    {
        public static RadioDialogueManager Instance { get; private set; }

        private readonly Queue<RadioMessage> messageQueue = new Queue<RadioMessage>();
        private bool isDisplaying = false;

        // Events dành cho UI Canvas đăng ký lắng nghe
        public event Action<RadioMessage> OnMessageStarted;
        public event Action OnMessageEnded;

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

        /// <summary>
        /// Gửi một thông điệp bộ đàm vào hàng đợi
        /// </summary>
        public void SendRadioMessage(string speaker, Sprite portrait, string text, float duration = 4.0f)
        {
            RadioMessage msg = new RadioMessage(speaker, portrait, text, duration);
            messageQueue.Enqueue(msg);

            if (!isDisplaying)
            {
                StartCoroutine(ProcessMessageQueueRoutine());
            }
        }

        private IEnumerator ProcessMessageQueueRoutine()
        {
            isDisplaying = true;

            while (messageQueue.Count > 0)
            {
                RadioMessage currentMsg = messageQueue.Dequeue();
                
                Debug.Log($"[RadioComms] 📡 [{currentMsg.speakerName}]: {currentMsg.messageText}");
                OnMessageStarted?.Invoke(currentMsg);

                yield return new WaitForSeconds(currentMsg.duration);

                OnMessageEnded?.Invoke();
                yield return new WaitForSeconds(0.3f); // Khoảng nghỉ giữa các tin nhắn
            }

            isDisplaying = false;
        }
    }
}
