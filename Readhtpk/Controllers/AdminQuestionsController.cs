using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;
using Readhtpk.Models.Import;
using Readhtpk.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Readhtpk.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AdminQuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminQuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminQuestions
        public async Task<IActionResult> Index()
        {
            // Include Subject để hiển thị tên môn học trong danh sách
            var applicationDbContext = _context.Questions.Include(q => q.Subject);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AdminQuestions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // GET: AdminQuestions/Create
        public IActionResult Create()
        {
            // Lấy danh sách môn học để đưa vào Dropdown
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");
            return View();
        }

        // POST: AdminQuestions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,AnswerA,AnswerB,AnswerC,AnswerD,CorrectAnswer,Explanation,Difficulty,SubjectId,IsActive")] Question question)
        {
            if (ModelState.IsValid)
            {
                _context.Add(question);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", question.SubjectId);
            return View(question);
        }

        // GET: AdminQuestions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", question.SubjectId);
            return View(question);
        }

        // POST: AdminQuestions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,AnswerA,AnswerB,AnswerC,AnswerD,CorrectAnswer,Explanation,Difficulty,SubjectId,IsActive")] Question question)
        {
            if (id != question.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", question.SubjectId);
            return View(question);
        }

        // GET: AdminQuestions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: AdminQuestions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }

        // GET: AdminQuestions/Import
        public IActionResult Import()
        {
            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name");
            return View();
        }
        // POST: AdminQuestions/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(QuestionImportViewModel model)
        {
            ViewBag.SubjectId = new SelectList(
                _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name),
                "Id", "Name", model.SubjectId);

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError("File", "Vui lòng chọn file Excel để import");
                return View(model);
            }

            // Kiểm tra định dạng file
            var extension = Path.GetExtension(model.File.FileName).ToLower();
            if (extension != ".xlsx")
            {
                ModelState.AddModelError("File", "Chỉ hỗ trợ file Excel định dạng .xlsx");
                return View(model);
            }

            try
            {
                // Đọc và xử lý file Excel
                using (var stream = model.File.OpenReadStream())
                {
                    var (questions, results) = await ExcelImportHelper.ImportQuestionsAsync(stream, model.SubjectId);

                    // Lưu các câu hỏi hợp lệ vào Database
                    if (questions.Any())
                    {
                        await _context.Questions.AddRangeAsync(questions);
                        await _context.SaveChangesAsync();
                    }

                    // Chuẩn bị dữ liệu báo cáo
                    model.Results = results;
                    model.TotalRows = results.Count;
                    model.SuccessCount = results.Count(r => r.IsSuccess);
                    model.FailedCount = results.Count(r => !r.IsSuccess);

                    TempData["SuccessMessage"] = $"Import thành công {model.SuccessCount}/{model.TotalRows} câu hỏi!";

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi khi xử lý file: {ex.Message}");
                return View(model);
            }
        }
    }
}