using System.Text.RegularExpressions;
using System.Text.Json;
using AlliedSM.Data;
using AlliedSM.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    // Imports students (name, father name, class, monthly fee) from an Excel sheet.
    // Kept in its own controller so your existing StudentsController is not touched.
    [Authorize(Roles = "Admin")]
    public class ExcelImportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExcelImportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------- Step 1: upload page ----------
        public IActionResult Index() => View();

        // Blank template with the exact column headings the importer understands.
        public IActionResult Template()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Students");
            string[] heads = { "Student Name", "Father Name", "ST.ID", "Class", "Student Fee" };
            for (int i = 0; i < heads.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = heads[i];
                c.Style.Font.Bold = true;
                c.Style.Fill.BackgroundColor = XLColor.LightBlue;
            }
            ws.Cell(2, 1).Value = "Ali Ahmad"; ws.Cell(2, 2).Value = "Ahmad Khan"; ws.Cell(2, 4).Value = "Eight"; ws.Cell(2, 5).Value = 3500;
            ws.Cell(3, 1).Value = "Sara Noor";  ws.Cell(3, 2).Value = "Noor Ali";   ws.Cell(3, 4).Value = "Two";   ws.Cell(3, 5).Value = 3000;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Students-Import-Template.xlsx");
        }

        // ---------- Step 2: read the file, show class mapping + sample ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ImportError"] = "Please choose an Excel (.xlsx) file first.";
                return RedirectToAction(nameof(Index));
            }

            List<ExcelStudentRow> rows;
            try
            {
                using var stream = file.OpenReadStream();
                rows = ReadRows(stream);
            }
            catch (Exception ex)
            {
                TempData["ImportError"] = "Could not read the file: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }

            if (rows.Count == 0)
            {
                TempData["ImportError"] = "No student rows were found in the file.";
                return RedirectToAction(nameof(Index));
            }

            var classes = await _context.SchoolClasses.OrderBy(c => c.DisplayOrder).ToListAsync();

            var maps = rows
                .GroupBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ExcelClassMap
                {
                    ExcelClass = g.Key,
                    Count = g.Count(),
                    SchoolClassId = classes.FirstOrDefault(c => Norm(c.ClassName) == Norm(g.Key))?.SchoolClassId
                })
                .OrderBy(m => SortKey(m.ExcelClass))
                .ToList();

            var vm = new ExcelImportPreviewVm
            {
                Rows = rows,
                RowsJson = JsonSerializer.Serialize(rows),
                Maps = maps,
                Classes = classes.Select(c => new SelectListItem(c.ClassName, c.SchoolClassId.ToString())).ToList()
            };
            return View(vm);
        }

        // ---------- Step 3: save ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string rowsJson, List<ExcelClassMap> maps)
        {
            var result = new ExcelImportResultVm();
            try
            {
                var rows = JsonSerializer.Deserialize<List<ExcelStudentRow>>(rowsJson ?? "[]") ?? new List<ExcelStudentRow>();

                // 1) resolve (or create) the classes
                var classes = await _context.SchoolClasses.ToListAsync();
                var nextOrder = classes.Any() ? classes.Max(c => c.DisplayOrder) + 1 : 1;
                var classIdByExcel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var created = new List<(string excel, SchoolClass cls)>();

                foreach (var m in maps)
                {
                    if (m.SchoolClassId.HasValue && classes.Any(c => c.SchoolClassId == m.SchoolClassId.Value))
                    {
                        classIdByExcel[m.ExcelClass] = m.SchoolClassId.Value;
                        continue;
                    }
                    var cls = new SchoolClass { ClassName = m.ExcelClass, DisplayOrder = nextOrder++ };
                    _context.SchoolClasses.Add(cls);
                    classes.Add(cls);
                    created.Add((m.ExcelClass, cls));
                    result.CreatedClasses.Add(m.ExcelClass);
                }
                if (created.Count > 0)
                {
                    await _context.SaveChangesAsync();
                    foreach (var (excel, cls) in created) classIdByExcel[excel] = cls.SchoolClassId;
                }

                // 2) a section for every class (Student.SectionId is required)
                var sectionByClass = new Dictionary<int, int>();
                foreach (var classId in classIdByExcel.Values.Distinct())
                    sectionByClass[classId] = await GetOrCreateSectionIdAsync(classId, result);

                // 3) existing data, for duplicate checks and registration numbers
                var existing = await _context.Students
                    .Select(s => new { s.StudentName, s.FatherName, s.SchoolClassId, s.RegistrationNo })
                    .ToListAsync();
                var seen = new HashSet<string>(existing.Select(s => DupKey(s.StudentName, s.FatherName, s.SchoolClassId)));
                var regSet = new HashSet<string>(existing.Select(s => s.RegistrationNo), StringComparer.OrdinalIgnoreCase);

                var prefix = $"ALD-{DateTime.Now.Year}-";
                int nextReg = existing
                    .Select(s => s.RegistrationNo)
                    .Where(r => r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(r => int.TryParse(r.Substring(prefix.Length), out var n) ? n : 0)
                    .DefaultIfEmpty(0).Max() + 1;

                // 4) build the students
                foreach (var r in rows)
                {
                    if (!classIdByExcel.TryGetValue(r.ClassName, out var classId))
                    {
                        result.Skipped.Add($"{r.Name} - class '{r.ClassName}' was not mapped");
                        continue;
                    }

                    var father = string.IsNullOrWhiteSpace(r.Father) ? "N/A" : r.Father;
                    var key = DupKey(r.Name, father, classId);
                    if (!seen.Add(key))
                    {
                        result.Skipped.Add($"{r.Name} ({r.ClassName}) - already exists");
                        continue;
                    }

                    string reg;
                    var wanted = (r.StId ?? "").Trim();
                    if (wanted.Length > 0 && wanted != "0" && wanted.Length <= 20 && !regSet.Contains(wanted))
                        reg = wanted;
                    else
                    {
                        do { reg = prefix + (nextReg++).ToString("0000"); } while (regSet.Contains(reg));
                    }
                    regSet.Add(reg);

                    _context.Students.Add(new Student
                    {
                        RegistrationNo = reg,
                        StudentName = Trunc(r.Name, 100),
                        FatherName = Trunc(father, 100),
                        ParentMobile = "",                       // not in the Excel - fill in later via Edit
                        Gender = "Male",                         // default - edit if needed
                        DateOfBirth = new DateTime(2015, 1, 1),  // placeholder - edit if needed
                        SchoolClassId = classId,
                        SectionId = sectionByClass[classId],
                        AdmissionDate = DateTime.Now,
                        IsActive = true,
                        TuitionFee = r.Fee,
                        ExamFee = 0,
                        LabFee = 0,
                        AdmissionFee = 0
                    });
                    result.Imported++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Error = ex.GetBaseException().Message;
            }
            return View("Result", result);
        }

        // ---------- helpers ----------

        // Finds a Section for the class. Uses EF metadata, so it works whatever the
        // Section model's property names are. Creates section "A" if none exists.
        private async Task<int> GetOrCreateSectionIdAsync(int classId, ExcelImportResultVm result)
        {
            var et = _context.Model.FindEntityType(typeof(Section))!;
            var pkName = et.FindPrimaryKey()!.Properties[0].Name;
            var fk = et.GetForeignKeys().FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(SchoolClass));
            var fkName = fk?.Properties[0].Name;

            var sections = await _context.Sections.ToListAsync();

            int IdOf(Section s) => Convert.ToInt32(_context.Entry(s).Property(pkName).CurrentValue);
            int? ClassOf(Section s)
            {
                if (fkName == null) return null;
                var v = _context.Entry(s).Property(fkName).CurrentValue;
                return v == null ? null : Convert.ToInt32(v);
            }

            var match = sections.FirstOrDefault(s => fkName == null || ClassOf(s) == classId);
            if (match != null) return IdOf(match);

            var sec = new Section();
            var entry = _context.Entry(sec);
            if (fkName != null) entry.Property(fkName).CurrentValue = classId;
            var nameProp = et.GetProperties()
                .FirstOrDefault(p => p.ClrType == typeof(string) && !p.IsKey() && !p.IsForeignKey());
            if (nameProp != null) entry.Property(nameProp.Name).CurrentValue = "A";

            _context.Sections.Add(sec);
            await _context.SaveChangesAsync();
            result.CreatedSections++;
            return IdOf(sec);
        }

        private static List<ExcelStudentRow> ReadRows(Stream stream)
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var header = ws.FirstRowUsed() ?? throw new Exception("The sheet is empty.");

            var cols = new Dictionary<string, int>();
            foreach (var c in header.CellsUsed())
                cols[HeadKey(c.GetString())] = c.Address.ColumnNumber;

            int Col(params string[] names)
            {
                foreach (var n in names)
                    if (cols.TryGetValue(HeadKey(n), out var i)) return i;
                return 0;
            }

            int cName   = Col("Student Name", "Name", "StudentName");
            int cFather = Col("Father Name", "FatherName");
            int cStId   = Col("ST.ID", "ST ID", "Student ID", "Registration No");
            int cClass  = Col("Class");
            int cFee    = Col("Student Fee", "Monthly Fee", "Fee", "Tuition Fee");

            if (cName == 0 || cClass == 0)
                throw new Exception("Columns 'Student Name' and 'Class' were not found in the first row.");

            string Get(IXLRow row, int col) =>
                col == 0 ? "" : Regex.Replace(row.Cell(col).GetString().Trim(), @"\s+", " ");

            var list = new List<ExcelStudentRow>();
            foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() > header.RowNumber()))
            {
                var name = Get(row, cName);
                var cls = Get(row, cClass);
                if (name.Length == 0 || cls.Length == 0) continue;

                decimal fee = 0;
                if (cFee != 0)
                {
                    var cell = row.Cell(cFee);
                    if (!cell.TryGetValue<decimal>(out fee))
                        decimal.TryParse(cell.GetString().Replace(",", "").Trim(), out fee);
                }

                list.Add(new ExcelStudentRow
                {
                    Name = name,
                    Father = Get(row, cFather),
                    StId = Get(row, cStId),
                    ClassName = cls,
                    Fee = fee
                });
            }
            return list;
        }

        private static string HeadKey(string s) =>
            new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string DupKey(string name, string father, int classId) =>
            $"{(name ?? "").Trim().ToLowerInvariant()}|{(father ?? "").Trim().ToLowerInvariant()}|{classId}";

        private static string Trunc(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        private static readonly Dictionary<string, string> Words = new()
        {
            ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4", ["five"] = "5",
            ["six"] = "6", ["seven"] = "7", ["eight"] = "8", ["nine"] = "9", ["ten"] = "10",
            ["playgroup"] = "pg"
        };

        // "Class 2", "Two", "Ten-26", "PG-26" -> comparable keys ("2", "2", "10", "pg")
        private static string Norm(string s)
        {
            var t = new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (t.StartsWith("class")) t = t.Substring(5);
            if (t.Length > 2 && t.EndsWith("26")) t = t.Substring(0, t.Length - 2);
            return Words.TryGetValue(t, out var w) ? w : t;
        }

        private static readonly string[] Order =
            { "pg", "prep", "nursery", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };

        private static int SortKey(string excelClass)
        {
            var i = Array.IndexOf(Order, Norm(excelClass));
            return i < 0 ? 99 : i;
        }
    }
}
