namespace AlliedSM.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalEnrolment { get; set; }
        public decimal TodayFeeCollection { get; set; }
        public decimal TodayExpense { get; set; }
        public double TodayAttendancePercent { get; set; }
        public int PendingExpenses { get; set; }
        public List<DailyTotal> Last7DaysFee { get; set; } = new();
        public List<string> RiskAlerts { get; set; } = new();
        public List<ClassSnapshotRow> ClassSnapshots { get; set; } = new();
    }

    public class ClassSnapshotRow
    {
        public string ClassName { get; set; } = string.Empty;
        public int Students { get; set; }
        public double AttendancePercent { get; set; }
        public decimal FeeCollected { get; set; }
        public int Defaulters { get; set; }
    }

    public class TeacherDashboardViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public List<string> MyClasses { get; set; } = new();
        public int PendingMarksExams { get; set; }
        public bool AttendanceMarkedToday { get; set; }
    }

    public class AccountantDashboardViewModel
    {
        public decimal TodayIncome { get; set; }
        public decimal TodayExpense { get; set; }
        public decimal NetBalance { get; set; }
        public int DefaultersCount { get; set; }
        public List<Expense> RecentExpenses { get; set; } = new();
    }

    public class ParentDashboardViewModel
    {
        public Student? Child { get; set; }
        public double AttendancePercent { get; set; }
        public string FeeStatus { get; set; } = "—";
        public decimal FeeDueAmount { get; set; }
        public List<Mark> RecentMarks { get; set; } = new();
    }
}
