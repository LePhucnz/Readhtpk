using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;
using System.ComponentModel.DataAnnotations;

namespace Readhtpk.Controllers
{
    [Authorize(Roles = "Admin")]  // Chỉ Admin truy cập được
    public class AdminUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: AdminUsers
        public async Task<IActionResult> Index()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var users = new List<UserListViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin")) continue;

                users.Add(new UserListViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }
            return View(users);
        }

        // GET: AdminUsers/Create
        public IActionResult Create()
        {
            // Chỉ cho phép tạo Teacher hoặc Student
            ViewBag.Roles = new SelectList(
                _roleManager.Roles.Where(r => r.Name != "Admin"),
                "Name", "Name");
            return View();
        }

        // POST: AdminUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            ViewBag.Roles = new SelectList(
                _roleManager.Roles.Where(r => r.Name != "Admin"),
                "Name", "Name", model.Role);

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Gán role đã chọn (Teacher hoặc Student)
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    TempData["Success"] = "Tạo tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: AdminUsers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Không cho edit nếu user là Admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin")) return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CurrentRole = roles.FirstOrDefault()
            };
            ViewBag.Roles = new SelectList(
                _roleManager.Roles.Where(r => r.Name != "Admin"),
                "Name", "Name", model.CurrentRole);
            return View(model);
        }

        // POST: AdminUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            ViewBag.Roles = new SelectList(
                _roleManager.Roles.Where(r => r.Name != "Admin"),
                "Name", "Name", model.NewRole);

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null) return NotFound();

                // Không cho edit Admin
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin")) return NotFound();

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Update role if changed
                    if (!string.IsNullOrEmpty(model.NewRole) && model.NewRole != model.CurrentRole)
                    {
                        await _userManager.RemoveFromRolesAsync(user, roles);
                        await _userManager.AddToRoleAsync(user, model.NewRole);
                    }
                    TempData["Success"] = "Cập nhật tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // POST: AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Không cho xóa Admin
                if (roles.Contains("Admin"))
                {
                    TempData["Error"] = "Không thể xóa tài khoản Admin!";
                    return RedirectToAction(nameof(Index));
                }

                // Không cho xóa chính mình
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == id)
                {
                    TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }

    // ============ VIEW MODELS ============

    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phân quyền")]
        [Display(Name = "Phân quyền")]
        public string? Role { get; set; } // Teacher hoặc Student
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ tên")]
        public string? FullName { get; set; }

        public string? CurrentRole { get; set; }

        [Display(Name = "Phân quyền mới")]
        public string? NewRole { get; set; }
    }
}