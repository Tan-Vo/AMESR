#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using CosmicDiver.Core;
using CosmicDiver.Narrative;

namespace CosmicDiver.EditorTools
{
    public static class CosmicGameSetupMenu
    {
        [MenuItem("Cosmic Diver/Create Day 1-7 Starter Quests", false, 10)]
        public static void CreateStarterQuests()
        {
            string folderPath = "Assets/Data/Quests";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // --- NGÀY 1: CA LẶN THỨC TỈNH ---
            var q1 = ScriptableObject.CreateInstance<QuestData>();
            q1.questId = "MAIN_DAY01_FIRST_DIVE";
            q1.title = "Ca Lặn Thức Tỉnh";
            q1.category = QuestCategory.MainStory;
            q1.description = "Lặn xuống tầng nông của Biển Tinh Vân (0-50m), săn bắt 3 con Sứa Huỳnh Quang mang về cho Bếp trưởng Gordo.";
            q1.unlockDay = 1;
            q1.unlockPhase = DayPhase.MorningDive;
            q1.targetIdentifier = "Neon_Jellyfish";
            q1.requiredAmount = 3;
            q1.rewardCredits = 150;
            q1.rewardUnlockId = "Recipe_Jellyfish_Soup";
            q1.speakerName = "Chef Gordo";
            q1.startRadioDialogue = "Này nhóc Kai! Cả hạm đội đang đói meo rồi đấy. Lặn xuống vớt cho ta vài con sứa béo ngậy lên đây nhanh lên!";
            q1.completeRadioDialogue = "Tuyệt hảo! Món Súp Sứa Huỳnh Quang đã sẵn sàng cho thực đơn tối nay rồi!";
            AssetDatabase.CreateAsset(q1, $"{folderPath}/Quest_Day01_FirstDive.asset");

            // --- NGÀY 2: THU THẬP HỢP KIM TITAN ---
            var q2 = ScriptableObject.CreateInstance<QuestData>();
            q2.questId = "CREW_DAY02_TITANIUM_SALVAGE";
            q2.title = "Gia Cố Bình Dưỡng Khí";
            q2.category = QuestCategory.CrewRequest;
            q2.description = "Thu thập 5 thỏi Hợp Kim Titan từ mảnh vỡ vệ tinh cũ để Robot Bolt-88 độ bình oxy Mark-II.";
            q2.unlockDay = 2;
            q2.unlockPhase = DayPhase.MorningDive;
            q2.prerequisiteQuest = q1;
            q2.targetIdentifier = "Titanium_Ore";
            q2.requiredAmount = 5;
            q2.rewardCredits = 250;
            q2.rewardUnlockId = "Upgrade_Oxygen_Tank_Mark2";
            q2.speakerName = "Bolt-88";
            q2.startRadioDialogue = "Bíp bíp... Bình oxy cũ của cậu rỉ sét 42% rồi. Kiếm quặng Titan về đây, tôi sẽ hàn cho cậu chiếc bình chịu áp suất 100 mét.";
            q2.completeRadioDialogue = "Hoàn tất gia cố! Bình dưỡng khí Mark-II đã sẵn sàng. Giờ cậu có thể lặn sâu hơn gấp đôi!";
            AssetDatabase.CreateAsset(q2, $"{folderPath}/Quest_Day02_TitaniumSalvage.asset");

            // --- NGÀY 3: HỢP ĐỒNG SĂN THƯỞNG VESPER ---
            var q3 = ScriptableObject.CreateInstance<QuestData>();
            q3.questId = "BOUNTY_DAY03_VOLT_EEL";
            q3.title = "Hải Xà Xung Điện Đột Biến";
            q3.category = QuestCategory.VesperBounty;
            q3.description = "Bắt hoặc tiêu diệt Hải Xà Xung Điện đang phục kích gần rạn san hô để nhận tiền thưởng lớn từ Madam Vesper.";
            q3.unlockDay = 3;
            q3.unlockPhase = DayPhase.AfternoonDive;
            q3.targetIdentifier = "Volt_Eel";
            q3.requiredAmount = 1;
            q3.rewardCredits = 400;
            q3.rewardUnlockId = "Blueprint_Taser_Harpoon";
            q3.speakerName = "Madam Vesper";
            q3.startRadioDialogue = "Chào thợ lặn cưng. Con lươn điện đó làm hỏng 2 chuyến tàu buôn của tôi rồi. Xử lý nó, tôi sẽ thưởng hậu hĩnh.";
            q3.completeRadioDialogue = "Làm việc dứt khoát lắm! Đây là bản thiết kế Mũi Móc Phóng Điện như đã hứa.";
            AssetDatabase.CreateAsset(q3, $"{folderPath}/Quest_Day03_VoltEelBounty.asset");

            // --- NGÀY 5: RÃNH VỰC RUNG CHẤN ---
            var q5 = ScriptableObject.CreateInstance<QuestData>();
            q5.questId = "MAIN_DAY05_SEISMIC_ANOMALY";
            q5.title = "Tiếng Đập Từ Vực Sâu";
            q5.category = QuestCategory.MainStory;
            q5.description = "Lặn sâu xuống 120m để cắm cọc tiêu địa chấn điều tra rung chấn kỳ lạ theo lệnh của Tiến sĩ Sarah.";
            q5.unlockDay = 5;
            q5.unlockPhase = DayPhase.MorningDive;
            q5.requiredDepth = 120f;
            q5.rewardCredits = 500;
            q5.rewardUnlockId = "Biome_CryoTrench_Access";
            q5.speakerName = "Dr. Sarah Vance";
            q5.startRadioDialogue = "Kai, cảm biến vừa ghi nhận dao động tần số thấp cực mạnh từ đáy vực. Hãy lặn xuống 120m và cắm cọc tiêu giúp tôi ngay!";
            q5.completeRadioDialogue = "Kỳ lạ thật... Nhịp dao động này có tính tuần hoàn sinh học, như thể... một buồng tim khổng lồ đang co bóp vậy?!";
            AssetDatabase.CreateAsset(q5, $"{folderPath}/Quest_Day05_SeismicAnomaly.asset");

            // --- NGÀY 7: ĐẠI TIỆC VIP HẠM ĐỘI ---
            var q7 = ScriptableObject.CreateInstance<QuestData>();
            q7.questId = "VIP_DAY07_FLEET_INSPECTOR";
            q7.title = "Đại Tiệc VIP: Thanh Tra Hạm Đội";
            q7.category = QuestCategory.CulinaryOrder;
            q7.description = "Phục vụ đại tiệc tối tại Nebula Cantina đạt mốc 600 Credits để vượt qua bài kiểm tra sinh tồn của Hạm Đội.";
            q7.unlockDay = 7;
            q7.unlockPhase = DayPhase.NightCantina;
            q7.requiredAmount = 600;
            q7.rewardCredits = 800;
            q7.rewardUnlockId = "License_Deep_Explorer";
            q7.speakerName = "Chef Gordo";
            q7.startRadioDialogue = "Tối nay Thanh Tra Hạm Đội đích thân đến thử món. Chuẩn bị những lát thịt cá ngon nhất, không được làm hỏng danh tiếng của quán!";
            q7.completeRadioDialogue = "Ha ha ha! Lão thanh tra vét sạch cả đĩa! Hạm Đội đã cấp phép cho chúng ta tiếp cận Vực Băng Tinh Thể rồi!";
            AssetDatabase.CreateAsset(q7, $"{folderPath}/Quest_Day07_FleetInspector.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Cosmic Diver", "Đã khởi tạo thành công bộ 5 Quest mẫu (Ngày 1 đến Ngày 7) tại Assets/Data/Quests!", "Tuyệt vời");
        }
    }
}
#endif
