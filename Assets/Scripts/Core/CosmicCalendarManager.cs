using System;
using UnityEngine;

namespace CosmicDiver.Core
{
    /// <summary>
    /// Quản lý tiến trình ngày/đêm dài hạn và lịch sự kiện (Calendar Progression System)
    /// </summary>
    public class CosmicCalendarManager : MonoBehaviour
    {
        public static CosmicCalendarManager Instance { get; private set; }

        [Header("Tiến Trình Thời Gian")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private DayPhase currentPhase = DayPhase.MorningDive;

        [Header("Thông Số Sự Kiện Đặc Biệt")]
        [SerializeField] private bool isEMPStormActive = false;
        [SerializeField] private bool isVIPNightActive = false;
        [SerializeField] private string currentEventDescription = "Biển Tinh Vân yên bình.";

        // C# Events thông báo cho các hệ thống khác
        public event Action<int, DayPhase> OnPhaseChanged;
        public event Action<int> OnDayStarted;
        public event Action<string> OnSpecialEventTriggered;

        public int CurrentDay => currentDay;
        public DayPhase CurrentPhase => currentPhase;
        public bool IsEMPStormActive => isEMPStormActive;
        public bool IsVIPNightActive => isVIPNightActive;
        public string CurrentEventDescription => currentEventDescription;

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
            EvaluateDailyEvents();
            OnDayStarted?.Invoke(currentDay);
            OnPhaseChanged?.Invoke(currentDay, currentPhase);
        }

        /// <summary>
        /// Chuyển giao giữa các ca trong ngày (Sáng -> Chiều -> Tối -> Tổng kết)
        /// </summary>
        public void AdvancePhase()
        {
            switch (currentPhase)
            {
                case DayPhase.MorningDive:
                    currentPhase = DayPhase.AfternoonDive;
                    Debug.Log($"[CosmicCalendar] Bắt đầu Ca Chiều - Ngày {currentDay}");
                    break;

                case DayPhase.AfternoonDive:
                    currentPhase = DayPhase.NightCantina;
                    Debug.Log($"[CosmicCalendar] Mở cửa Quán Ăn Không Gian Nebula Cantina - Tối Ngày {currentDay}");
                    break;

                case DayPhase.NightCantina:
                    currentPhase = DayPhase.DaySummary;
                    Debug.Log($"[CosmicCalendar] Đóng cửa quán, tổng kết doanh thu Ngày {currentDay}");
                    break;

                case DayPhase.DaySummary:
                    StartNextDay();
                    return;
            }

            OnPhaseChanged?.Invoke(currentDay, currentPhase);
        }

        /// <summary>
        /// Bắt đầu một ngày mới hoàn toàn
        /// </summary>
        public void StartNextDay()
        {
            currentDay++;
            currentPhase = DayPhase.MorningDive;
            
            EvaluateDailyEvents();

            Debug.Log($"[CosmicCalendar] ☀️ BÌNH MINH NGÀY {currentDay} - Sự kiện: {currentEventDescription}");
            
            OnDayStarted?.Invoke(currentDay);
            OnPhaseChanged?.Invoke(currentDay, currentPhase);
        }

        /// <summary>
        /// Đánh giá các sự kiện định kỳ chu kỳ 2-3 ngày hoặc sự kiện cuối tuần
        /// </summary>
        private void EvaluateDailyEvents()
        {
            isEMPStormActive = false;
            isVIPNightActive = false;
            currentEventDescription = "Biển Tinh Vân yên tĩnh, thuận lợi cho việc lặn.";

            // 1. Kiểm tra sự kiện Cuối Tuần (Mỗi 7 ngày: Ngày 7, 14, 21, 28, 35...)
            if (currentDay % 7 == 0)
            {
                isVIPNightActive = true;
                currentEventDescription = "⭐ ĐẠI TIỆC ẨM THỰC VIP: Thực khách quyền lực từ Hạm Đội sẽ ghé quán tối nay!";
                OnSpecialEventTriggered?.Invoke(currentEventDescription);
                return;
            }

            // 2. Kiểm tra sự kiện Chu kỳ 3 ngày (Bão Bụi Điện Từ: Ngày 3, 6, 9, 12...)
            if (currentDay % 3 == 0)
            {
                isEMPStormActive = true;
                currentEventDescription = "⚡ BÃO ĐIỆN TỪ VŨ TRỤ: Tầm nhìn giảm, quái vật hung hãn hơn, nhưng khoáng thạch hiếm rơi gấp 3!";
                OnSpecialEventTriggered?.Invoke(currentEventDescription);
                return;
            }

            // 3. Sự kiện Ngày 5: Rung chấn đáy biển (Foreshadowing hành tinh sống)
            if (currentDay == 5)
            {
                currentEventDescription = "⚠️ DỊ THƯỜNG RUNG CHẤN: Radar ghi nhận nhịp đập địa chấn từ tầng sâu rãnh vực.";
                OnSpecialEventTriggered?.Invoke(currentEventDescription);
            }
        }
    }
}
