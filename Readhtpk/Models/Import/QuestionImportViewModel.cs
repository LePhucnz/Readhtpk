using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Readhtpk.Models.Import
{
    public class QuestionImportViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        [Display(Name = "Môn học")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file Excel")]
        [Display(Name = "File Excel (.xlsx)")]
        public IFormFile? File { get; set; }

        // Danh sách kết quả import để hiển thị báo cáo
        public List<QuestionImportResult>? Results { get; set; }

        // Thống kê
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }

    public class QuestionImportResult
    {
        public int RowNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}