using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ClassesController(ApplicationDbContext context) => _context = context;

        // GET: Classes — dashboard-style report: sections count, students count per class
        public async Task<IActionResult> Index()
        {
            var classes = await _context.SchoolClasses
                .Include(c => c.Sections)
                .Include(c => c.Students)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            return View(classes);
        }

        public IActionResult Create() => View(new SchoolClass());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SchoolClass model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.SchoolClasses.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Class add ho gayi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cls = await _context.SchoolClasses.FindAsync(id);
            if (cls == null) return NotFound();
            return View(cls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SchoolClass model)
        {
            if (id != model.SchoolClassId) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Class update ho gayi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cls = await _context.SchoolClasses.Include(c => c.Students).FirstOrDefaultAsync(c => c.SchoolClassId == id);
            if (cls == null) return NotFound();

            if (cls.Students != null && cls.Students.Any())
            {
                TempData["Error"] = "Is class mein students maujood hain — pehle unko transfer/promote karein.";
                return RedirectToAction(nameof(Index));
            }

            _context.SchoolClasses.Remove(cls);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Class delete ho gayi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
