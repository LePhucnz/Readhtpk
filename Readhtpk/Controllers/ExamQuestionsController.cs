using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;

namespace Readhtpk.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamQuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamQuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ExamQuestions/ByExam/5
        public async Task<IActionResult> ByExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null) return NotFound();

            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.Subject)
                .Where(eq => eq.ExamId == examId)
                .OrderBy(eq => eq.Order)
                .ToListAsync();

            ViewBag.ExamTitle = exam.Title;
            ViewBag.ExamId = examId;
            ViewBag.TotalQuestions = examQuestions.Count;
            ViewBag.TotalMarks = examQuestions.Sum(eq => eq.Marks);

            return View(examQuestions);
        }

        // GET: ExamQuestions/AddToExam/5
        public async Task<IActionResult> AddToExam(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();

            // Lấy danh sách câu hỏi của môn này (chưa có trong đề)
            var existingQuestionIds = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .Select(eq => eq.QuestionId)
                .ToListAsync();

            var availableQuestions = await _context.Questions
                .Where(q => q.SubjectId == exam.SubjectId &&
                           q.IsActive &&
                           !existingQuestionIds.Contains(q.Id))
                .Include(q => q.Subject)
                .ToListAsync();

            ViewBag.ExamTitle = exam.Title;
            ViewBag.ExamId = examId;

            return View(availableQuestions);
        }

        // POST: ExamQuestions/AddToExam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToExam(int examId, List<int> questionIds)
        {
            if (questionIds == null || questionIds.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một câu hỏi";
                return RedirectToAction(nameof(AddToExam), new { examId });
            }

            // Lấy thứ tự cao nhất hiện tại
            var maxOrder = await _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .MaxAsync(eq => (int?)eq.Order) ?? 0;

            // Thêm các câu hỏi được chọn
            foreach (var questionId in questionIds)
            {
                // Kiểm tra trùng
                var exists = await _context.ExamQuestions
                    .AnyAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);

                if (!exists)
                {
                    _context.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamId = examId,
                        QuestionId = questionId,
                        Order = ++maxOrder,
                        Marks = 1 // Mặc định 1 điểm, có thể sửa sau
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Cập nhật TotalQuestions
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                exam.TotalQuestions = await _context.ExamQuestions
                    .CountAsync(eq => eq.ExamId == examId);
                exam.TotalMarks = await _context.ExamQuestions
                    .SumAsync(eq => eq.Marks);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã thêm {questionIds.Count} câu hỏi vào đề thi";
            return RedirectToAction(nameof(ByExam), new { examId });
        }

        // GET: ExamQuestions/Edit/5
        public async Task<IActionResult> Edit(int examId, int questionId)
        {
            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);

            if (examQuestion == null) return NotFound();

            ViewBag.ExamId = examId;
            return View(examQuestion);
        }

        // POST: ExamQuestions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int examId, int questionId, [Bind("Order,Marks")] ExamQuestion model)
        {
            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);

            if (examQuestion == null) return NotFound();

            examQuestion.Order = model.Order;
            examQuestion.Marks = model.Marks;

            try
            {
                await _context.SaveChangesAsync();

                // Cập nhật TotalMarks
                var exam = await _context.Exams.FindAsync(examId);
                if (exam != null)
                {
                    exam.TotalMarks = await _context.ExamQuestions
                        .Where(eq => eq.ExamId == examId)
                        .SumAsync(eq => eq.Marks);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Cập nhật thành công";
                return RedirectToAction(nameof(ByExam), new { examId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExamQuestionExists(examId, questionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: ExamQuestions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int examId, int questionId)
        {
            var examQuestion = await _context.ExamQuestions
                .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);

            if (examQuestion != null)
            {
                _context.ExamQuestions.Remove(examQuestion);

                // Cập nhật lại Order cho các câu sau
                var remainingQuestions = await _context.ExamQuestions
                    .Where(eq => eq.ExamId == examId && eq.Order > examQuestion.Order)
                    .OrderBy(eq => eq.Order)
                    .ToListAsync();

                foreach (var eq in remainingQuestions)
                {
                    eq.Order--;
                }

                // Cập nhật TotalQuestions và TotalMarks
                var exam = await _context.Exams.FindAsync(examId);
                if (exam != null)
                {
                    exam.TotalQuestions = remainingQuestions.Count;
                    exam.TotalMarks = remainingQuestions.Sum(eq => eq.Marks);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa câu hỏi khỏi đề thi";
            }

            return RedirectToAction(nameof(ByExam), new { examId });
        }

        private bool ExamQuestionExists(int examId, int questionId)
        {
            return _context.ExamQuestions.Any(e => e.ExamId == examId && e.QuestionId == questionId);
        }
    }
}