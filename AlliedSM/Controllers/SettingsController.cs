using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SettingsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var settings = await GetOrCreateAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SchoolSetting model)
        {
            if (!ModelState.IsValid) return View(model);

            var settings = await GetOrCreateAsync();
            settings.BankName = model.BankName;
            settings.AccountNumber = model.AccountNumber;
            settings.JazzCashNumber = model.JazzCashNumber;
            settings.AccountsWhatsApp = model.AccountsWhatsApp;
            settings.PrincipalMobile = model.PrincipalMobile;
            settings.DayEndReportTime = model.DayEndReportTime;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings save ho gayin.";
            return RedirectToAction(nameof(Index));
        }

        // FIX: no longer filters/sets by a hard-coded Id — just takes whichever
        // single row exists (there's only ever one), and lets the database
        // assign the identity value itself when the first row is created.
        private async Task<SchoolSetting> GetOrCreateAsync()
        {
            var settings = await _context.SchoolSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SchoolSetting();
                _context.SchoolSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }
    }
}
