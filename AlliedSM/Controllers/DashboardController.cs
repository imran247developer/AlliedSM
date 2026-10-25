using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction(nameof(Admin));
            if (User.IsInRole("Teacher")) return RedirectToAction(nameof(Teacher));
            if (User.IsInRole("Accountant")) return RedirectToAction(nameof(Accountant));
            if (User.IsInRole("Parent")) return RedirectToAction(nameof(Parent));
            return RedirectToAction(nameof(Admin));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var today = DateTime.Today;

            // NOTE: FeeChallan.TotalPayable is a computed ([NotMapped]) property, so any SUM
            // over it must happen in memory (ToListAsync() first, then .Sum()) — EF Core cannot
            // translate a computed C# property into SQL.

            var vm = new AdminDashboardViewModel
            {
                TotalEnrolment = await _context.Students.CountAsync(s => s.IsActive),
                PendingExpenses = await _context.Expenses.CountAsync(e => !e.IsApproved)
            };

            var todayPaidChallans = await _context.FeeChallans
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate.HasValue && f.PaidDate.Value.Date == today)
                .ToListAsync();
            vm.TodayFeeCollection = todayPaidChallans.Sum(f => f.TotalPayable);

            vm.TodayExpense = await _context.Expenses
                .Where(e => e.IsApproved && e.ExpenseDate.Date == today)
                .SumAsync(e => e.Amount);

            var totalAttendanceToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today);
            var presentToday = await _context.Attendances.CountAsync(a => a.AttendanceDate.Date == today && a.Status == AttendanceStatus.Present);
            vm.TodayAttendancePercent = totalAttendanceToday > 0 ? Math.Round(presentToday * 100.0 / totalAttendanceToday, 1) : 0;

            var weekStart = today.AddDays(-6);
            var weekChallans = await _context.FeeChallans
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate.HasValue
                            && f.PaidDate.Value.Date >= weekStart && f.PaidDate.Value.Date <= today)
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var amount = weekChallans.Where(f => f.PaidDate!.Value.Date == day).Sum(f => f.TotalPayable);
                vm.Last7DaysFee.Add(new DailyTotal { Date = day, Amount = amount });
            }

            var thirtyDaysAgo = today.AddDays(-30);
            var studentsWithAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate >= thirtyDaysAgo)
                .Include(a => a.Student)
                .ToListAsync();

            var attendanceRisk = studentsWithAttendance
                .Where(a => a.Student != null)
                .GroupBy(a => a.Student!)
                .Select(g => new { Student = g.Key, Pct = g.Count(a => a.Status == AttendanceStatus.Present) * 100.0 / g.Count() })
                .Where(x => x.Pct < 75)
                .Take(5);

            foreach (var r in attendanceRisk)
                vm.RiskAlerts.Add($"Low Attendance — {r.Student.StudentName} ({r.Student.SchoolClass?.ClassName}): {r.Pct:0}%");

            var weakMarks = await _context.Marks.Include(m => m.Student).Include(m => m.Exam)
                .Where(m => m.Exam!.TotalMarks > 0 && (m.ObtainedMarks * 100 / m.Exam.TotalMarks) < 40)
                .Take(5).ToListAsync();
            foreach (var m in weakMarks)
                vm.RiskAlerts.Add($"Weak Marks — {m.Student?.StudentName}: {m.Exam?.Subject?.SubjectName} {(m.ObtainedMarks * 100 / m.Exam!.TotalMarks):0}%");

            var overdueChallans = await _context.FeeChallans.Include(f => f.Student)
                .Where(f => f.Status != ChallanStatus.Paid && f.DueDate < today).Take(5).ToListAsync();
            foreach (var c in overdueChallans)
                vm.RiskAlerts.Add($"Fee Overdue — {c.Student?.StudentName}: {(today - c.DueDate).Days} din se pending");

            var classes = await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync();
            var allPaidChallans = await _context.FeeChallans.Where(f => f.Status == ChallanStatus.Paid).Include(f => f.Student).ToListAsync();
            var allUnpaidChallans = await _context.FeeChallans.Where(f => f.Status != ChallanStatus.Paid && f.DueDate < today).Include(f => f.Student).ToListAsync();

            foreach (var cls in classes)
            {
                var students = await _context.Students.CountAsync(s => s.SchoolClassId == cls.SchoolClassId && s.IsActive);
                if (students == 0) continue;

                var classAttendanceToday = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == today && a.Student!.SchoolClassId == cls.SchoolClassId).ToListAsync();
                var classPct = classAttendanceToday.Count > 0
                    ? Math.Round(classAttendanceToday.Count(a => a.Status == AttendanceStatus.Present) * 100.0 / classAttendanceToday.Count, 1) : 0;

                var classFee = allPaidChallans.Where(f => f.Student?.SchoolClassId == cls.SchoolClassId).Sum(f => f.TotalPayable);
                var classDefaulters = allUnpaidChallans.Count(f => f.Student?.SchoolClassId == cls.SchoolClassId);

                vm.ClassSnapshots.Add(new ClassSnapshotRow
                {
                    ClassName = cls.ClassName,
                    Students = students,
                    AttendancePercent = classPct,
                    FeeCollected = classFee,
                    Defaulters = classDefaulters
                });
            }

            return View(vm);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Teacher()
        {
            var user = await _userManager.GetUserAsync(User);
            var vm = new TeacherDashboardViewModel { TeacherName = user?.FullName ?? "Teacher" };

            if (user?.LinkedStaffId != null)
            {
                var myClassSubjects = await _context.ClassSubjects
                    .Include(cs => cs.SchoolClass).Include(cs => cs.Subject)
                    .Where(cs => cs.TeacherId == user.LinkedStaffId).ToListAsync();
                vm.MyClasses = myClassSubjects.Select(cs => $"{cs.SchoolClass?.ClassName} — {cs.Subject?.SubjectName}").ToList();
            }

            vm.AttendanceMarkedToday = await _context.Attendances.AnyAsync(a => a.AttendanceDate.Date == DateTime.Today);
            return View(vm);
        }

        [Authorize(Roles = "Accountant")]
        public async Task<IActionResult> Accountant()
        {
            var today = DateTime.Today;

            var todayPaidChallans = await _context.FeeChallans
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate.HasValue && f.PaidDate.Value.Date == today)
                .ToListAsync();
            var todayIncome = todayPaidChallans.Sum(f => f.TotalPayable);

            var todayExpense = await _context.Expenses.Where(e => e.IsApproved && e.ExpenseDate.Date == today).SumAsync(e => e.Amount);

            var allPaidChallans = await _context.FeeChallans.Where(f => f.Status == ChallanStatus.Paid).ToListAsync();
            var totalIncomeEver = allPaidChallans.Sum(f => f.TotalPayable);
            var totalExpenseEver = await _context.Expenses.Where(e => e.IsApproved).SumAsync(e => e.Amount);

            var vm = new AccountantDashboardViewModel
            {
                TodayIncome = todayIncome,
                TodayExpense = todayExpense,
                NetBalance = totalIncomeEver - totalExpenseEver,
                DefaultersCount = await _context.FeeChallans.CountAsync(f => f.Status != ChallanStatus.Paid && f.DueDate < today),
                RecentExpenses = await _context.Expenses.OrderByDescending(e => e.ExpenseDate).Take(5).ToListAsync()
            };

            return View(vm);
        }

        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> Parent()
        {
            var user = await _userManager.GetUserAsync(User);
            var vm = new ParentDashboardViewModel();

            if (user?.LinkedStudentId != null)
            {
                vm.Child = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                    .FirstOrDefaultAsync(s => s.StudentId == user.LinkedStudentId);

                if (vm.Child != null)
                {
                    var last30 = await _context.Attendances
                        .Where(a => a.StudentId == vm.Child.StudentId && a.AttendanceDate >= DateTime.Today.AddDays(-30))
                        .ToListAsync();
                    vm.AttendancePercent = last30.Count > 0
                        ? Math.Round(last30.Count(a => a.Status == AttendanceStatus.Present) * 100.0 / last30.Count, 1) : 0;

                    var latestChallan = await _context.FeeChallans
                        .Where(f => f.StudentId == vm.Child.StudentId)
                        .OrderByDescending(f => f.IssueDate).FirstOrDefaultAsync();
                    if (latestChallan != null)
                    {
                        vm.FeeStatus = latestChallan.Status.ToString();
                        vm.FeeDueAmount = latestChallan.Status == ChallanStatus.Paid ? 0 : latestChallan.TotalPayable;
                    }

                    vm.RecentMarks = await _context.Marks.Include(m => m.Exam).ThenInclude(e => e!.Subject)
                        .Where(m => m.StudentId == vm.Child.StudentId)
                        .OrderByDescending(m => m.ExamId).Take(5).ToListAsync();
                }
            }

            return View(vm);
        }
    }
}
