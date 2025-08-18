using EduVS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace EduVS.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Test> Tests => Set<Test>();
        public DbSet<Student> Students => Set<Student>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>(eb =>
            {
                eb.HasKey(s => s.Code);
                eb.Property(s => s.Code).HasMaxLength(10);
                eb.Property(s => s.Name).IsRequired();
            });

            modelBuilder.Entity<Test>(eb =>
            {
                eb.HasKey(t => t.Id);
                eb.Property(t => t.TimeStamp).IsRequired();
                eb.Property(t => t.Name).IsRequired();
                eb.Property(t => t.TemplatePathA).IsRequired();
                eb.Property(t => t.TemplatePathB).IsRequired(false);

                eb.HasOne(t => t.Subject)
                  .WithMany(s => s.Tests)
                  .HasForeignKey(t => t.SubjectCode)
                  .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Student>(eb =>
            {
                eb.HasKey(s => s.Id);
                eb.Property(s => s.Name).IsRequired();
                eb.Property(s => s.Surname).IsRequired();
                eb.Property(s => s.Class).IsRequired();
                eb.Property(s => s.Email).IsRequired();
            });
        }
    }
}
