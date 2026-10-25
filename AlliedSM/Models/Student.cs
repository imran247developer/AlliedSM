using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlliedSM.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required, StringLength(20)]
        public string RegistrationNo { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string StudentName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FatherName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? StudentCnicOrBForm { get; set; }

        [StringLength(20)]
        public string? FatherCnic { get; set; }

        [Required, StringLength(20)]
        public string ParentMobile { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required, StringLength(10)]
        public string Gender { get; set; } = "Male";

        public int SchoolClassId { get; set; }
        [ForeignKey(nameof(SchoolClassId))]
        public SchoolClass? SchoolClass { get; set; }

        public int SectionId { get; set; }
        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        [DataType(DataType.Date)]
        public DateTime AdmissionDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Monthly-recurring fee components
        public decimal TuitionFee { get; set; }
        public decimal ExamFee { get; set; }
        public decimal LabFee { get; set; }

        // CHANGED: TransportFee -> AdmissionFee.
        // Admission Fee is a ONE-TIME charge collected only on the first
        // (admission) challan — it does NOT repeat on every monthly Bulk
        // Generate run, which is why it is excluded from MonthlyFeeTotal below.
        public decimal AdmissionFee { get; set; }

        [NotMapped]
        public decimal MonthlyFeeTotal => TuitionFee + ExamFee + LabFee;

        public ICollection<Attendance>? AttendanceRecords { get; set; }
        public ICollection<FeeChallan>? FeeChallans { get; set; }
        public ICollection<Mark>? Marks { get; set; }
    }
}
