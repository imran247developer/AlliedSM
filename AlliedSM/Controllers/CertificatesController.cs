using AlliedSM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CertificatesController(ApplicationDbContext context) => _context = context;

        // GET: Certificates — pick a class then a student, or jump straight to bulk ID cards
        public async Task<IActionResult> Index(int? classId)
        {
            ViewBag.Classes = new SelectList(await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync(), "SchoolClassId", "ClassName", classId);

            if (classId.HasValue)
            {
                ViewBag.Students = await _context.Students
                    .Where(s => s.SchoolClassId == classId && s.IsActive)
                    .OrderBy(s => s.StudentName).ToListAsync();
            }

            return View();
        }

        // GET: Certificates/IdCard/5 — single printable ID card (front + back)
        public async Task<IActionResult> IdCard(int id)
        {
            var student = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();
            return View(new List<AlliedSM.Models.Student> { student });
        }

        // GET: Certificates/BulkIdCards?classId=5 — a printable sheet, one card per active student
        public async Task<IActionResult> BulkIdCards(int classId)
        {
            var students = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .Where(s => s.SchoolClassId == classId && s.IsActive)
                .OrderBy(s => s.StudentName).ToListAsync();
            return View("IdCard", students);
        }

        // GET: Certificates/LeavingCertificate/5
        public async Task<IActionResult> LeavingCertificate(int id)
        {
            var student = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();
            return View(student);
        }

        // GET: Certificates/CharacterCertificate/5
        public async Task<IActionResult> CharacterCertificate(int id)
        {
            var student = await _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return NotFound();
            return View(student);
        }
    }
}
