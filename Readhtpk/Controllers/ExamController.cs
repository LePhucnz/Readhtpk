using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;
using System.Security.Claims;
using Readhtpk.Models.ViewModels;

namespace Readhtpk.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: Exam/Start/5
        public async Task<IActionResult> Start(int id)
        {
            var userId = GetCurrentUser();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Check if already taken
            var exists = await _context.ExamResults
                .AnyAsync(er => er.ExamId == id && er.UserId == userId);
            if (exists) return RedirectToAction("History");

            // Create exam result record
            var examResult = new ExamResult
            {
                UserId = userId,
                ExamId = id,
                StartTime = DateTime.Now,
                Status = "InProgress"
            };
            _context.ExamResults.Add(examResult);
            await _context.SaveChangesAsync();

            // ✅ SỬA: Lấy câu hỏi qua ExamQuestion, dùng đúng property names
            var questions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id && eq.Question.IsActive) // Chỉ lấy câu hỏi active
                .OrderBy(eq => eq.Order) // Có thể sort theo Order hoặc xáo ngẫu nhiên
                                         // .OrderBy(x => Guid.NewGuid()) // ✅ Bỏ comment này nếu muốn xáo ngẫu nhiên
                .Select(eq => new QuestionViewModel
                {
                    Id = eq.Question.Id,
                    Content = eq.Question.Content,
                    AnswerA = eq.Question.AnswerA,
                    AnswerB = eq.Question.AnswerB,
                    AnswerC = eq.Question.AnswerC,
                    AnswerD = eq.Question.AnswerD,
                    Marks = eq.Marks // Lấy điểm của câu này từ ExamQuestion
                })
                .ToListAsync();

            // Save to session
            HttpContext.Session.SetInt32("CurrentExamId", id);
            HttpContext.Session.SetInt32("CurrentExamResultId", examResult.Id);

            // Pass to view
            ViewBag.ExamTitle = exam.Title;
            ViewBag.DurationMinutes = exam.DurationMinutes; // ✅ SỬA: DurationMinutes
            ViewBag.StartTime = examResult.StartTime;
            ViewBag.TotalMarks = exam.TotalMarks;

            return View(questions);
        }

        // POST: Exam/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(List<UserAnswerViewModel> answers)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var examResultId = HttpContext.Session.GetInt32("CurrentExamResultId").Value;
                var examResult = await _context.ExamResults.FindAsync(examResultId);
                if (examResult == null) return BadRequest("Invalid exam session");

                var exam = await _context.Exams.FindAsync(examResult.ExamId);

                // Check timeout - ✅ SỬA: DurationMinutes
                if (DateTime.Now > examResult.StartTime.AddMinutes(exam.DurationMinutes))
                {
                    examResult.Status = "Timeout";
                }

                // Grade exam - So sánh SelectedAnswer với CorrectAnswer
                double score = 0;
                foreach (var ans in answers)
                {
                    // Save user's answer
                    _context.UserAnswers.Add(new UserAnswer
                    {
                        ExamResultId = examResultId,
                        QuestionId = ans.QuestionId,
                        SelectedAnswer = ans.SelectedAnswer?.ToUpper()
                    });

                    // Get correct answer from database
                    var question = await _context.Questions.FindAsync(ans.QuestionId);
                    if (question != null && question.CorrectAnswer?.ToUpper() == ans.SelectedAnswer?.ToUpper())
                    {
                        // ✅ Có thể cộng điểm theo Marks trong ExamQuestion nếu cần
                        score += 1; // Hoặc lấy từ ExamQuestion.Marks
                    }
                }

                // Update final result
                examResult.Score = score;
                examResult.EndTime = DateTime.Now;
                examResult.Status = "Submitted";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Clear session
                HttpContext.Session.Remove("CurrentExamId");
                HttpContext.Session.Remove("CurrentExamResultId");

                return RedirectToAction("Result", new { id = examResultId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Nên dùng ILogger để log thay vì return message
                return BadRequest("Error submitting exam");
            }
        }

        // GET: Exam/Result/5
        public async Task<IActionResult> Result(int id)
        {
            var userId = GetCurrentUser();
            var result = await _context.ExamResults
                .Include(er => er.Exam)
                    .ThenInclude(e => e.Subject)
                .Include(er => er.UserAnswers)
                    .ThenInclude(ua => ua.Question)
                .FirstOrDefaultAsync(er => er.Id == id && er.UserId == userId);

            if (result == null) return NotFound();

            return View(result);
        }

        // GET: Exam/History
        public async Task<IActionResult> History()
        {
            var userId = GetCurrentUser();
            var results = await _context.ExamResults
                .Include(er => er.Exam)
                    .ThenInclude(e => e.Subject)
                .Where(er => er.UserId == userId)
                .OrderByDescending(er => er.StartTime)
                .ToListAsync();

            return View(results);
        }

        private string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}