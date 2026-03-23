using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;
using Microsoft.AspNetCore.Authorization;

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
            var applicationDbContext = _context.Exams.Include(e => e.Subject);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AdminExams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null)
            {
                return NotFound();
            }

            return View(exam);
        }

        // GET: AdminExams/Create
        public IActionResult Create()
        {
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");
            return View();
        }

        // POST: AdminExams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,DurationMinutes,TotalQuestions,TotalMarks,ExamDate,IsActive,SubjectId,CreatedBy")] Exam exam)
        {
            if (ModelState.IsValid)
            {
                _context.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // GET: AdminExams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
            {
                return NotFound();
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // POST: AdminExams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DurationMinutes,TotalQuestions,TotalMarks,ExamDate,IsActive,SubjectId,CreatedBy")] Exam exam)
        {
            if (id != exam.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id))
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
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // GET: AdminExams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null)
            {
                return NotFound();
            }

            return View(exam);
        }

        // POST: AdminExams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}