using UnityEngine;
using UnityEngine.UI;
using CosmicDiver.Diving;

namespace CosmicDiver.UI
{
    /// <summary>
    /// Hiển thị thanh Dưỡng Khí & Cảnh Báo Nguy Hiểm trên HUD Thợ Lặn
    /// </summary>
    public class OxygenHUDView : MonoBehaviour
    {
        [Header("Thanh Dưỡng Khí")]
        [SerializeField] private Image oxygenFillBar;
        [SerializeField] private Text oxygenText;
        [SerializeField] private Gradient oxygenColorGradient;

        [Header("Cảnh Báo Thiếu Oxy")]
        [SerializeField] private GameObject criticalWarningPanel;
        [SerializeField] private float flashSpeed = 4f;

        private OxygenSystem targetOxygenSystem;

        public void Bind(OxygenSystem system)
        {
            if (targetOxygenSystem != null)
            {
                targetOxygenSystem.OnOxygenChanged -= HandleOxygenChanged;
            }

            targetOxygenSystem = system;

            if (targetOxygenSystem != null)
            {
                targetOxygenSystem.OnOxygenChanged += HandleOxygenChanged;
                HandleOxygenChanged(targetOxygenSystem.CurrentOxygen, targetOxygenSystem.MaxOxygen);
            }
        }

        private void OnDestroy()
        {
            if (targetOxygenSystem != null)
            {
                targetOxygenSystem.OnOxygenChanged -= HandleOxygenChanged;
            }
        }

        private void Update()
        {
            // Nhấp nháy panel cảnh báo khi lượng oxy rơi vào mức nguy kịch
            if (targetOxygenSystem != null && targetOxygenSystem.IsPanicking)
            {
                if (criticalWarningPanel != null)
                {
                    criticalWarningPanel.SetActive(true);
                    CanvasGroup cg = criticalWarningPanel.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
                    }
                }
            }
            else
            {
                if (criticalWarningPanel != null && criticalWarningPanel.activeSelf)
                {
                    criticalWarningPanel.SetActive(false);
                }
            }
        }

        private void HandleOxygenChanged(float current, float max)
        {
            float ratio = Mathf.Clamp01(current / max);

            if (oxygenFillBar != null)
            {
                oxygenFillBar.fillAmount = ratio;
                if (oxygenColorGradient != null)
                {
                    oxygenFillBar.color = oxygenColorGradient.Evaluate(ratio);
                }
            }

            if (oxygenText != null)
            {
                oxygenText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)} O₂";
            }
        }
    }
}
