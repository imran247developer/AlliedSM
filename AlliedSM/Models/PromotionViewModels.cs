namespace AlliedSM.Models
{
    public class PromotionViewModel
    {
        public int? FromClassId { get; set; }
        public int? FromSectionId { get; set; }
        public int ToClassId { get; set; }
        public int? ToSectionId { get; set; }
        public List<PromotionRow> Students { get; set; } = new();
    }

    public class PromotionRow
    {
        public int StudentId { get; set; }
        public string RegistrationNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}
