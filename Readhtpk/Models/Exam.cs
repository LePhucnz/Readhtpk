namespace Readhtpk.Models
{
    public class Exam
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public int TotalQuestions { get; set; }
        public decimal TotalMarks { get; set; }
        public DateTime ExamDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int SubjectId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Subject? Subject { get; set; }
        public ICollection<ExamQuestion>? ExamQuestions { get; set; }
    }
}