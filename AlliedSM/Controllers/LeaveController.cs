using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Leave — Admin sees everyone's requests (with Approve/Reject), Teacher/Accountant see only their own
        public async Task<IActionResult> Index()
        {
            var query = _context.LeaveRequests.Include(l => l.Staff).AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.LinkedStaffId == null) return View(new List<LeaveRequest>());
                query = query.Where(l => l.StaffId == user.LinkedStaffId);
            }

            return View(await query.OrderByDescending(l => l.AppliedDate).ToListAsync());
        }

        // GET: Leave/Apply — Teacher/Accountant/Admin-staff apply for themselves
        public IActionResult Apply() => View(new LeaveRequest { FromDate = DateTime.Today, ToDate = DateTime.Today });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(LeaveRequest model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.LinkedStaffId == null)
            {
                ModelState.AddModelError(string.Empty, "Aapka login kisi Staff profile se link nahi hai — Admin se rabta karein.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            model.StaffId = user.LinkedStaffId.Value;
            model.Status = LeaveStatus.Pending;
            model.AppliedDate = DateTime.Now;

            _context.LeaveRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave application submit ho gayi — Admin ke approval ka intezar karein.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Leave/Approve/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? remarks)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave == null) return NotFound();
            leave.Status = LeaveStatus.Approved;
            leave.AdminRemarks = remarks;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Leave approve ho gayi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Leave/Reject/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? remarks)
        {
            var leave = await _context.LeaveRequests.FindAsync(id);
            if (leave == null) return NotFound();
            leave.Status = LeaveStatus.Rejected;
            leave.AdminRemarks = remarks;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Leave reject kar di gayi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
