using Microsoft.AspNetCore.Mvc.Rendering;

namespace AlliedSM.Models
{
    // One parsed line of the uploaded Excel sheet.
    public class ExcelStudentRow
    {
        public string Name { get; set; } = "";
        public string Father { get; set; } = "";
        public string StId { get; set; } = "";
        public string ClassName { get; set; } = "";
        public decimal Fee { get; set; }
    }

    // "Excel class name  ->  class in the system" choice shown on the preview page.
    public class ExcelClassMap
    {
        public string ExcelClass { get; set; } = "";
        public int Count { get; set; }
        public int? SchoolClassId { get; set; }   // null = create a new class with the Excel name
    }

    public class ExcelImportPreviewVm
    {
        public string RowsJson { get; set; } = "";
        public List<ExcelStudentRow> Rows { get; set; } = new();
        public List<ExcelClassMap> Maps { get; set; } = new();
        public List<SelectListItem> Classes { get; set; } = new();
    }

    public class ExcelImportResultVm
    {
        public int Imported { get; set; }
        public List<string> CreatedClasses { get; set; } = new();
        public int CreatedSections { get; set; }
        public List<string> Skipped { get; set; } = new();
        public string? Error { get; set; }
    }
}
