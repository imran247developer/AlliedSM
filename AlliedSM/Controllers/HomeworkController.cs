using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize]
    public class HomeworkController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeworkController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Homework — class-wise filter. Admin/Teacher see everything they can manage;
        // Parent automatically sees only their own child's class (read-only).
        public async Task<IActionResult> Index(int? classId)
        {
            var query = _context.Homeworks
                .Include(h => h.SchoolClass)
                .Include(h => h.Subject)
                .Include(h => h.PostedByStaff)
                .AsQueryable();

            if (User.IsInRole("Parent"))
            {
                var user = await _userManager.GetUserAsync(User);
                var child = user?.LinkedStudentId != null ? await _context.Students.FindAsync(user.LinkedStudentId) : null;
                if (child == null) return View(new List<Homework>());
                query = query.Where(h => h.SchoolClassId == child.SchoolClassId);
                ViewBag.ReadOnly = true;
            }
            else
            {
                if (classId.HasValue) query = query.Where(h => h.SchoolClassId == classId);
                ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);
            }

            return View(await query.OrderByDescending(h => h.AssignedDate).ToListAsync());
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new Homework { AssignedDate = DateTime.Today });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Homework model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            model.PostedByStaffId = user?.LinkedStaffId;

            _context.Homeworks.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Homework upload ho gaya — students/parents ab dekh sakte hain.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id)
        {
            var hw = await _context.Homeworks.FindAsync(id);
            if (hw == null) return NotFound();
            await PopulateDropdowns();
            return View(hw);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Homework model)
        {
            if (id != model.HomeworkId) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Homework update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hw = await _context.Homeworks.FindAsync(id);
            if (hw != null)
            {
                _context.Homeworks.Remove(hw);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Homework delete ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.SchoolClassId = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName");
            ViewBag.SubjectId = new SelectList(await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync(), "SubjectId", "SubjectName");
        }
    }
}
