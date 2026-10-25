using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlliedSM.Models
{
    public enum ChallanStatus
    {
        Unpaid,
        Paid,
        Overdue
    }

    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        JazzCash,
        EasyPaisa
    }

    public class FeeChallan
    {
        [Key]
        public int FeeChallanId { get; set; }

        [Required, StringLength(20)]
        public string VoucherNo { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public decimal TuitionFee { get; set; }
        public decimal ExamFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal TransportFee { get; set; }   // kept for manual/custom challans (Fee/Create)

        // NEW — shown only on the student's first (admission) challan.
        public decimal AdmissionFee { get; set; }

        public decimal ArrearsAmount { get; set; }
        public decimal LateFeeFine { get; set; }

        [NotMapped]
        public decimal TotalPayable => TuitionFee + ExamFee + LabFee + TransportFee + AdmissionFee + ArrearsAmount + LateFeeFine;

        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public ChallanStatus Status { get; set; } = ChallanStatus.Unpaid;

        public PaymentMethod? PaidVia { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        public bool SmsSent { get; set; } = false;
    }
}
