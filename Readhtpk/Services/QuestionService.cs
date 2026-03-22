using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Readhtpk.Data;
using Readhtpk.Models;

namespace Readhtpk.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuestionService> _logger;

        public QuestionService(ApplicationDbContext context, ILogger<QuestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ImportQuestionsFromExcelAsync(Stream fileStream, int subjectId)
        {
            var questions = new List<Question>();
            var errors = new List<string>();
            int successCount = 0;

            try
            {
                var rows = MiniExcel.Query<QuestionExcelDto>(fileStream).ToList();
                int rowIndex = 2;

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Content))
                    {
                        rowIndex++;
                        continue;
                    }

                    try
                    {
                        var correctAnswer = row.CorrectAnswer?.Trim().ToUpper();
                        if (!new[] { "A", "B", "C", "D" }.Contains(correctAnswer))
                        {
                            errors.Add($"Dòng {rowIndex}: Đáp án '{row.CorrectAnswer}' không hợp lệ");
                            rowIndex++;
                            continue;
                        }

                        var question = new Question
                        {
                            Content = row.Content.Trim(),
                            AnswerA = row.AnswerA?.Trim() ?? string.Empty,
                            AnswerB = row.AnswerB?.Trim() ?? string.Empty,
                            AnswerC = row.AnswerC?.Trim() ?? string.Empty,
                            AnswerD = row.AnswerD?.Trim() ?? string.Empty,
                            CorrectAnswer = correctAnswer,
                            Explanation = row.Explanation?.Trim(),
                            Difficulty = row.Difficulty > 0 && row.Difficulty <= 3 ? row.Difficulty : 1,
                            SubjectId = subjectId,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        questions.Add(question);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Dòng {rowIndex}: {ex.Message}");
                    }
                    rowIndex++;
                }

                if (errors.Any())
                {
                    _logger.LogWarning("Lỗi import:\n{Errors}", string.Join("\n", errors.Take(20)));
                }

                if (questions.Any())
                {
                    await _context.Questions.AddRangeAsync(questions);
                    await _context.SaveChangesAsync();
                }

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi import Excel");
                throw new Exception($"Lỗi: {ex.Message}");
            }
        }

        public async Task<List<Question>> GetQuestionsBySubjectIdAsync(int subjectId)
        {
            return await _context.Questions
                .Include(q => q.Subject)
                .Where(q => q.SubjectId == subjectId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> CreateQuestionAsync(Question question)
        {
            try
            {
                question.CreatedAt = DateTime.Now;
                question.IsActive = true;
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo câu hỏi");
                return false;
            }
        }

        public async Task<bool> UpdateQuestionAsync(Question question)
        {
            try
            {
                _context.Questions.Update(question);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật câu hỏi");
                return false;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question != null)
                {
                    question.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa câu hỏi");
                return false;
            }
        }
    }

    public class QuestionExcelDto
    {
        public string Content { get; set; } = string.Empty;
        public string AnswerA { get; set; } = string.Empty;
        public string AnswerB { get; set; } = string.Empty;
        public string AnswerC { get; set; } = string.Empty;
        public string AnswerD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int Difficulty { get; set; } = 1;
    }
}