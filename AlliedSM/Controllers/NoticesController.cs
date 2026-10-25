using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize]
    public class NoticesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NoticesController(ApplicationDbContext context) => _context = context;

        // GET: Notices — everyone sees notices meant for their audience, newest first
        public async Task<IActionResult> Index()
        {
            var query = _context.Notices.AsQueryable();

            if (User.IsInRole("Parent"))
                query = query.Where(n => n.Audience == NoticeAudience.Everyone || n.Audience == NoticeAudience.ParentsOnly);
            else if (User.IsInRole("Teacher") || User.IsInRole("Accountant"))
                query = query.Where(n => n.Audience == NoticeAudience.Everyone || n.Audience == NoticeAudience.StaffOnly);
            // Admin sees everything

            return View(await query.OrderByDescending(n => n.IsUrgent).ThenByDescending(n => n.PostedDate).ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new Notice());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notice model)
        {
            if (!ModelState.IsValid) return View(model);
            model.PostedDate = DateTime.Now;
            _context.Notices.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Notice board par post ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null) return NotFound();
            return View(notice);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Notice model)
        {
            if (id != model.NoticeId) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Notice update ho gaya.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice != null)
            {
                _context.Notices.Remove(notice);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Notice delete ho gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
