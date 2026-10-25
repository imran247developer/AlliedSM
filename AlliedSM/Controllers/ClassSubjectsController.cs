using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassSubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ClassSubjectsController(ApplicationDbContext context) => _context = context;

        // GET: ClassSubjects?classId=5 — class-wise subject allocation with teacher assigned
        public async Task<IActionResult> Index(int? classId)
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);

            var query = _context.ClassSubjects
                .Include(cs => cs.SchoolClass)
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(cs => cs.SchoolClassId == classId);

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Create(int? classId)
        {
            await PopulateDropdowns(classId);
            return View(new ClassSubject { SchoolClassId = classId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassSubject model)
        {
            var alreadyExists = await _context.ClassSubjects.AnyAsync(cs =>
                cs.SchoolClassId == model.SchoolClassId && cs.SubjectId == model.SubjectId);

            if (alreadyExists)
            {
                ModelState.AddModelError(string.Empty, "Yeh subject is class ko pehle se allot ho chuka hai.");
                await PopulateDropdowns(model.SchoolClassId);
                return View(model);
            }

            _context.ClassSubjects.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subject class ko allot ho gaya.";
            return RedirectToAction(nameof(Index), new { classId = model.SchoolClassId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cs = await _context.ClassSubjects.FindAsync(id);
            if (cs == null) return NotFound();
            await PopulateDropdowns(cs.SchoolClassId);
            return View(cs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassSubject model)
        {
            if (id != model.ClassSubjectId) return NotFound();
            var existing = await _context.ClassSubjects.FindAsync(id);
            if (existing == null) return NotFound();

            existing.SubjectId = model.SubjectId;
            existing.TeacherId = model.TeacherId;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Allocation update ho gayi.";
            return RedirectToAction(nameof(Index), new { classId = existing.SchoolClassId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cs = await _context.ClassSubjects.FindAsync(id);
            if (cs == null) return NotFound();
            var classId = cs.SchoolClassId;
            _context.ClassSubjects.Remove(cs);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Allocation delete ho gayi.";
            return RedirectToAction(nameof(Index), new { classId });
        }

        private async Task PopulateDropdowns(int? classId)
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);
            ViewBag.Subjects = new SelectList(await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync(), "SubjectId", "SubjectName");
            ViewBag.Teachers = new SelectList(await _context.Staff.Where(s => s.Designation == "Teacher" && s.IsActive).ToListAsync(), "StaffId", "FullName");
        }
    }
}
