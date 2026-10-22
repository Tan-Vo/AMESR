using System;
using UnityEngine;

namespace CosmicDiver.Economy
{
    /// <summary>
    /// Hệ thống cân bằng kinh tế và chi phí nâng cấp theo mô hình hàm mũ tỷ lệ vàng (Cosmic Game Engine Standard)
    /// </summary>
    public static class EconomyBalanceSystem
    {
        // Tỷ lệ vàng tăng trưởng chi phí nâng cấp
        public const float GAMMA_STANDARD = 1.618f; // Dành cho trang bị thông thường (Bình oxy, chân vịt, dao)
        public const float GAMMA_CORE_MODULE = 2.15f; // Dành cho Module Lõi (Súng phóng móc plasma, giáp áp suất tầng sâu)

        /// <summary>
        /// Tính toán chi phí nâng cấp cho cấp độ n: C(n) = Round(C_0 * gamma^(n - 1))
        /// </summary>
        /// <param name="baseCost">Chi phí cơ bản ban đầu C_0</param>
        /// <param name="currentLevel">Cấp độ hiện tại (1 <= n <= 10)</param>
        /// <param name="isCoreModule">Có phải module lõi công nghệ cao hay không</param>
        public static int CalculateUpgradeCost(int baseCost, int currentLevel, bool isCoreModule = false)
        {
            if (currentLevel < 1) currentLevel = 1;
            float gamma = isCoreModule ? GAMMA_CORE_MODULE : GAMMA_STANDARD;
            
            double cost = baseCost * Math.Pow(gamma, currentLevel - 1);
            return (int)Math.Round(cost);
        }

        /// <summary>
        /// Tính toán giá bán món ăn tại Nebula Cantina dựa trên độ hiếm của thịt và độ sâu săn bắt
        /// </summary>
        /// <param name="baseDishValue">Giá trị gốc của món</param>
        /// <param name="captureDepth">Độ sâu săn bắt (mét)</param>
        /// <param name="starQuality">Chất lượng bắt (1 sao: bắn chết, 3 sao: bắt sống bằng súng lưới)</param>
        public static int CalculateDishSalePrice(int baseDishValue, float captureDepth, int starQuality)
        {
            float depthBonus = 1f + (captureDepth / 1000f) * 0.5f; // Càng sâu giá càng cao
            float qualityMultiplier = starQuality switch
            {
                3 => 2.0f, // Bắt sống nguyên vẹn: +100% giá
                2 => 1.4f, // Đánh thuốc mê / taser: +40% giá
                _ => 1.0f  // Bắn chết: giá tiêu chuẩn
            };

            return Mathf.RoundToInt(baseDishValue * depthBonus * qualityMultiplier);
        }
    }
}
