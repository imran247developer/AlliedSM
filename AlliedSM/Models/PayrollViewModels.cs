namespace AlliedSM.Models
{
    public class GenerateSalaryViewModel
    {
        public int Month { get; set; } = DateTime.Today.Month;
        public int Year { get; set; } = DateTime.Today.Year;
    }
}
