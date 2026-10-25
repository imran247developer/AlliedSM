using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlliedSM.Models
{
    public class Staff
    {
        [Key]
        public int StaffId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Cnic { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Designation { get; set; } = string.Empty; // Teacher / Accountant / Admin staff

        [StringLength(20)]
        public string? Mobile { get; set; }

        public decimal BaseSalary { get; set; }

        public bool IsActive { get; set; } = true;

        // Link to login account (ASP.NET Identity user id)
        public string? ApplicationUserId { get; set; }

        public ICollection<SalarySlip>? SalarySlips { get; set; }
    }

    public class SalarySlip
    {
        [Key]
        public int SalarySlipId { get; set; }

        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        public int Month { get; set; }   // 1-12
        public int Year { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Fines { get; set; }
        public decimal Deductions { get; set; }

        [NotMapped]
        public decimal NetPayable => BaseSalary + Allowances - Fines - Deductions;

        public bool IsPaid { get; set; } = false;
        public DateTime? PaidDate { get; set; }
    }

    public enum ExpenseCategory
    {
        Utility,
        Maintenance,
        Stationery,
        PettyCash,
        Salary,
        Other
    }

    public class Expense
    {
        [Key]
        public int ExpenseId { get; set; }

        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        public ExpenseCategory Category { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public bool IsApproved { get; set; } = false;
    }

    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required, StringLength(80)]
        public string ExamName { get; set; } = string.Empty; // "Mid-Term 2026"

        public int SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int TotalMarks { get; set; } = 100;

        public ICollection<Mark>? Marks { get; set; }
    }

    public class Mark
    {
        [Key]
        public int MarkId { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public decimal ObtainedMarks { get; set; }

        [NotMapped]
        public string Grade
        {
            get
            {
                if (Exam == null || Exam.TotalMarks == 0) return "-";
                var pct = (ObtainedMarks / Exam.TotalMarks) * 100;
                return pct switch
                {
                    >= 90 => "A+",
                    >= 80 => "A",
                    >= 70 => "B",
                    >= 60 => "C",
                    >= 50 => "D",
                    >= 33 => "E",
                    _ => "F"
                };
            }
        }
    }
}
