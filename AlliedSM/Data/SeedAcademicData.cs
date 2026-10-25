using AlliedSM.Models;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Data
{
    // Call SeedAcademicData.RunAsync(context) once from Program.cs (see README)
    // to populate a starter list of classes and sections so the Admission
    // dropdowns are not empty on first run. Safe to run multiple times.
    public static class SeedAcademicData
    {
        public static async Task RunAsync(ApplicationDbContext context)
        {
            if (await context.SchoolClasses.AnyAsync()) return; // already seeded

            var classNames = new[] { "Nursery", "KG", "Class 1", "Class 2", "Class 3", "Class 4",
                                      "Class 5", "Class 6", "Class 7", "Class 8", "Class 9", "Class 10" };

            for (int i = 0; i < classNames.Length; i++)
            {
                var cls = new SchoolClass { ClassName = classNames[i], DisplayOrder = i };
                cls.Sections = new List<Section>
                {
                    new Section { SectionName = "A" },
                    new Section { SectionName = "B" }
                };
                context.SchoolClasses.Add(cls);
            }

            var subjects = new[] { "English", "Urdu", "Mathematics", "Science", "Islamiyat",
                                    "Social Studies", "Computer Science" };
            foreach (var s in subjects)
                context.Subjects.Add(new Subject { SubjectName = s });

            await context.SaveChangesAsync();
        }
    }
}
