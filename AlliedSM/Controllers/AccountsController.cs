using AlliedSM.Data;
using AlliedSM.Models;
using AlliedSM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _sms;

        public AccountsController(ApplicationDbContext context, ISmsService sms)
        {
            _context = context;
            _sms = sms;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var from = (fromDate ?? DateTime.Today.AddDays(-6)).Date;
            var to = (toDate ?? DateTime.Today).Date;

            var paidChallans = await _context.FeeChallans
                .Include(f => f.Student)
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate != null
                            && f.PaidDate.Value.Date >= from && f.PaidDate.Value.Date <= to)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.IsApproved && e.ExpenseDate.Date >= from && e.ExpenseDate.Date <= to)
                .ToListAsync();

            var vm = new AccountsDashboardViewModel
            {
                FromDate = from,
                ToDate = to,
                TotalIncome = paidChallans.Sum(c => c.TotalPayable),
                TotalExpense = expenses.Sum(e => e.Amount),
                TodayIncome = paidChallans.Where(c => c.PaidDate!.Value.Date == DateTime.Today).Sum(c => c.TotalPayable),
                TodayExpense = expenses.Where(e => e.ExpenseDate.Date == DateTime.Today).Sum(e => e.Amount),
                RecentIncome = paidChallans.OrderByDescending(c => c.PaidDate).Take(10).ToList(),
                RecentExpenses = expenses.OrderByDescending(e => e.ExpenseDate).Take(10).ToList()
            };

            for (var d = from; d <= to; d = d.AddDays(1))
            {
                vm.DailyIncome.Add(new DailyTotal { Date = d, Amount = paidChallans.Where(c => c.PaidDate!.Value.Date == d).Sum(c => c.TotalPayable) });
                vm.DailyExpense.Add(new DailyTotal { Date = d, Amount = expenses.Where(e => e.ExpenseDate.Date == d).Sum(e => e.Amount) });
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDayEndReport()
        {
            var settings = await _context.SchoolSettings.FirstOrDefaultAsync();

            if (settings == null || string.IsNullOrWhiteSpace(settings.PrincipalMobile))
            {
                TempData["Error"] = "Principal ka mobile number set nahi hai — pehle Settings page se add karein.";
                return RedirectToAction(nameof(Index));
            }

            var message = await DayEndReportBuilder.BuildAsync(_context, DateTime.Today);
            await _sms.SendAsync(settings.PrincipalMobile, message);

            TempData["Success"] = "Day-end report Principal ko bhej diya gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
