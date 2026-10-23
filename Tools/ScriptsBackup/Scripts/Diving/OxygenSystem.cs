using System;
using UnityEngine;

namespace CosmicDiver.Diving
{
    /// <summary>
    /// Hệ thống Dưỡng Khí & Áp Suất Không Gian (Mô hình phi tuyến tính theo Cosmic Game Engine Skill)
    /// </summary>
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Thông Số Bình Dưỡng Khí")]
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float currentOxygen = 100f;

        [Header("Hệ Số Toán Học Tiêu Hao")]
        [Tooltip("Tốc độ tiêu hao cơ bản (units/giây)")]
        [SerializeField] private float baseDrainRate = 0.45f;

        [Tooltip("Hệ số áp suất hư không đè nặng theo độ sâu (alpha)")]
        [SerializeField] private float depthFactorAlpha = 0.75f;

        [Tooltip("Hệ số đốt oxy khi kích hoạt phản lực đẩy Thruster (mu_boost)")]
        [SerializeField] private float boostMultiplier = 2.2f;

        [Tooltip("Độ sâu tối đa của bản đồ (mét)")]
        [SerializeField] private float maxDepth = 3000f;

        [Header("Cảnh Báo Nguy Hiểm")]
        [SerializeField] private float panicThresholdPercent = 0.15f; // Dưới 15% sẽ kích hoạt cảnh báo nguy kịch
        private bool isPanicking = false;

        // Events
        public event Action<float, float> OnOxygenChanged; // (current, max)
        public event Action OnOxygenLowWarning;
        public event Action OnOxygenDepleted; // Hết oxy, kích hoạt khoang cứu hộ khẩn cấp

        public float CurrentOxygen => currentOxygen;
        public float MaxOxygen => maxOxygen;
        public float OxygenPercent => currentOxygen / maxOxygen;
        public bool IsPanicking => isPanicking;

        private void Start()
        {
            currentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }

        /// <summary>
        /// Tiêu hao oxy trong từng khung hình dựa trên độ sâu và trạng thái phản lực
        /// </summary>
        /// <param name="currentDepth">Độ sâu hiện tại tính bằng mét</param>
        /// <param name="isBoosting">Có đang bật phản lực tăng tốc hay không</param>
        public void ConsumeOxygen(float currentDepth, bool isBoosting)
        {
            if (currentOxygen <= 0f) return;

            // Đảm bảo độ sâu không âm
            float d = Mathf.Clamp(currentDepth, 0f, maxDepth);
            float b = isBoosting ? 1f : 0f;

            // Công thức toán học: R_drain = (R_base + alpha * (d / d_max)^1.5) * (1 + mu_boost * B)
            float depthRatio = Mathf.Clamp01(d / maxDepth);
            float depthResistance = depthFactorAlpha * Mathf.Pow(depthRatio, 1.5f);
            float boostFactor = 1f + (boostMultiplier * b);

            float drainRate = (baseDrainRate + depthResistance) * boostFactor;
            float oxygenLoss = drainRate * Time.deltaTime;

            currentOxygen = Mathf.Max(0f, currentOxygen - oxygenLoss);
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);

            // Kiểm tra trạng thái hoảng loạn / thiếu oxy
            if (OxygenPercent <= panicThresholdPercent && !isPanicking)
            {
                isPanicking = true;
                OnOxygenLowWarning?.Invoke();
                Debug.LogWarning("[OxygenSystem] ⚠️ CẢNH BÁO: Dưỡng khí xuống dưới mức an toàn 15%!");
            }
            else if (OxygenPercent > panicThresholdPercent && isPanicking)
            {
                isPanicking = false;
            }

            // Hết oxy hoàn toàn
            if (currentOxygen <= 0f)
            {
                currentOxygen = 0f;
                OnOxygenDepleted?.Invoke();
                Debug.LogError("[OxygenSystem] 💀 HẾT OXY: Tự động kích hoạt kén cấp cứu đưa về Tàu Mẹ!");
            }
        }

        /// <summary>
        /// Nạp thêm dưỡng khí (nhặt bình oxy hoặc chạm vào phao cấp cứu)
        /// </summary>
        public void RefillOxygen(float amount)
        {
            currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
            if (OxygenPercent > panicThresholdPercent) isPanicking = false;
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }

        /// <summary>
        /// Nâng cấp dung tích bình oxy sau khi mua ở xưởng Bolt-88
        /// </summary>
        public void UpgradeMaxOxygen(float newMax)
        {
            maxOxygen = newMax;
            currentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }
    }
}
