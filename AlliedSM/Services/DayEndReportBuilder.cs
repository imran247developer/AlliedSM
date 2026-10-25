using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Services
{
    // Builds the same end-of-day summary text whether it's triggered manually
    // (Accounts/SendDayEndReport button) or automatically (DayEndReportService).
    public static class DayEndReportBuilder
    {
        public static async Task<string> BuildAsync(ApplicationDbContext context, DateTime date)
        {
            var income = await context.FeeChallans
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate.HasValue && f.PaidDate.Value.Date == date.Date)
                .ToListAsync();
            var totalIncome = income.Sum(f => f.TotalPayable);

            var totalExpense = await context.Expenses
                .Where(e => e.IsApproved && e.ExpenseDate.Date == date.Date)
                .SumAsync(e => e.Amount);

            var totalAttendance = await context.Attendances.CountAsync(a => a.AttendanceDate.Date == date.Date);
            var present = await context.Attendances.CountAsync(a => a.AttendanceDate.Date == date.Date && a.Status == AttendanceStatus.Present);
            var absent = await context.Attendances.CountAsync(a => a.AttendanceDate.Date == date.Date && a.Status == AttendanceStatus.Absent);
            var attendancePct = totalAttendance > 0 ? Math.Round(present * 100.0 / totalAttendance, 1) : 0;

            var newAdmissions = await context.Students.CountAsync(s => s.AdmissionDate.Date == date.Date);

            return $"Allied School Sadiqabad — Day-End Report ({date:dd-MMM-yyyy})\n" +
                   $"Fee Collected: Rs {totalIncome}\n" +
                   $"Expenses: Rs {totalExpense}\n" +
                   $"Net: Rs {totalIncome - totalExpense}\n" +
                   $"Attendance: {attendancePct}% ({present} present, {absent} absent)\n" +
                   $"New Admissions Today: {newAdmissions}";
        }
    }
}
