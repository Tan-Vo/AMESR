using System;

namespace CosmicDiver.Core
{
    /// <summary>
    /// Các giai đoạn trong 1 ngày (Chu kỳ Ngày/Đêm tương tự Dave the Diver)
    /// </summary>
    public enum DayPhase
    {
        MorningDive = 0,    // Ca lặn Buổi Sáng (Tầng nông, thu thập cơ bản)
        AfternoonDive = 1,  // Ca lặn Buổi Chiều (Tầng sâu, nhiệm vụ cốt truyện)
        NightCantina = 2,   // Buổi Tối: Quản lý Quầy Ăn Không Gian Nebula Cantina
        DaySummary = 3      // Tổng kết doanh thu, số sao uy tín & chuẩn bị ngày mới
    }

    /// <summary>
    /// Các phân tầng độ sâu của Biển Tinh Vân Vũ Trụ
    /// </summary>
    public enum DepthZone
    {
        NebulaShallows = 0, // 0m - 100m: Tầng Bọt Biển Tinh Vân (An toàn, giàu sinh vật nhỏ)
        CryoTrench = 1,     // 100m - 500m: Vực Băng Tinh Thể (Tiêu hao oxy nhanh, quái tinh anh)
        MethaneAbyss = 2,   // 500m - 1500m: Biển Methane Tối Vĩnh Cửu (Cần đèn pha áp suất)
        PrecursorRift = 3,  // 1500m - 2500m: Vết Nứt Cổ Đại Precursor (Trọng lực dị thường)
        BioVesselCore = 4   // 2500m - 3000m+: Lòng Thực Thể Gaea-Prime (Tâm thất hành tinh sống)
    }

    /// <summary>
    /// Phân loại các nguồn nhiệm vụ
    /// </summary>
    public enum QuestCategory
    {
        MainStory,    // Nhiệm vụ cốt truyện chính từ Hạm Đội & Tiến Sĩ Sarah
        CrewRequest,  // Nhiệm vụ ủy thác từ phi hành đoàn (Bolt-88, Gordo)
        CulinaryOrder,// Đơn đặt món ăn đặc biệt của thực khách
        VesperBounty  // Hợp đồng săn quái đột biến từ Madam Vesper
    }

    /// <summary>
    /// Trạng thái nhiệm vụ
    /// </summary>
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }

    /// <summary>
    /// Các trạng thái hành động của Thợ Lặn Kai
    /// </summary>
    public enum DiverActionState
    {
        Idle,           // Trôi bồng bềnh zero-g
        Swimming,       // Đang đạp chân bơi
        Boosting,       // Kích hoạt phản lực gia tốc (đốt oxy x2.2)
        AimingHarpoon,  // Giương súng móc ngắm bắn
        FiringHarpoon,  // Đang phóng móc kéo mồi
        OxygenPanic,    // Hấp hối khi oxy < 15%
        Hurt,           // Bị va đập hoặc quái cắn
        Dead            // Hết oxy / tử trận
    }
}
