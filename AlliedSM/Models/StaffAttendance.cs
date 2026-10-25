using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlliedSM.Models
{
    // One row per staff member per day, built from the biometric machine's CSV export.
    public class StaffAttendance
    {
        [Key]
        public int StaffAttendanceId { get; set; }

        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }

        public int LateMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public decimal LateFine { get; set; }

        [StringLength(100)]
        public string? Remarks { get; set; }

        [NotMapped]
        public double OvertimeHours => Math.Round(OvertimeMinutes / 60.0, 1);
    }
}
