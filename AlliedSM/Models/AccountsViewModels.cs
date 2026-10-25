namespace AlliedSM.Models
{
    public class AccountsDashboardViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpense;

        public decimal TodayIncome { get; set; }
        public decimal TodayExpense { get; set; }

        public List<DailyTotal> DailyIncome { get; set; } = new();
        public List<DailyTotal> DailyExpense { get; set; } = new();

        public List<FeeChallan> RecentIncome { get; set; } = new();
        public List<Expense> RecentExpenses { get; set; } = new();
    }

    public class DailyTotal
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
