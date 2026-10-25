using AlliedSM.Data;
using AlliedSM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class MarksController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MarksController(ApplicationDbContext context) => _context = context;

        // GET: Marks/BulkEntry?examId=5
        // Total marks is already fixed on the Exam (set once in Exams/Create) — this screen
        // only shows the class-wise student list so obtained marks can be entered in one grid.
        public async Task<IActionResult> BulkEntry(int examId)
        {
            var exam = await _context.Exams.Include(e => e.SchoolClass).Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.ExamId == examId);
            if (exam == null) return NotFound();

            var students = await _context.Students
                .Where(s => s.SchoolClassId == exam.SchoolClassId && s.IsActive)
                .OrderBy(s => s.StudentName)
                .ToListAsync();

            var existingMarks = await _context.Marks.Where(m => m.ExamId == examId).ToListAsync();

            var vm = new MarksEntryViewModel
            {
                ExamId = examId,
                ExamName = exam.ExamName,
                ClassName = exam.SchoolClass?.ClassName ?? "",
                SubjectName = exam.Subject?.SubjectName ?? "",
                TotalMarks = exam.TotalMarks,
                Rows = students.Select(s => new MarksEntryRow
                {
                    StudentId = s.StudentId,
                    RegistrationNo = s.RegistrationNo,
                    StudentName = s.StudentName,
                    ObtainedMarks = existingMarks.FirstOrDefault(m => m.StudentId == s.StudentId)?.ObtainedMarks ?? 0
                }).ToList()
            };

            return View(vm);
        }

        // POST: Marks/BulkEntry — saves the whole class's obtained marks for this exam in one go
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkEntry(MarksEntryViewModel vm)
        {
            foreach (var row in vm.Rows)
            {
                var mark = await _context.Marks.FirstOrDefaultAsync(m => m.ExamId == vm.ExamId && m.StudentId == row.StudentId);
                if (mark == null)
                {
                    mark = new Mark { ExamId = vm.ExamId, StudentId = row.StudentId };
                    _context.Marks.Add(mark);
                }
                mark.ObtainedMarks = row.ObtainedMarks;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marks save ho gaye — grade automatically calculate ho jayega.";
            return RedirectToAction(nameof(BulkEntry), new { examId = vm.ExamId });
        }

        // GET: Marks/ReportCard?studentId=5&examName=Mid-Term 2026 — printable, all subjects for that term
        public async Task<IActionResult> ReportCard(int studentId, string examName)
        {
            var student = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return NotFound();

            var marks = await _context.Marks
                .Include(m => m.Exam).ThenInclude(e => e!.Subject)
                .Where(m => m.StudentId == studentId && m.Exam!.ExamName == examName && m.Exam.SchoolClassId == student.SchoolClassId)
                .ToListAsync();

            ViewBag.Student = student;
            ViewBag.ExamName = examName;
            return View(marks);
        }
    }
}
