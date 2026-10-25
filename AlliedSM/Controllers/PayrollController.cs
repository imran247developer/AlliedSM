using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PayrollController(ApplicationDbContext context) => _context = context;

        // GET: Payroll — list slips, filter by month/year
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var m = month ?? DateTime.Today.Month;
            var y = year ?? DateTime.Today.Year;

            ViewBag.Month = m;
            ViewBag.Year = y;

            var slips = await _context.SalarySlips
                .Include(s => s.Staff)
                .Where(s => s.Month == m && s.Year == y)
                .OrderBy(s => s.Staff!.FullName)
                .ToListAsync();

            return View(slips);
        }

        // GET: Payroll/Generate
        public IActionResult Generate() => View(new GenerateSalaryViewModel());

        // POST: Payroll/Generate — creates a slip for every active staff member, for the chosen month,
        // pre-filled with their base salary (allowances/fines/deductions edited individually afterwards).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GenerateSalaryViewModel vm)
        {
            var staffList = await _context.Staff.Where(s => s.IsActive).ToListAsync();
            int created = 0;

            foreach (var staff in staffList)
            {
                var exists = await _context.SalarySlips.AnyAsync(s =>
                    s.StaffId == staff.StaffId && s.Month == vm.Month && s.Year == vm.Year);
                if (exists) continue;

                _context.SalarySlips.Add(new SalarySlip
                {
                    StaffId = staff.StaffId,
                    Month = vm.Month,
                    Year = vm.Year,
                    BaseSalary = staff.BaseSalary,
                    Allowances = 0,
                    Fines = 0,
                    Deductions = 0,
                    IsPaid = false
                });
                created++;
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{created} salary slips generate ho gaye ({vm.Month}/{vm.Year}). Allowances/fines edit kar ke finalize karein.";
            return RedirectToAction(nameof(Index), new { month = vm.Month, year = vm.Year });
        }

        // GET: Payroll/Edit/5 — adjust allowances, fines, deductions before paying
        public async Task<IActionResult> Edit(int id)
        {
            var slip = await _context.SalarySlips.Include(s => s.Staff).FirstOrDefaultAsync(s => s.SalarySlipId == id);
            if (slip == null) return NotFound();
            return View(slip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal allowances, decimal fines, decimal deductions)
        {
            var slip = await _context.SalarySlips.FindAsync(id);
            if (slip == null) return NotFound();

            slip.Allowances = allowances;
            slip.Fines = fines;
            slip.Deductions = deductions;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Salary slip update ho gayi.";
            return RedirectToAction(nameof(Index), new { month = slip.Month, year = slip.Year });
        }

        // POST: Payroll/MarkPaid/5 — this becomes an Expense (Category = Salary) so it reflects in School Accounts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var slip = await _context.SalarySlips.Include(s => s.Staff).FirstOrDefaultAsync(s => s.SalarySlipId == id);
            if (slip == null) return NotFound();

            slip.IsPaid = true;
            slip.PaidDate = DateTime.Now;

            _context.Expenses.Add(new Expense
            {
                ExpenseDate = DateTime.Now,
                Category = ExpenseCategory.Salary,
                Description = $"Salary — {slip.Staff?.FullName} ({slip.Month}/{slip.Year})",
                Amount = slip.NetPayable,
                IsApproved = true
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Salary paid mark ho gayi aur School Accounts mein expense add ho gaya.";
            return RedirectToAction(nameof(Index), new { month = slip.Month, year = slip.Year });
        }

        // GET: Payroll/Print/5 — printable slip
        public async Task<IActionResult> Print(int id)
        {
            var slip = await _context.SalarySlips.Include(s => s.Staff).FirstOrDefaultAsync(s => s.SalarySlipId == id);
            if (slip == null) return NotFound();
            return View(slip);
        }
    }
}
