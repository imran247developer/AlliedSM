using System.ComponentModel.DataAnnotations;

namespace AlliedSM.Models
{
    public class Homework
    {
        [Key]
        public int HomeworkId { get; set; }

        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; } = DateTime.Today;

        public int SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        // Set by the teacher who is logged in (Staff record), for accountability
        public int? PostedByStaffId { get; set; }
        public Staff? PostedByStaff { get; set; }
    }

    public enum NoticeAudience
    {
        Everyone,
        StaffOnly,
        ParentsOnly
    }

    public class Notice
    {
        [Key]
        public int NoticeId { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public NoticeAudience Audience { get; set; } = NoticeAudience.Everyone;

        [DataType(DataType.Date)]
        public DateTime PostedDate { get; set; } = DateTime.Now;

        public bool IsUrgent { get; set; } = false;
    }
}
