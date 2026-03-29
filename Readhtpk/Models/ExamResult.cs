namespace Readhtpk.Models
{
    // ExamResult.cs
    public class ExamResult
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }
        public int ExamId { get; set; }
        public Exam? Exam { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double Score { get; set; }
        public string Status { get; set; } = "InProgress";
        public int AttemptNumber { get; set; } = 1; // ✅ Số lần làm bài

        public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
    }
    // UserAnswer.cs
    public class UserAnswer
    {
        public int Id { get; set; }

        public int ExamResultId { get; set; }
        public ExamResult ExamResult { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }

        // Lưu đáp án người dùng chọn: "A", "B", "C", hoặc "D"
        public string SelectedAnswer { get; set; } // Giá trị: "A", "B", "C", "D"
    }
}
