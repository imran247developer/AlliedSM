using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExpensesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Expenses
        public async Task<IActionResult> Index(string? category, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Expenses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ExpenseCategory>(category, out var cat))
                query = query.Where(e => e.Category == cat);

            if (fromDate.HasValue) query = query.Where(e => e.ExpenseDate.Date >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(e => e.ExpenseDate.Date <= toDate.Value.Date);

            return View(await query.OrderByDescending(e => e.ExpenseDate).ToListAsync());
        }

        // GET: Expenses/Create
        public IActionResult Create() => View(new Expense());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (!ModelState.IsValid) return View(expense);
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Expense add ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.ExpenseId) return NotFound();
            if (!ModelState.IsValid) return View(expense);

            _context.Update(expense);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Expense update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Expenses/Approve/5 — only Admin approves, feeds the dashboard total once approved
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                expense.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Expense approve ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Expenses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Expense delete ho gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
