using System.ComponentModel.DataAnnotations;

namespace AlliedSM.Models
{
    public class SchoolClass
    {
        [Key]
        public int SchoolClassId { get; set; }

        [Required, StringLength(50)]
        public string ClassName { get; set; } = string.Empty; // e.g. "Class 8", "Nursery"

        public int DisplayOrder { get; set; } // for sorting Nursery -> Class 10

        public ICollection<Section>? Sections { get; set; }
        public ICollection<Student>? Students { get; set; }
        public ICollection<ClassSubject>? ClassSubjects { get; set; }
    }

    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [Required, StringLength(10)]
        public string SectionName { get; set; } = string.Empty; // A, B, C

        public int SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public ICollection<Student>? Students { get; set; }
    }

    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required, StringLength(80)]
        public string SubjectName { get; set; } = string.Empty;

        public ICollection<ClassSubject>? ClassSubjects { get; set; }
    }

    // Many-to-many: which subject is taught in which class, by which teacher
    public class ClassSubject
    {
        [Key]
        public int ClassSubjectId { get; set; }

        public int SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? TeacherId { get; set; }
        public Staff? Teacher { get; set; }
    }
}
