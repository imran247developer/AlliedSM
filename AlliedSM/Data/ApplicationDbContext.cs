using AlliedSM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlliedSM.Data
{
    // ApplicationUser extends Identity's default user so every login (Admin, Teacher,
    // Accountant, Parent) is a normal Identity account with a Role assigned to it.
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public int? LinkedStudentId { get; set; }  // set for Parent accounts
        public int? LinkedStaffId { get; set; }    // set for Teacher/Accountant accounts
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<SchoolClass> SchoolClasses { get; set; } = null!;
        public DbSet<Section> Sections { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<ClassSubject> ClassSubjects { get; set; } = null!;
        public DbSet<Attendance> Attendances { get; set; } = null!;
        public DbSet<FeeChallan> FeeChallans { get; set; } = null!;
        public DbSet<Staff> Staff { get; set; } = null!;
        public DbSet<SalarySlip> SalarySlips { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Exam> Exams { get; set; } = null!;
        public DbSet<Mark> Marks { get; set; } = null!;
        public DbSet<Homework> Homeworks { get; set; } = null!;         // Module 10
        public DbSet<Notice> Notices { get; set; } = null!;             // Module 10
        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!; // Module 13
        public DbSet<StaffAttendance> StaffAttendances { get; set; } = null!; // Module 15
        public DbSet<SchoolSetting> SchoolSettings { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // required for Identity tables

            // Prevent double delete cascades (SQL Server disallows multiple cascade paths)
            builder.Entity<Student>()
                .HasOne(s => s.SchoolClass)
                .WithMany(c => c.Students)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(s => s.Section)
                .WithMany(sec => sec.Students)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ClassSubject>()
                .HasOne(cs => cs.Teacher)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<FeeChallan>()
                .HasIndex(f => f.VoucherNo)
                .IsUnique();

            builder.Entity<Student>()
                .HasIndex(s => s.RegistrationNo)
                .IsUnique();
        }
    }
}
