using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Readhtpk.Models.ViewModels
{
    public class ExamCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đề thi không được để trống")]
        [Display(Name = "Tên đề thi")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Thời lượng (phút)")]
        [Range(1, 300, ErrorMessage = "Thời lượng phải từ 1 đến 300 phút")]
        public int DurationMinutes { get; set; } = 60;

        [Display(Name = "Tổng điểm")]
        public decimal TotalMarks { get; set; }

        [Display(Name = "Ngày thi")]
        public DateTime ExamDate { get; set; } = DateTime.Now;

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        [Display(Name = "Môn học")]
        public int SubjectId { get; set; }

        [Display(Name = "Người tạo")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Danh sách câu hỏi (cho Google Forms style)
        [Display(Name = "Câu hỏi")]
        public List<QuestionInputViewModel> Questions { get; set; } = new();
    }

    public class QuestionInputViewModel
    {
        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đáp án A không được để trống")]
        public string AnswerA { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đáp án B không được để trống")]
        public string AnswerB { get; set; } = string.Empty;

        public string? AnswerC { get; set; }
        public string? AnswerD { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đáp án đúng")]
        public string CorrectAnswer { get; set; } = string.Empty;

        public string? Explanation { get; set; }

        [Range(1, 3, ErrorMessage = "Độ khó phải từ 1 đến 3")]
        public int Difficulty { get; set; } = 1;

        [Range(0.5, 10, ErrorMessage = "Điểm phải từ 0.5 đến 10")]
        public decimal Marks { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}