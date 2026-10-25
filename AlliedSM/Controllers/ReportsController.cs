using AlliedSM.Data;
using AlliedSM.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Controllers
{
    // Excel (.xlsx) exports for every major report — matches the
    // "Exportable Data Reports: Excel/PDF/Print" requirement.
    [Authorize(Roles = "Admin,Accountant")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportsController(ApplicationDbContext context) => _context = context;

        // GET: Reports — a simple menu of everything that can be exported
        public IActionResult Index() => View();

        // GET: Reports/ExportStudents?classId=5 (classId optional — omit for all classes)
        public async Task<IActionResult> ExportStudents(int? classId)
        {
            var query = _context.Students.Include(s => s.SchoolClass).Include(s => s.Section)
                .Where(s => s.IsActive).AsQueryable();
            if (classId.HasValue) query = query.Where(s => s.SchoolClassId == classId);
            var students = await query.OrderBy(s => s.StudentName).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Students");
            string[] headers = { "Reg. No", "Student Name", "Father Name", "Class", "Section", "Gender", "DOB", "Parent Mobile", "Monthly Fee" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var s in students)
            {
                ws.Cell(row, 1).Value = s.RegistrationNo;
                ws.Cell(row, 2).Value = s.StudentName;
                ws.Cell(row, 3).Value = s.FatherName;
                ws.Cell(row, 4).Value = s.SchoolClass?.ClassName;
                ws.Cell(row, 5).Value = s.Section?.SectionName;
                ws.Cell(row, 6).Value = s.Gender;
                ws.Cell(row, 7).Value = s.DateOfBirth.ToString("dd-MMM-yyyy");
                ws.Cell(row, 8).Value = s.ParentMobile;
                ws.Cell(row, 9).Value = s.MonthlyFeeTotal;
                row++;
            }
            ws.Columns().AdjustToContents();
            return ExcelFile(wb, "Students");
        }

        // GET: Reports/ExportAttendance?fromDate=..&toDate=..&classId=..
        public async Task<IActionResult> ExportAttendance(DateTime? fromDate, DateTime? toDate, int? classId)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-7);
            var to = toDate ?? DateTime.Today;

            var query = _context.Attendances.Include(a => a.Student).ThenInclude(s => s!.SchoolClass)
                .Where(a => a.AttendanceDate.Date >= from.Date && a.AttendanceDate.Date <= to.Date).AsQueryable();
            if (classId.HasValue) query = query.Where(a => a.Student!.SchoolClassId == classId);
            var records = await query.OrderBy(a => a.AttendanceDate).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Attendance");
            string[] headers = { "Date", "Reg. No", "Student Name", "Class", "Status", "Remarks" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var a in records)
            {
                ws.Cell(row, 1).Value = a.AttendanceDate.ToString("dd-MMM-yyyy");
                ws.Cell(row, 2).Value = a.Student?.RegistrationNo;
                ws.Cell(row, 3).Value = a.Student?.StudentName;
                ws.Cell(row, 4).Value = a.Student?.SchoolClass?.ClassName;
                ws.Cell(row, 5).Value = a.Status.ToString();
                ws.Cell(row, 6).Value = a.Remarks;
                row++;
            }
            ws.Columns().AdjustToContents();
            return ExcelFile(wb, "Attendance");
        }

        // GET: Reports/ExportFee?fromDate=..&toDate=..
        public async Task<IActionResult> ExportFee(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddMonths(-1);
            var to = toDate ?? DateTime.Today;

            var challans = await _context.FeeChallans.Include(f => f.Student).ThenInclude(s => s!.SchoolClass)
                .Where(f => f.IssueDate.Date >= from.Date && f.IssueDate.Date <= to.Date)
                .OrderBy(f => f.IssueDate).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fee Challans");
            string[] headers = { "Voucher #", "Student", "Class", "Total Payable", "Issue Date", "Due Date", "Status", "Paid Via", "Paid Date" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var c in challans)
            {
                ws.Cell(row, 1).Value = c.VoucherNo;
                ws.Cell(row, 2).Value = c.Student?.StudentName;
                ws.Cell(row, 3).Value = c.Student?.SchoolClass?.ClassName;
                ws.Cell(row, 4).Value = c.TotalPayable;
                ws.Cell(row, 5).Value = c.IssueDate.ToString("dd-MMM-yyyy");
                ws.Cell(row, 6).Value = c.DueDate.ToString("dd-MMM-yyyy");
                ws.Cell(row, 7).Value = c.Status.ToString();
                ws.Cell(row, 8).Value = c.PaidVia?.ToString() ?? "-";
                ws.Cell(row, 9).Value = c.PaidDate?.ToString("dd-MMM-yyyy") ?? "-";
                row++;
            }
            ws.Columns().AdjustToContents();
            return ExcelFile(wb, "Fee-Challans");
        }

        // GET: Reports/ExportPayroll?month=..&year=..
        public async Task<IActionResult> ExportPayroll(int? month, int? year)
        {
            var m = month ?? DateTime.Today.Month;
            var y = year ?? DateTime.Today.Year;

            var slips = await _context.SalarySlips.Include(s => s.Staff)
                .Where(s => s.Month == m && s.Year == y).OrderBy(s => s.Staff!.FullName).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Payroll");
            string[] headers = { "Staff Name", "Designation", "Base Salary", "Allowances", "Fines", "Deductions", "Net Payable", "Status" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var s in slips)
            {
                ws.Cell(row, 1).Value = s.Staff?.FullName;
                ws.Cell(row, 2).Value = s.Staff?.Designation;
                ws.Cell(row, 3).Value = s.BaseSalary;
                ws.Cell(row, 4).Value = s.Allowances;
                ws.Cell(row, 5).Value = s.Fines;
                ws.Cell(row, 6).Value = s.Deductions;
                ws.Cell(row, 7).Value = s.NetPayable;
                ws.Cell(row, 8).Value = s.IsPaid ? "Paid" : "Pending";
                row++;
            }
            ws.Columns().AdjustToContents();
            return ExcelFile(wb, $"Payroll-{m}-{y}");
        }

        // GET: Reports/ExportAccounts?fromDate=..&toDate=.. — income + expense in one workbook (2 sheets)
        public async Task<IActionResult> ExportAccounts(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var income = await _context.FeeChallans.Include(f => f.Student)
                .Where(f => f.Status == ChallanStatus.Paid && f.PaidDate.HasValue
                            && f.PaidDate.Value.Date >= from.Date && f.PaidDate.Value.Date <= to.Date)
                .OrderBy(f => f.PaidDate).ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.IsApproved && e.ExpenseDate.Date >= from.Date && e.ExpenseDate.Date <= to.Date)
                .OrderBy(e => e.ExpenseDate).ToListAsync();

            using var wb = new XLWorkbook();

            var wsIncome = wb.Worksheets.Add("Income");
            wsIncome.Cell(1, 1).Value = "Voucher #"; wsIncome.Cell(1, 2).Value = "Student";
            wsIncome.Cell(1, 3).Value = "Amount"; wsIncome.Cell(1, 4).Value = "Paid Via"; wsIncome.Cell(1, 5).Value = "Paid Date";
            wsIncome.Row(1).Style.Font.Bold = true;
            int r = 2;
            foreach (var c in income)
            {
                wsIncome.Cell(r, 1).Value = c.VoucherNo;
                wsIncome.Cell(r, 2).Value = c.Student?.StudentName;
                wsIncome.Cell(r, 3).Value = c.TotalPayable;
                wsIncome.Cell(r, 4).Value = c.PaidVia?.ToString();
                wsIncome.Cell(r, 5).Value = c.PaidDate?.ToString("dd-MMM-yyyy");
                r++;
            }
            wsIncome.Cell(r + 1, 2).Value = "Total Income:";
            wsIncome.Cell(r + 1, 3).Value = income.Sum(c => c.TotalPayable);
            wsIncome.Columns().AdjustToContents();

            var wsExpense = wb.Worksheets.Add("Expenses");
            wsExpense.Cell(1, 1).Value = "Date"; wsExpense.Cell(1, 2).Value = "Category";
            wsExpense.Cell(1, 3).Value = "Description"; wsExpense.Cell(1, 4).Value = "Amount";
            wsExpense.Row(1).Style.Font.Bold = true;
            r = 2;
            foreach (var e in expenses)
            {
                wsExpense.Cell(r, 1).Value = e.ExpenseDate.ToString("dd-MMM-yyyy");
                wsExpense.Cell(r, 2).Value = e.Category.ToString();
                wsExpense.Cell(r, 3).Value = e.Description;
                wsExpense.Cell(r, 4).Value = e.Amount;
                r++;
            }
            wsExpense.Cell(r + 1, 3).Value = "Total Expense:";
            wsExpense.Cell(r + 1, 4).Value = expenses.Sum(e => e.Amount);
            wsExpense.Columns().AdjustToContents();

            return ExcelFile(wb, "School-Accounts");
        }

        private FileContentResult ExcelFile(XLWorkbook wb, string baseName)
        {
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var fileName = $"{baseName}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
