using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;

namespace Readhtpk.Controllers
{
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
    }
}