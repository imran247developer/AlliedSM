using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ExamsController(ApplicationDbContext context) => _context = context;

        // GET: Exams — class-wise filter, matches the "date sheet & exam setup" requirement
        public async Task<IActionResult> Index(int? classId)
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);

            var query = _context.Exams.Include(e => e.SchoolClass).Include(e => e.Subject).AsQueryable();
            if (classId.HasValue) query = query.Where(e => e.SchoolClassId == classId);

            return View(await query.OrderByDescending(e => e.ExamId).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new Exam { TotalMarks = 100 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }
            _context.Exams.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam set ho gaya. Ab Marks Entry se obtained marks daal sakte hain.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            await PopulateDropdowns();
            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam model)
        {
            if (id != model.ExamId) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                var marks = _context.Marks.Where(m => m.ExamId == id);
                _context.Marks.RemoveRange(marks);
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Exam delete ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.SchoolClassId = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName");
            ViewBag.SubjectId = new SelectList(await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync(), "SubjectId", "SubjectName");
        }
    }
}
