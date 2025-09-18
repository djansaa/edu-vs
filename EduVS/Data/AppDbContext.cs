using EduVS.Models;
using Microsoft.EntityFrameworkCore;

namespace EduVS.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Test> Tests => Set<Test>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<ClassStudent> ClassStudents => Set<ClassStudent>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // subject
            b.Entity<Subject>(e =>
            {
                e.ToTable("subject");
                e.HasKey(x => x.Code);
                e.Property(x => x.Code).HasColumnName("code");
                e.Property(x => x.Name).HasColumnName("name").IsRequired();
            });

            // tests
            b.Entity<Test>(e =>
            {
                e.ToTable("test");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.SubjectCode).HasColumnName("subject_code");
                e.Property(x => x.TimeStamp).HasColumnName("time_stamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP"); // insert default
                e.Property(x => x.Name).HasColumnName("name").IsRequired();
                e.Property(x => x.TemplateAPath).HasColumnName("template_path_a").IsRequired();
                e.Property(x => x.TemplateBPath).HasColumnName("template_path_b");

                e.HasOne(x => x.Subject)
                 .WithMany(s => s.Tests)
                 .HasForeignKey(x => x.SubjectCode)
                 .OnDelete(DeleteBehavior.SetNull); // ON DELETE SET NULL (ON UPDATE)
            });

            // students
            b.Entity<Student>(e =>
            {
                e.ToTable("student");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Name).HasColumnName("name").IsRequired();
                e.Property(x => x.Surname).HasColumnName("surname").IsRequired();
                e.Property(x => x.Email).HasColumnName("email").IsRequired();
            });

            // classes
            b.Entity<Class>(e =>
            {
                e.ToTable("class");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Name).HasColumnName("name").IsRequired();
                e.Property(x => x.Year).HasColumnName("year").IsRequired();
            });

            // class_students (composite PK + CASCADE)
            b.Entity<ClassStudent>(e =>
            {
                e.ToTable("class_student");
                e.HasKey(x => new { x.ClassId, x.StudentId });
                e.Property(x => x.ClassId).HasColumnName("class_id");
                e.Property(x => x.StudentId).HasColumnName("student_id");

                e.HasOne(x => x.Class)
                 .WithMany(c => c.StudentLinks)
                 .HasForeignKey(x => x.ClassId)
                 .OnDelete(DeleteBehavior.Cascade); // ON DELETE CASCADE

                e.HasOne(x => x.Student)
                 .WithMany(s => s.ClassLinks)
                 .HasForeignKey(x => x.StudentId)
                 .OnDelete(DeleteBehavior.Cascade); // ON DELETE CASCADE
            });
        }
    }
}
