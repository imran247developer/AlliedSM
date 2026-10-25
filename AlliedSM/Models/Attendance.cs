using System.ComponentModel.DataAnnotations;

namespace AlliedSM.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Leave,
        Late
    }

    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        public AttendanceStatus Status { get; set; }

        [StringLength(200)]
        public string? Remarks { get; set; }

        public bool SmsSent { get; set; } = false; // set true after alert dispatched for Absent
    }
}
