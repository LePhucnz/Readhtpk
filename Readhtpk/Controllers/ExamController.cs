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

            // ✅ SỬA: Kiểm tra xem đã có bài thi chưa
            var existingResult = await _context.ExamResults
                .FirstOrDefaultAsync(er => er.ExamId == id && er.UserId == userId);

            if (existingResult != null)
            {
                // Nếu đã nộp rồi → không cho thi lại
                if (existingResult.Status == "Submitted" || existingResult.Status == "Timeout")
                {
                    return RedirectToAction("History");
                }

                // Nếu đang dở (InProgress) → tiếp tục thi
                if (existingResult.Status == "InProgress")
                {
                    // Kiểm tra xem có câu trả lời nào chưa
                    var existingAnswers = await _context.UserAnswers
                        .Where(ua => ua.ExamResultId == existingResult.Id)
                        .ToListAsync();

                    if (existingAnswers.Any())
                    {
                        // Đã có câu trả lời → redirect đến Result hoặc History
                        return RedirectToAction("Result", new { id = existingResult.Id });
                    }

                    // Chưa có câu trả lời → tiếp tục thi
                    // Xóa result cũ để tạo mới (hoặc reuse cũng được)
                    _context.ExamResults.Remove(existingResult);
                    await _context.SaveChangesAsync();
                }
            }

            // Tạo exam result mới
            var examResult = new ExamResult
            {
                UserId = userId,
                ExamId = id,
                StartTime = DateTime.Now,
                Status = "InProgress"
            };
            _context.ExamResults.Add(examResult);
            await _context.SaveChangesAsync();

            // Get questions for this exam
            var questions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id && eq.Question.IsActive)
                .OrderBy(eq => eq.Order)
                .Select(eq => new QuestionViewModel
                {
                    Id = eq.Question.Id,
                    Content = eq.Question.Content,
                    AnswerA = eq.Question.AnswerA,
                    AnswerB = eq.Question.AnswerB,
                    AnswerC = eq.Question.AnswerC,
                    AnswerD = eq.Question.AnswerD,
                    Marks = eq.Marks
                })
                .ToListAsync();

            // Save to session
            HttpContext.Session.SetInt32("CurrentExamId", id);
            HttpContext.Session.SetInt32("CurrentExamResultId", examResult.Id);

            ViewBag.ExamTitle = exam.Title;
            ViewBag.DurationMinutes = exam.DurationMinutes;
            ViewBag.StartTime = examResult.StartTime;
            ViewBag.TotalMarks = exam.TotalMarks;

            return View("Start", questions);
        }

        // POST: Exam/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(List<UserAnswerViewModel> answers)
        {
            try
            {
                var examResultId = HttpContext.Session.GetInt32("CurrentExamResultId");
                if (!examResultId.HasValue)
                    return BadRequest("No active exam session");

                var examResult = await _context.ExamResults.FindAsync(examResultId.Value);
                if (examResult == null)
                    return BadRequest("Invalid session");

                var exam = await _context.Exams.FindAsync(examResult.ExamId);

                // Check timeout
                if (DateTime.Now > examResult.StartTime.AddMinutes(exam.DurationMinutes))
                {
                    examResult.Status = "Timeout";
                }

                // ✅ CHẤM ĐIỂM ĐÚNG: So sánh với CorrectAnswer
                double score = 0;

                if (answers != null)
                {
                    foreach (var ans in answers)
                    {
                        // Lưu câu trả lời của user
                        _context.UserAnswers.Add(new UserAnswer
                        {
                            ExamResultId = examResultId.Value,
                            QuestionId = ans.QuestionId,
                            SelectedAnswer = ans.SelectedAnswer?.ToUpper() // Chuẩn hóa thành chữ hoa
                        });

                        // Lấy câu hỏi để chấm điểm
                        var question = await _context.Questions.FindAsync(ans.QuestionId);
                        if (question != null)
                        {
                            // So sánh đáp án người dùng chọn với CorrectAnswer trong DB
                            if (question.CorrectAnswer?.ToUpper() == ans.SelectedAnswer?.ToUpper())
                            {
                                score += 1; // Mỗi câu 1 điểm, có thể điều chỉnh theo ExamQuestion.Marks
                            }
                        }
                    }
                }

                // Update final result
                examResult.Score = score;
                examResult.EndTime = DateTime.Now;
                examResult.Status = "Submitted";

                await _context.SaveChangesAsync();

                // Clear session
                HttpContext.Session.Remove("CurrentExamId");
                HttpContext.Session.Remove("CurrentExamResultId");

                return RedirectToAction("Result", new { id = examResultId.Value });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
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
        [HttpGet]
        public async Task<IActionResult> History(int? examId)
        {
            var userId = GetCurrentUser();

            // ✅ BƯỚC 1: Khởi tạo query cơ bản (chưa sắp xếp)
            var query = _context.ExamResults
                .Include(er => er.Exam)
                    .ThenInclude(e => e.Subject)
                .Where(er => er.UserId == userId)
                .AsQueryable(); // Đảm bảo kiểu là IQueryable

            // ✅ BƯỚC 2: Áp dụng bộ lọc (nếu có examId)
            if (examId.HasValue)
            {
                query = query.Where(er => er.ExamId == examId.Value);

                // Lấy thông tin đề thi để hiển thị tiêu đề
                var exam = await _context.Exams.FindAsync(examId.Value);
                if (exam != null)
                {
                    ViewBag.ExamTitle = exam.Title;
                    ViewBag.FilteredExamId = examId.Value;
                }
            }

            // ✅ BƯỚC 3: Sắp xếp (OrderBy) - PHẢI LÀ BƯỚC CUỐI CÙNG
            var results = await query
                .OrderByDescending(er => er.StartTime)
                .ToListAsync();

            return View(results);
        }

        private string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        // GET: Exam/Retake/5 - Làm lại bài (GIỮ LẠI LỊCH SỬ CŨ)
        [HttpGet]
        public async Task<IActionResult> Retake(int id)
        {
            var userId = GetCurrentUser();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // ❌ XÓA PHẦN XÓA DỮ LIỆU CŨ (Để giữ lại lịch sử)
            // var oldResults = ... (Bỏ hoàn toàn block này)

            // 1️⃣ Đếm số lần làm trước đó để tăng AttemptNumber
            var previousAttempts = await _context.ExamResults
                .CountAsync(er => er.ExamId == id && er.UserId == userId);

            // 2️⃣ Tạo exam result MỚI (Không xóa cái cũ)
            var examResult = new ExamResult
            {
                UserId = userId,
                ExamId = id,
                StartTime = DateTime.Now,
                Status = "InProgress",
                AttemptNumber = previousAttempts + 1  // Lần làm thứ 2, 3, 4...
            };
            _context.ExamResults.Add(examResult);
            await _context.SaveChangesAsync();

            // 3️⃣ Lấy câu hỏi và XÁO TRỘN THỨ TỰ
            var questions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id && eq.Question.IsActive)
                .OrderBy(eq => Guid.NewGuid()) // 🎲 Xáo thứ tự câu hỏi
                .Select(eq => new QuestionViewModel
                {
                    Id = eq.Question.Id,
                    Content = eq.Question.Content,
                    AnswerA = eq.Question.AnswerA,
                    AnswerB = eq.Question.AnswerB,
                    AnswerC = eq.Question.AnswerC,
                    AnswerD = eq.Question.AnswerD,
                    Marks = eq.Marks
                })
                .ToListAsync();

            // 4️⃣ Lưu session
            HttpContext.Session.SetInt32("CurrentExamId", id);
            HttpContext.Session.SetInt32("CurrentExamResultId", examResult.Id);

            ViewBag.ExamTitle = exam.Title;
            ViewBag.DurationMinutes = exam.DurationMinutes;
            ViewBag.StartTime = examResult.StartTime;
            ViewBag.TotalMarks = exam.TotalMarks;
            ViewBag.IsRetake = true;

            return View("Start", questions);
        }
        // GET: Exam/TakeExam/5 - Action thông minh để vào thi
        [HttpGet]
        public async Task<IActionResult> TakeExam(int id)
        {
            var userId = GetCurrentUser();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Kiểm tra lịch sử làm bài của user với đề này
            var existingResult = await _context.ExamResults
                .Where(er => er.ExamId == id && er.UserId == userId)
                .OrderByDescending(er => er.StartTime)
                .FirstOrDefaultAsync();

            if (existingResult == null)
            {
                // ✅ Chưa làm lần nào → Vào Start để thi mới
                return RedirectToAction("Start", new { id = id });
            }

            if (existingResult.Status == "InProgress")
            {
                // ✅ Đang làm dở → Tiếp tục thi (vào lại Start với session cũ)
                // Kiểm tra xem có câu trả lời nào chưa
                var hasAnswers = await _context.UserAnswers
                    .AnyAsync(ua => ua.ExamResultId == existingResult.Id);

                if (hasAnswers)
                {
                    // Đã có câu trả lời → Khôi phục session và vào thi tiếp
                    HttpContext.Session.SetInt32("CurrentExamId", id);
                    HttpContext.Session.SetInt32("CurrentExamResultId", existingResult.Id);
                    return RedirectToAction("Start", new { id = id });
                }
                else
                {
                    // Chưa có câu trả lời → Có thể vào Start bình thường
                    return RedirectToAction("Start", new { id = id });
                }
            }

            if (existingResult.Status == "Submitted" || existingResult.Status == "Timeout")
            {
                // ✅ Đã nộp bài → Vào Retake để làm lại (xáo trộn câu hỏi)
                return RedirectToAction("Retake", new { id = id });
            }

            // Fallback: Vào Start
            return RedirectToAction("Start", new { id = id });
        }

    }
}