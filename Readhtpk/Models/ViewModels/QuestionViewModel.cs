using Microsoft.AspNetCore.Mvc;

// Models/ViewModels/QuestionViewModel.cs
namespace Readhtpk.Models.ViewModels
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;

        // ✅ SỬA: Dùng đúng tên property của Question model
        public string AnswerA { get; set; } = string.Empty;
        public string AnswerB { get; set; } = string.Empty;
        public string AnswerC { get; set; } = string.Empty;
        public string AnswerD { get; set; } = string.Empty;

        public decimal Marks { get; set; } = 1; // Điểm câu hỏi (từ ExamQuestion.Marks)
    }
}
