using AlliedSM.Data;
using AlliedSM.Models;
using AlliedSM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class FeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _sms;

        public FeeController(ApplicationDbContext context, ISmsService sms)
        {
            _context = context;
            _sms = sms;
        }

        public async Task<IActionResult> Index(int? classId, string? status, string? search)
        {
            var query = _context.FeeChallans.Include(f => f.Student).ThenInclude(s => s!.SchoolClass).AsQueryable();

            if (classId.HasValue) query = query.Where(f => f.Student!.SchoolClassId == classId);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ChallanStatus>(status, out var st))
                query = query.Where(f => f.Status == st);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(f => f.VoucherNo.Contains(search) || f.Student!.StudentName.Contains(search));

            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);

            return View(await query.OrderByDescending(f => f.IssueDate).ToListAsync());
        }

        public async Task<IActionResult> Defaulters()
        {
            var list = await _context.FeeChallans
                .Include(f => f.Student).ThenInclude(s => s!.SchoolClass)
                .Where(f => f.Status != ChallanStatus.Paid && f.DueDate < DateTime.Today)
                .OrderBy(f => f.DueDate)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Students = new SelectList(await _context.Students.Where(s => s.IsActive).ToListAsync(), "StudentId", "StudentName");
            return View(new FeeChallan { DueDate = DateTime.Today.AddDays(15) });
        }

        // FIX: a student could previously get 2–3 challans for the same month if
        // the form was submitted more than once (double-click, back button + resubmit,
        // etc). Now blocked: one challan per student per calendar month, checked
        // against FeeChallan.IssueDate (the month this challan was generated for).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeeChallan challan)
        {
            ModelState.Remove(nameof(FeeChallan.VoucherNo));

            var now = DateTime.Now;
            bool alreadyExists = await _context.FeeChallans.AnyAsync(f =>
                f.StudentId == challan.StudentId &&
                f.IssueDate.Year == now.Year &&
                f.IssueDate.Month == now.Month);

            if (alreadyExists)
            {
                ModelState.AddModelError("", $"This student already has a fee challan generated for {now:MMMM yyyy}. " +
                    "Edit the existing challan instead of creating a duplicate one.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Students = new SelectList(await _context.Students.Where(s => s.IsActive).ToListAsync(), "StudentId", "StudentName", challan.StudentId);
                return View(challan);
            }

            challan.VoucherNo = await NextVoucherNo();
            challan.IssueDate = now;
            challan.Status = ChallanStatus.Unpaid;
            _context.FeeChallans.Add(challan);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Challan {challan.VoucherNo} generated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> BulkGenerate()
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName");
            return View(new BulkGenerateViewModel());
        }

        // POST: Fee/BulkGenerate — monthly RECURRING challans.
        // These never include AdmissionFee (that is a one-time charge already
        // billed on the student's very first / admission challan — see
        // StudentsController.GenerateChallanForStudent). Only Tuition + Exam +
        // Lab (+ any carried-over Arrears/Late Fee) repeat every month.
        //
        // FIX: previously running Bulk Generate twice for the same class in the
        // same month (e.g. clicking it twice, or running it again after already
        // generating October's challans) created duplicate October challans for
        // every student in the class. Now, any student who already has a challan
        // for the current calendar month is skipped — so a class's challans for
        // a given month only ever get generated once, no matter how many times
        // the button is pressed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkGenerate(BulkGenerateViewModel vm)
        {
            var studentsQuery = _context.Students.Where(s => s.IsActive && s.SchoolClassId == vm.SchoolClassId);
            if (vm.SectionId.HasValue) studentsQuery = studentsQuery.Where(s => s.SectionId == vm.SectionId);

            var students = await studentsQuery.ToListAsync();
            var now = DateTime.Now;

            // Students in this class who already have a challan for the current
            // month — these are skipped instead of re-billed.
            var studentIdsAlreadyBilled = (await _context.FeeChallans
                    .Where(f => f.IssueDate.Year == now.Year && f.IssueDate.Month == now.Month)
                    .Select(f => f.StudentId)
                    .ToListAsync())
                .ToHashSet();

            var generatedIds = new List<int>();
            var skippedCount = 0;

            foreach (var student in students)
            {
                if (studentIdsAlreadyBilled.Contains(student.StudentId))
                {
                    skippedCount++;
                    continue;
                }

                var unpaidPrevious = await _context.FeeChallans
                    .Where(f => f.StudentId == student.StudentId && f.Status != ChallanStatus.Paid)
                    .ToListAsync();

                var arrears = unpaidPrevious.Sum(f => f.TotalPayable);
                var hasOverdue = unpaidPrevious.Any(f => f.DueDate < DateTime.Today);

                var challan = new FeeChallan
                {
                    VoucherNo = await NextVoucherNo(),
                    StudentId = student.StudentId,
                    TuitionFee = student.TuitionFee,
                    ExamFee = student.ExamFee,
                    LabFee = student.LabFee,
                    ArrearsAmount = arrears,
                    LateFeeFine = hasOverdue ? vm.LateFeeFineIfOverdue : 0,
                    IssueDate = now,
                    DueDate = vm.DueDate,
                    Status = ChallanStatus.Unpaid
                };
                _context.FeeChallans.Add(challan);
                await _context.SaveChangesAsync();
                generatedIds.Add(challan.FeeChallanId);
            }

            if (generatedIds.Count == 0)
            {
                TempData["Success"] = $"No new challans generated — every student in this class already has a {now:MMMM yyyy} challan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = skippedCount > 0
                ? $"{generatedIds.Count} challans generated for {now:MMMM yyyy}. {skippedCount} student(s) skipped — already had a challan this month."
                : $"{generatedIds.Count} challans generated for {now:MMMM yyyy}.";

            return RedirectToAction(nameof(Print), new { ids = string.Join(",", generatedIds) });
        }

        public async Task<IActionResult> Print(string ids)
        {
            var idList = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var challans = await _context.FeeChallans.Include(f => f.Student).ThenInclude(s => s!.SchoolClass)
                .Include(f => f.Student).ThenInclude(s => s!.Section)
                .Where(f => idList.Contains(f.FeeChallanId)).ToListAsync();
            return View(challans);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var challan = await _context.FeeChallans.FindAsync(id);
            if (challan == null) return NotFound();
            ViewBag.Students = new SelectList(await _context.Students.ToListAsync(), "StudentId", "StudentName", challan.StudentId);
            return View(challan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FeeChallan challan)
        {
            if (id != challan.FeeChallanId) return NotFound();
            var existing = await _context.FeeChallans.FindAsync(id);
            if (existing == null) return NotFound();

            existing.TuitionFee = challan.TuitionFee;
            existing.ExamFee = challan.ExamFee;
            existing.LabFee = challan.LabFee;
            existing.TransportFee = challan.TransportFee;
            existing.AdmissionFee = challan.AdmissionFee;
            existing.ArrearsAmount = challan.ArrearsAmount;
            existing.LateFeeFine = challan.LateFeeFine;
            existing.DueDate = challan.DueDate;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Challan updated.";
            return RedirectToAction(nameof(Index));
        }

        // FIX: this action previously silently failed to persist a payment in some
        // cases because the "Mark Paid" modal's HTML was nested incorrectly inside
        // the challans table (see Views/Fee/Index.cshtml for the fix) — the button
        // sometimes opened the WRONG challan's modal, or none at all, so the wrong
        // (or no) POST reached this action. The server-side logic below was already
        // correct; the bug was purely in the view markup.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id, PaymentMethod method, string? reference)
        {
            var challan = await _context.FeeChallans.FindAsync(id);
            if (challan == null) return NotFound();

            challan.Status = ChallanStatus.Paid;
            challan.PaidVia = method;
            challan.PaidDate = DateTime.Now;
            challan.TransactionReference = reference;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Challan {challan.VoucherNo} marked paid — income updated in School Accounts.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var challan = await _context.FeeChallans.FindAsync(id);
            if (challan != null)
            {
                _context.FeeChallans.Remove(challan);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Challan deleted.";
            return RedirectToAction(nameof(Index));
        }

        // NEW: backs both "Delete Selected" (checked rows) and "Delete All" (every
        // challan currently shown) on the Fee & Challans page — same comma-separated
        // "ids" pattern already used by Print/BulkGenerate's redirect.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(string ids)
        {
            var idList = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var challans = await _context.FeeChallans.Where(f => idList.Contains(f.FeeChallanId)).ToListAsync();

            _context.FeeChallans.RemoveRange(challans);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{challans.Count} challan(s) deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> NextVoucherNo()
        {
            var count = await _context.FeeChallans.CountAsync();
            return $"ALD-V-{1000 + count + 1}";
        }
    }
}
