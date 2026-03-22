namespace Readhtpk.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AnswerA { get; set; } = string.Empty;
        public string AnswerB { get; set; } = string.Empty;
        public string AnswerC { get; set; } = string.Empty;
        public string AnswerD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int Difficulty { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public int SubjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Subject? Subject { get; set; }
        public ICollection<ExamQuestion>? ExamQuestions { get; set; }
    }
}