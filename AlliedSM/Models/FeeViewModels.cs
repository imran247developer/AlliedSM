namespace AlliedSM.Models
{
    public class BulkGenerateViewModel
    {
        public int SchoolClassId { get; set; }
        public int? SectionId { get; set; }
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(15);
        public decimal LateFeeFineIfOverdue { get; set; } = 200;
    }
}
