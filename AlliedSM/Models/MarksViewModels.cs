namespace AlliedSM.Models
{
    // One row per student in the bulk marks-entry grid — total marks is set once
    // (on the Exam itself), only obtained marks are entered here, class-wise.
    public class MarksEntryRow
    {
        public int StudentId { get; set; }
        public string RegistrationNo { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal ObtainedMarks { get; set; }
    }

    public class MarksEntryViewModel
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int TotalMarks { get; set; }
        public List<MarksEntryRow> Rows { get; set; } = new();
    }
}
