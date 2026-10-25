using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlliedSM.Models
{
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Required, StringLength(300)]
        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        [DataType(DataType.Date)]
        public DateTime AppliedDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? AdminRemarks { get; set; }

        [NotMapped]
        public int DaysCount => (ToDate - FromDate).Days + 1;
    }
}
