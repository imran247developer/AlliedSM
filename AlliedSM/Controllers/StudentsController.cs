using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? classId, int? sectionId, string? search)
        {
            var query = _context.Students
                .Include(s => s.SchoolClass)
                .Include(s => s.Section)
                .Where(s => s.IsActive)
                .AsQueryable();

            if (classId.HasValue) query = query.Where(s => s.SchoolClassId == classId);
            if (sectionId.HasValue) query = query.Where(s => s.SectionId == sectionId);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.StudentName.Contains(search) || s.RegistrationNo.Contains(search));

            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);
            ViewBag.Sections = new SelectList(await _context.Sections.ToListAsync(), "SectionId", "SectionName", sectionId);

            return View(await query.OrderByDescending(s => s.AdmissionDate).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.SchoolClass).Include(s => s.Section)
                .Include(s => s.FeeChallans)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Students/Create
        // Saves the student, auto-generates the Registration No. and the first
        // fee challan. The first challan's Admission Fee comes from the
        // student's (one-time) AdmissionFee field — it is not repeated on
        // later monthly challans (see FeeController.BulkGenerate).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            ModelState.Remove(nameof(Student.RegistrationNo));

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(student);
            }

            var year = DateTime.Now.Year;
            var countThisYear = await _context.Students.CountAsync(s => s.RegistrationNo.Contains($"-{year}-"));
            student.RegistrationNo = $"ALD-{year}-{(countThisYear + 1):D4}";
            student.AdmissionDate = DateTime.Now;
            student.IsActive = true;

            _context.Add(student);
            await _context.SaveChangesAsync();

            await GenerateChallanForStudent(student, DateTime.Now.AddDays(15));

            TempData["Success"] = $"Admission complete. Registration No: {student.RegistrationNo}. Fee challan generated.";
            return RedirectToAction(nameof(Details), new { id = student.StudentId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            await PopulateDropdowns();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentId) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(student);
            }

            try
            {
                _context.Update(student);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Students.AnyAsync(s => s.StudentId == id)) return NotFound();
                throw;
            }

            TempData["Success"] = "Student record updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students
                .Include(s => s.SchoolClass).Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                student.IsActive = false;
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Student record removed.";
            return RedirectToAction(nameof(Index));
        }

        private async Task GenerateChallanForStudent(Student student, DateTime dueDate)
        {
            var lastVoucher = await _context.FeeChallans.CountAsync();
            var voucherNo = $"ALD-V-{(1000 + lastVoucher + 1)}";

            var challan = new FeeChallan
            {
                VoucherNo = voucherNo,
                StudentId = student.StudentId,
                TuitionFee = student.TuitionFee,
                ExamFee = student.ExamFee,
                LabFee = student.LabFee,
                AdmissionFee = student.AdmissionFee,   // one-time, only on this first challan
                IssueDate = DateTime.Now,
                DueDate = dueDate,
                Status = ChallanStatus.Unpaid
            };
            _context.FeeChallans.Add(challan);
            await _context.SaveChangesAsync();
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.SchoolClassId = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName");
            ViewBag.SectionId = new SelectList(await _context.Sections.ToListAsync(), "SectionId", "SectionName");
        }
    }
}
