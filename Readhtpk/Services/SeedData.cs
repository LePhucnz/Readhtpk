using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Readhtpk.Data;
using Readhtpk.Models;

namespace Readhtpk.Services
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Seed từng phần với ConfigureAwait(false)
            await SeedRoles(roleManager).ConfigureAwait(false);
            await SeedAdminUser(userManager).ConfigureAwait(false);
            await SeedSubjects(context).ConfigureAwait(false);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            try
            {
                string[] roleNames = { "Admin", "Teacher", "Student" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
                        Console.WriteLine($"✅ Created role: {roleName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Lỗi seed roles: {ex.Message}");
            }
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            try
            {
                var adminEmail = "admin@exam.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail).ConfigureAwait(false);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "System Administrator",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123").ConfigureAwait(false);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin").ConfigureAwait(false);
                        Console.WriteLine("✅ Created admin user: admin@exam.com");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Lỗi seed admin: {ex.Message}");
            }
        }

        private static async Task SeedSubjects(ApplicationDbContext context)
        {
            try
            {
                // Kiểm tra kết nối trước
                if (await context.Database.CanConnectAsync().ConfigureAwait(false))
                {
                    if (!await context.Subjects.AnyAsync().ConfigureAwait(false))
                    {
                        var subjects = new List<Subject>
                        {
                            new Subject { Name = "Toán Học", Code = "MATH", Description = "Môn Toán", IsActive = true },
                            new Subject { Name = "Vật Lý", Code = "PHY", Description = "Môn Vật Lý", IsActive = true },
                            new Subject { Name = "Hóa Học", Code = "CHEM", Description = "Môn Hóa Học", IsActive = true },
                            new Subject { Name = "Tiếng Anh", Code = "ENG", Description = "Môn Tiếng Anh", IsActive = true },
                            new Subject { Name = "Ngữ Văn", Code = "LIT", Description = "Môn Ngữ Văn", IsActive = true }
                        };

                        await context.Subjects.AddRangeAsync(subjects).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                        Console.WriteLine("✅ Seeded 5 default subjects");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Lỗi seed subjects: {ex.Message}");
            }
        }
    }
}