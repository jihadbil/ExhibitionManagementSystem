namespace ExhibitionManagementSystem.Desktop.Models
{
    public class EngagementModel
    {
        // خصائص الخطة المطلوبة
        public string ExhibitionName { get; set; } = string.Empty;
        public int DayOneSessions { get; set; }
        public int DayTwoSessions { get; set; }
        public int DayThreeSessions { get; set; }
        public double EngagementRate { get; set; }

        // خصائص الواجهة الحالية لمنع حدوث مشاكل في الربط (Binding)
        public string BoothId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int ViewsCount { get; set; }
    }
}
