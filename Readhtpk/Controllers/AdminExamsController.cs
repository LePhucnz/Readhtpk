using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;
using Readhtpk.Models.ViewModels;

namespace Readhtpk.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AdminExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminExams
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams
                .Include(e => e.Subject)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(exams);
        }

        // GET: AdminExams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null) return NotFound();

            return View(exam);
        }

        // GET: AdminExams/Create
        public IActionResult Create()
        {
            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name");
            return View();
        }

        // POST: AdminExams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamCreateViewModel model)
        {
            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name", model.SubjectId);

            if (ModelState.IsValid)
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var exam = new Exam
                            {
                                Title = model.Title,
                                Description = model.Description,
                                DurationMinutes = model.DurationMinutes,
                                TotalQuestions = model.Questions?.Count ?? 0,
                                TotalMarks = model.Questions?.Sum(q => q.Marks) ?? 0,
                                ExamDate = model.ExamDate,
                                IsActive = model.IsActive,
                                SubjectId = model.SubjectId,
                                CreatedBy = model.CreatedBy,
                                CreatedAt = DateTime.Now
                            };

                            _context.Exams.Add(exam);
                            await _context.SaveChangesAsync();

                            if (model.Questions != null && model.Questions.Any())
                            {
                                int order = 1;
                                foreach (var q in model.Questions)
                                {
                                    var question = new Question
                                    {
                                        Content = q.Content,
                                        AnswerA = q.AnswerA,
                                        AnswerB = q.AnswerB,
                                        AnswerC = q.AnswerC,
                                        AnswerD = q.AnswerD,
                                        CorrectAnswer = q.CorrectAnswer,
                                        Explanation = q.Explanation,
                                        Difficulty = q.Difficulty,
                                        IsActive = q.IsActive,
                                        SubjectId = model.SubjectId,
                                        CreatedAt = DateTime.Now
                                    };

                                    _context.Questions.Add(question);
                                    await _context.SaveChangesAsync();

                                    _context.ExamQuestions.Add(new ExamQuestion
                                    {
                                        ExamId = exam.Id,
                                        QuestionId = question.Id,
                                        Order = order++,
                                        Marks = q.Marks
                                    });
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["Success"] = "Tạo đề thi thành công!";
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError(string.Empty, $"Lỗi: {ex.Message}");
                            throw;
                        }
                    }
                });

                if (ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        // GET: AdminExams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();

            // Load câu hỏi từ ExamQuestions
            var questions = exam.ExamQuestions?
                .OrderBy(eq => eq.Order)
                .Select(eq => new QuestionInputViewModel
                {
                    Content = eq.Question.Content,
                    AnswerA = eq.Question.AnswerA,
                    AnswerB = eq.Question.AnswerB,
                    AnswerC = eq.Question.AnswerC,
                    AnswerD = eq.Question.AnswerD,
                    CorrectAnswer = eq.Question.CorrectAnswer,
                    Explanation = eq.Question.Explanation,
                    Difficulty = eq.Question.Difficulty,
                    Marks = eq.Marks,
                    IsActive = eq.Question.IsActive
                })
                .ToList() ?? new List<QuestionInputViewModel>();

            var model = new ExamCreateViewModel
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes,
                TotalMarks = exam.TotalMarks,
                ExamDate = exam.ExamDate,
                IsActive = exam.IsActive,
                SubjectId = exam.SubjectId,
                CreatedBy = exam.CreatedBy,
                Questions = questions  // ✅ Gán danh sách câu hỏi
            };

            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name", exam.SubjectId);

            return View(model);
        }

        // POST: AdminExams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamCreateViewModel model)
        {
            if (id != model.Id) return NotFound();

            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name", model.SubjectId);

            if (ModelState.IsValid)
            {
                // ✅ SỬA: Dùng execution strategy để bao quanh transaction
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var exam = await _context.Exams
                                .Include(e => e.ExamQuestions)
                                .FirstOrDefaultAsync(e => e.Id == id);

                            if (exam == null) return;

                            // 1. Cập nhật thông tin exam
                            exam.Title = model.Title;
                            exam.Description = model.Description;
                            exam.DurationMinutes = model.DurationMinutes;
                            exam.ExamDate = model.ExamDate;
                            exam.IsActive = model.IsActive;
                            exam.SubjectId = model.SubjectId;

                            // 2. Xóa tất cả ExamQuestions cũ
                            if (exam.ExamQuestions.Any())
                            {
                                _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
                                await _context.SaveChangesAsync();
                            }

                            // 3. Tạo Questions và ExamQuestions mới từ form
                            if (model.Questions != null && model.Questions.Any())
                            {
                                int order = 1;
                                foreach (var q in model.Questions)
                                {
                                    var question = new Question
                                    {
                                        Content = q.Content,
                                        AnswerA = q.AnswerA,
                                        AnswerB = q.AnswerB,
                                        AnswerC = q.AnswerC,
                                        AnswerD = q.AnswerD,
                                        CorrectAnswer = q.CorrectAnswer,
                                        Explanation = q.Explanation,
                                        Difficulty = q.Difficulty,
                                        IsActive = q.IsActive,
                                        SubjectId = model.SubjectId,
                                        CreatedAt = DateTime.Now
                                    };

                                    _context.Questions.Add(question);
                                    await _context.SaveChangesAsync();

                                    _context.ExamQuestions.Add(new ExamQuestion
                                    {
                                        ExamId = exam.Id,
                                        QuestionId = question.Id,
                                        Order = order++,
                                        Marks = q.Marks
                                    });
                                }

                                // Cập nhật TotalQuestions và TotalMarks
                                exam.TotalQuestions = model.Questions.Count;
                                exam.TotalMarks = model.Questions.Sum(q => q.Marks);
                            }
                            else
                            {
                                exam.TotalQuestions = 0;
                                exam.TotalMarks = 0;
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["Success"] = "Cập nhật đề thi thành công!";
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            ModelState.AddModelError(string.Empty, $"Lỗi: {ex.Message}");
                            throw; // Re-throw để execution strategy có thể retry
                        }
                    }
                });

                if (ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        // GET: AdminExams/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exam == null) return NotFound();

            return View(exam);
        }

        // POST: AdminExams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(id);
                if (exam == null)
                {
                    TempData["Error"] = "Không tìm thấy đề thi!";
                    return RedirectToAction(nameof(Index));
                }

                var examQuestions = await _context.ExamQuestions
                    .Where(eq => eq.ExamId == id)
                    .ToListAsync();

                if (examQuestions.Any())
                {
                    _context.ExamQuestions.RemoveRange(examQuestions);
                    await _context.SaveChangesAsync();
                }

                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa đề thi thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}