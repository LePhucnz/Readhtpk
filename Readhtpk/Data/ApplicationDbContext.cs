using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Models;

namespace Readhtpk.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ nhiều-nhiều giữa Exam và Question
            modelBuilder.Entity<ExamQuestion>()
                .HasKey(eq => new { eq.ExamId, eq.QuestionId });

            // Quan hệ ExamQuestion → Exam (NO ACTION khi xóa)
            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Exam)
                .WithMany(e => e.ExamQuestions)
                .HasForeignKey(eq => eq.ExamId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Quan hệ ExamQuestion → Question (NO ACTION khi xóa)
            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Question)
                .WithMany(q => q.ExamQuestions)
                .HasForeignKey(eq => eq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict); 

            // Cấu hình Subject
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Code).IsRequired().HasMaxLength(50);
            });

            // Cấu hình Question
            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(q => q.Content).IsRequired();
                entity.Property(q => q.CorrectAnswer).IsRequired().HasMaxLength(1);
            });

            // Cấu hình Exam
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            });

            // Cấu hình ExamResult
            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.HasOne(er => er.User)
                    .WithMany()
                    .HasForeignKey(er => er.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(er => er.Exam)
                    .WithMany()
                    .HasForeignKey(er => er.ExamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(er => er.Status).HasMaxLength(20);
            });

            // Cấu hình UserAnswer
            modelBuilder.Entity<UserAnswer>(entity =>
            {
                entity.HasOne(ua => ua.ExamResult)
                    .WithMany(er => er.UserAnswers)
                    .HasForeignKey(ua => ua.ExamResultId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ua => ua.Question)
                    .WithMany()
                    .HasForeignKey(ua => ua.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(ua => ua.SelectedAnswer).HasMaxLength(1);
            });
        }
    }
}