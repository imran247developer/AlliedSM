namespace AlliedSM.Models
{
    // One row per student, shown in the "Mark Attendance" grid
    public class AttendanceMarkRow
    {
        public int StudentId { get; set; }
        public string RegistrationNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public string? Remarks { get; set; }
    }

    public class AttendanceMarkViewModel
    {
        public int SchoolClassId { get; set; }
        public int SectionId { get; set; }
        public DateTime AttendanceDate { get; set; } = DateTime.Today;
        public List<AttendanceMarkRow> Rows { get; set; } = new();
    }
}
