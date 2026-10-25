namespace AlliedSM.Models
{
    // One parsed row from the uploaded Excel sheet.
    public class ImportRow
    {
        public int ExcelRow { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string ExcelClass { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public string? StId { get; set; }
    }

    // "Excel class name" -> "which class in the database". Target is either the
    // text "new" (create the class) or an existing SchoolClassId.
    public class ClassMapItem
    {
        public string ExcelClass { get; set; } = string.Empty;
        public string Target { get; set; } = "new";
        public int StudentCount { get; set; }
    }

    // Step 2 screen (preview + class mapping).
    public class ImportPreviewVM
    {
        public string Token { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public List<ClassMapItem> Mappings { get; set; } = new();
        public List<ImportRow> Sample { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<SchoolClass> ExistingClasses { get; set; } = new();
    }

    // What the preview form posts back.
    public class ImportConfirmVM
    {
        public string Token { get; set; } = string.Empty;
        public List<ClassMapItem> Mappings { get; set; } = new();
        public bool UseStIdAsRegNo { get; set; }
        public bool GenerateChallans { get; set; }
        public DateTime? ChallanDueDate { get; set; }
    }

    // Step 3 screen (result).
    public class ImportResultVM
    {
        public int Imported { get; set; }
        public int ChallansCreated { get; set; }
        public int ClassesCreated { get; set; }
        public List<string> Skipped { get; set; } = new();
    }
}
