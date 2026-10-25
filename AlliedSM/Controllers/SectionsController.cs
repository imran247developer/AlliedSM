using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SectionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SectionsController(ApplicationDbContext context) => _context = context;

        // GET: Sections?classId=5 — class-wise section list with student counts
        public async Task<IActionResult> Index(int classId)
        {
            var cls = await _context.SchoolClasses.FindAsync(classId);
            if (cls == null) return NotFound();

            ViewBag.ClassName = cls.ClassName;
            ViewBag.SchoolClassId = classId;

            var sections = await _context.Sections
                .Include(s => s.Students)
                .Where(s => s.SchoolClassId == classId)
                .ToListAsync();
            return View(sections);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int schoolClassId, string sectionName)
        {
            _context.Sections.Add(new Section { SchoolClassId = schoolClassId, SectionName = sectionName });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Section add ho gayi.";
            return RedirectToAction(nameof(Index), new { classId = schoolClassId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string sectionName)
        {
            var section = await _context.Sections.FindAsync(id);
            if (section == null) return NotFound();
            section.SectionName = sectionName;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Section update ho gayi.";
            return RedirectToAction(nameof(Index), new { classId = section.SchoolClassId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.Sections.Include(s => s.Students).FirstOrDefaultAsync(s => s.SectionId == id);
            if (section == null) return NotFound();

            if (section.Students != null && section.Students.Any())
            {
                TempData["Error"] = "Is section mein students hain — delete nahi ho sakta.";
                return RedirectToAction(nameof(Index), new { classId = section.SchoolClassId });
            }

            var classId = section.SchoolClassId;
            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Section delete ho gayi.";
            return RedirectToAction(nameof(Index), new { classId });
        }
    }
}
