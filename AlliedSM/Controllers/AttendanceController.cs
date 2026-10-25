using AlliedSM.Data;
using AlliedSM.Models;
using AlliedSM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _sms;

        public AttendanceController(ApplicationDbContext context, ISmsService sms)
        {
            _context = context;
            _sms = sms;
        }

        // GET: Attendance/Mark?classId=..&sectionId=..&date=2026-09-19
        // Class-wise filter + loads existing marks for that date if already taken (so it can be edited).
        public async Task<IActionResult> Mark(int? classId, int? sectionId, DateTime? date)
        {
            await PopulateDropdowns(classId, sectionId);

            var vm = new AttendanceMarkViewModel { AttendanceDate = date ?? DateTime.Today };

            if (classId == null || sectionId == null)
                return View(vm); // ask user to pick class/section first

            vm.SchoolClassId = classId.Value;
            vm.SectionId = sectionId.Value;

            var students = await _context.Students
                .Where(s => s.SchoolClassId == classId && s.SectionId == sectionId && s.IsActive)
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            var existing = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == vm.AttendanceDate.Date
                            && students.Select(s => s.StudentId).Contains(a.StudentId))
                .ToListAsync();

            vm.Rows = students.Select(s =>
            {
                var rec = existing.FirstOrDefault(e => e.StudentId == s.StudentId);
                return new AttendanceMarkRow
                {
                    StudentId = s.StudentId,
                    RegistrationNo = s.RegistrationNo,
                    StudentName = s.StudentName,
                    Status = rec?.Status ?? AttendanceStatus.Present,
                    Remarks = rec?.Remarks
                };
            }).ToList();

            return View(vm);
        }

        // POST: Attendance/Mark
        // Saves the whole class register for one date in a single submit,
        // and fires an SMS alert for every student marked Absent.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mark(AttendanceMarkViewModel vm)
        {
            foreach (var row in vm.Rows)
            {
                var existing = await _context.Attendances.FirstOrDefaultAsync(a =>
                    a.StudentId == row.StudentId && a.AttendanceDate.Date == vm.AttendanceDate.Date);

                if (existing == null)
                {
                    existing = new Attendance
                    {
                        StudentId = row.StudentId,
                        AttendanceDate = vm.AttendanceDate.Date
                    };
                    _context.Attendances.Add(existing);
                }

                existing.Status = row.Status;
                existing.Remarks = row.Remarks;

                if (row.Status == AttendanceStatus.Absent && !existing.SmsSent)
                {
                    var student = await _context.Students.FindAsync(row.StudentId);
                    if (student != null)
                    {
                        await _sms.SendAsync(student.ParentMobile,
                            $"Assalam-o-Alaikum, aapka bacha {student.StudentName} ({student.RegistrationNo}) aaj school mein absent hai. — Allied School Sadiqabad");
                        existing.SmsSent = true;
                    }
                }
                else if (row.Status != AttendanceStatus.Absent)
                {
                    existing.SmsSent = false; // reset if status corrected later
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{vm.AttendanceDate:dd-MMM-yyyy} ki attendance save ho gayi. Absentees ko SMS bhej diya gaya.";
            return RedirectToAction(nameof(Mark), new { classId = vm.SchoolClassId, sectionId = vm.SectionId, date = vm.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        // GET: Attendance/History — previous dates ka record dekhne/edit/delete karne ke liye
        public async Task<IActionResult> History(int? classId, int? sectionId, DateTime? fromDate, DateTime? toDate)
        {
            await PopulateDropdowns(classId, sectionId);

            var from = fromDate ?? DateTime.Today.AddDays(-7);
            var to = toDate ?? DateTime.Today;

            var query = _context.Attendances
                .Include(a => a.Student)
                .Where(a => a.AttendanceDate.Date >= from.Date && a.AttendanceDate.Date <= to.Date)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(a => a.Student!.SchoolClassId == classId);
            if (sectionId.HasValue)
                query = query.Where(a => a.Student!.SectionId == sectionId);

            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            return View(await query.OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.Student!.StudentName).ToListAsync());
        }

        // GET: Attendance/Edit/5 — single record correction
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.Attendances.Include(a => a.Student).FirstOrDefaultAsync(a => a.AttendanceId == id);
            if (record == null) return NotFound();
            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AttendanceStatus status, string? remarks)
        {
            var record = await _context.Attendances.FindAsync(id);
            if (record == null) return NotFound();

            record.Status = status;
            record.Remarks = remarks;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance record update ho gaya.";
            return RedirectToAction(nameof(History));
        }

        // POST: Attendance/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.Attendances.FindAsync(id);
            if (record != null)
            {
                _context.Attendances.Remove(record);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Attendance record delete ho gaya.";
            return RedirectToAction(nameof(History));
        }

        private async Task PopulateDropdowns(int? classId, int? sectionId)
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);
            ViewBag.Sections = new SelectList(await _context.Sections.ToListAsync(), "SectionId", "SectionName", sectionId);
        }
    }
}
