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
using System.Security.Claims;

namespace Readhtpk.Controllers
{
    public class AdminExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminExams
        [Authorize(Roles = "Admin,Teacher,Student")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra xem user có role Student và KHÔNG phải Admin/Teacher
            if (User.IsInRole("Student") && !User.IsInRole("Admin") && !User.IsInRole("Teacher"))
            {
                // Student chỉ xem đề thi ACTIVE và chưa quá hạn
                var exams = await _context.Exams
                    .Include(e => e.Subject)
                    .Where(e => e.IsActive && e.ExamDate >= DateTime.Now)
                    .ToListAsync();

                return View(exams);
            }

            // Admin/Teacher xem tất cả
            var allExams = await _context.Exams
                .Include(e => e.Subject)
                .ToListAsync();

            return View(allExams);
        }

        // GET: AdminExams/Details/5 - Chỉ Admin/Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exam == null) return NotFound();

            return View(exam);
        }

        // GET: AdminExams/Create - Chỉ Admin/Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult Create()
        {
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name");
            return View();
        }

        // POST: AdminExams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
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

        // GET: AdminExams/Edit/5 - Chỉ Admin/Teacher
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // POST: AdminExams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DurationMinutes,TotalQuestions,TotalMarks,ExamDate,IsActive,SubjectId,CreatedBy")] Exam exam)
        {
            if (id != exam.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExamExists(exam.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SubjectId = new SelectList(_context.Subjects, "Id", "Name", exam.SubjectId);
            return View(exam);
        }

        // GET: AdminExams/Delete/5 - Chỉ Admin/Teacher
        [Authorize(Roles = "Admin,Teacher")]
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
        [Authorize(Roles = "Admin,Teacher")]
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

        // HELPER: Lấy danh sách role của user
        private async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<string>();

            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }
    }
}