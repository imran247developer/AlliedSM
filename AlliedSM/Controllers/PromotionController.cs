using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PromotionController(ApplicationDbContext context) => _context = context;

        // GET: Promotion — pick a source class/section, see the student checklist
        public async Task<IActionResult> Index(int? fromClassId, int? fromSectionId)
        {
            var vm = new PromotionViewModel { FromClassId = fromClassId, FromSectionId = fromSectionId };

            await PopulateDropdowns(fromClassId);

            if (fromClassId.HasValue)
            {
                var query = _context.Students.Where(s => s.IsActive && s.SchoolClassId == fromClassId);
                if (fromSectionId.HasValue) query = query.Where(s => s.SectionId == fromSectionId);

                var students = await query.OrderBy(s => s.StudentName).ToListAsync();
                vm.Students = students.Select(s => new PromotionRow
                {
                    StudentId = s.StudentId,
                    RegistrationNo = s.RegistrationNo,
                    StudentName = s.StudentName
                }).ToList();
            }

            return View(vm);
        }

        // POST: Promotion — bulk-moves every checked student to the chosen target class/section.
        // Same action covers promotion (higher class), demotion (lower class), or a plain section transfer.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(PromotionViewModel vm)
        {
            var selectedIds = vm.Students.Where(s => s.Selected).Select(s => s.StudentId).ToList();
            if (!selectedIds.Any())
            {
                TempData["Error"] = "Koi student select nahi kiya gaya.";
                return RedirectToAction(nameof(Index), new { fromClassId = vm.FromClassId, fromSectionId = vm.FromSectionId });
            }

            var students = await _context.Students.Where(s => selectedIds.Contains(s.StudentId)).ToListAsync();
            foreach (var s in students)
            {
                s.SchoolClassId = vm.ToClassId;
                if (vm.ToSectionId.HasValue) s.SectionId = vm.ToSectionId.Value;
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{students.Count} students naye class/section mein transfer ho gaye.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Promotion/MarkLeft — bulk "student left school" (soft-delete, keeps history)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkLeft(PromotionViewModel vm)
        {
            var selectedIds = vm.Students.Where(s => s.Selected).Select(s => s.StudentId).ToList();
            var students = await _context.Students.Where(s => selectedIds.Contains(s.StudentId)).ToListAsync();
            foreach (var s in students) s.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{students.Count} students ko 'left school' mark kar diya gaya (record mehfooz hai).";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(int? fromClassId)
        {
            var classes = await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.FromClasses = new SelectList(classes, "SchoolClassId", "ClassName", fromClassId);
            ViewBag.ToClasses = new SelectList(classes, "SchoolClassId", "ClassName");
            ViewBag.Sections = new SelectList(await _context.Sections.ToListAsync(), "SectionId", "SectionName");
        }
    }
}
