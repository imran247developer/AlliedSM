using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StaffController(ApplicationDbContext context) => _context = context;

        // GET: Staff — filter by designation, search by name/CNIC
        public async Task<IActionResult> Index(string? designation, string? search)
        {
            var query = _context.Staff.Where(s => s.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(designation))
                query = query.Where(s => s.Designation == designation);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.FullName.Contains(search) || s.Cnic.Contains(search));

            return View(await query.OrderBy(s => s.FullName).ToListAsync());
        }

        public IActionResult Create() => View(new Staff());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Staff model)
        {
            if (!ModelState.IsValid) return View(model);
            model.IsActive = true;
            _context.Staff.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{model.FullName} ka profile add ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Staff model)
        {
            if (id != model.StaffId) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Staff profile update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        // Soft-delete: keeps salary-slip history intact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
                staff.IsActive = false;
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Staff record remove ho gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
