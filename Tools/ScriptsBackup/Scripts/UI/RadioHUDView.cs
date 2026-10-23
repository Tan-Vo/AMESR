using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CosmicDiver.Narrative;

namespace CosmicDiver.UI
{
    /// <summary>
    /// Hiển thị Banner Bộ Đàm Hologram ở góc trên màn hình khi có cuộc gọi radio
    /// </summary>
    public class RadioHUDView : MonoBehaviour
    {
        [Header("Giao Diện Hologram")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text messageContentText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject activeIndicatorGlow;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false; // Không cản trở thao tác chuột/ngắm bắn của người chơi
            }
        }

        private void OnEnable()
        {
            if (RadioDialogueManager.Instance != null)
            {
                RadioDialogueManager.Instance.OnMessageStarted += ShowRadioMessage;
                RadioDialogueManager.Instance.OnMessageEnded += HideRadioMessage;
            }
        }

        private void OnDisable()
        {
            if (RadioDialogueManager.Instance != null)
            {
                RadioDialogueManager.Instance.OnMessageStarted -= ShowRadioMessage;
                RadioDialogueManager.Instance.OnMessageEnded -= HideRadioMessage;
            }
        }

        private void ShowRadioMessage(RadioMessage msg)
        {
            if (speakerNameText != null) speakerNameText.text = $"[COMMS] {msg.speakerName.ToUpper()}";
            if (messageContentText != null) messageContentText.text = msg.messageText;

            if (portraitImage != null)
            {
                if (msg.speakerPortrait != null)
                {
                    portraitImage.sprite = msg.speakerPortrait;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }

            if (activeIndicatorGlow != null) activeIndicatorGlow.SetActive(true);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(1f, 0.2f));
        }

        private void HideRadioMessage()
        {
            if (activeIndicatorGlow != null) activeIndicatorGlow.SetActive(false);

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCanvasGroup(0f, 0.3f));
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
