using System.Globalization;
using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BiometricController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BiometricController(ApplicationDbContext context) => _context = context;

        // School shift rules — adjust to match your actual school timings.
        private static readonly TimeSpan ShiftStart = new(8, 0, 0);
        private static readonly TimeSpan GraceMinutes = TimeSpan.FromMinutes(15);   // late fine starts after 8:15
        private static readonly TimeSpan ShiftEnd = new(15, 0, 0);                   // overtime counted after 3:00 PM
        private const decimal FinePerLateBlock = 20m;                                // Rs 20 per 10 minutes late
        private const int LateBlockMinutes = 10;

        // GET: Biometric — recent imported records, filterable by date range
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-7);
            var to = toDate ?? DateTime.Today;

            var records = await _context.StaffAttendances
                .Include(s => s.Staff)
                .Where(s => s.Date.Date >= from.Date && s.Date.Date <= to.Date)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            return View(records);
        }

        // GET: Biometric/Import
        public IActionResult Import() => View();

        // POST: Biometric/Import
        // Expected CSV columns (no header row needed, but one is skipped if present):
        //   CNIC, Date (yyyy-MM-dd), CheckIn (HH:mm), CheckOut (HH:mm)
        // Example row:  35201-1234567-1,2026-09-19,08:22,15:40
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["Error"] = "Koi file select nahi ki gayi.";
                return RedirectToAction(nameof(Import));
            }

            var staffByCnic = await _context.Staff.ToDictionaryAsync(s => s.Cnic.Trim(), s => s);

            int imported = 0, skipped = 0;
            using var reader = new StreamReader(csvFile.OpenReadStream());

            string? line;
            bool firstLine = true;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Skip an obvious header row like "CNIC,Date,CheckIn,CheckOut"
                if (firstLine && line.Contains("CNIC", StringComparison.OrdinalIgnoreCase))
                {
                    firstLine = false;
                    continue;
                }
                firstLine = false;

                var parts = line.Split(',');
                if (parts.Length < 3) { skipped++; continue; }

                var cnic = parts[0].Trim();
                if (!staffByCnic.TryGetValue(cnic, out var staff)) { skipped++; continue; }

                if (!DateTime.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) { skipped++; continue; }

                TimeSpan? checkIn = TimeSpan.TryParse(parts[2].Trim(), out var ciParsed) ? ciParsed : null;
                TimeSpan? checkOut = parts.Length > 3 && TimeSpan.TryParse(parts[3].Trim(), out var coParsed) ? coParsed : null;

                var existing = await _context.StaffAttendances
                    .FirstOrDefaultAsync(s => s.StaffId == staff.StaffId && s.Date.Date == date.Date);
                if (existing == null)
                {
                    existing = new StaffAttendance { StaffId = staff.StaffId, Date = date.Date };
                    _context.StaffAttendances.Add(existing);
                }

                existing.CheckIn = checkIn;
                existing.CheckOut = checkOut;

                // Late fine calculation
                if (checkIn.HasValue && checkIn.Value > ShiftStart + GraceMinutes)
                {
                    var lateMinutes = (int)(checkIn.Value - ShiftStart).TotalMinutes;
                    existing.LateMinutes = lateMinutes;
                    var lateBlocks = (int)Math.Ceiling(lateMinutes / (double)LateBlockMinutes);
                    existing.LateFine = lateBlocks * FinePerLateBlock;
                }
                else
                {
                    existing.LateMinutes = 0;
                    existing.LateFine = 0;
                }

                // Overtime calculation
                if (checkOut.HasValue && checkOut.Value > ShiftEnd)
                    existing.OvertimeMinutes = (int)(checkOut.Value - ShiftEnd).TotalMinutes;
                else
                    existing.OvertimeMinutes = 0;

                imported++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{imported} records import ho gaye, {skipped} rows skip hui (CNIC match nahi hua ya format ghalat tha).";
            return RedirectToAction(nameof(Index));
        }

        // POST: Biometric/ApplyFinesToPayroll/5 — pushes accumulated late fines into that staff's
        // current month salary slip (Module 7) so payroll reflects biometric data automatically.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyFinesToPayroll(int staffId, int month, int year)
        {
            var totalFine = await _context.StaffAttendances
                .Where(s => s.StaffId == staffId && s.Date.Month == month && s.Date.Year == year)
                .SumAsync(s => s.LateFine);

            var slip = await _context.SalarySlips
                .FirstOrDefaultAsync(s => s.StaffId == staffId && s.Month == month && s.Year == year);

            if (slip == null)
            {
                TempData["Error"] = "Is mahine ki salary slip pehle generate karein (Payroll module).";
                return RedirectToAction(nameof(Index));
            }

            slip.Fines = totalFine;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Rs {totalFine} late-fine payroll mein add ho gaya.";
            return RedirectToAction(nameof(Index));
        }
    }
}
