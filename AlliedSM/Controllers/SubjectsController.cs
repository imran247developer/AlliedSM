using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SubjectsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync());

        public IActionResult Create() => View(new Subject());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Subjects.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subject add ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subject model)
        {
            if (id != model.SubjectId) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subject update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Subject delete ho gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
