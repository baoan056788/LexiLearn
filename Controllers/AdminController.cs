using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.Services;

namespace LexiLearn.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;

        public AdminController(AppDbContext context, ProgressService progressService)
        {
            _context = context;
            _progressService = progressService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _progressService.GetAdminDashboardAsync();
            return View(model);
        }

        public async Task<IActionResult> Users(string? search)
        {
            var query = _context.Users.Include(u => u.Role).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.VocabularySets)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, string? description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction("Categories");
            }

            _context.Categories.Add(new Category
            {
                CategoryName = categoryName,
                Description = description
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công!";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.VocabularySets)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound();

            if (category.VocabularySets.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có bộ từ.";
                return RedirectToAction("Categories");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> Sets(string? search)
        {
            var query = _context.VocabularySets
                .Include(s => s.User)
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Where(s => s.IsPublic);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search));

            var sets = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            return View(sets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSetVisibility(int id)
        {
            var set = await _context.VocabularySets.FindAsync(id);
            if (set == null) return NotFound();

            set.IsPublic = !set.IsPublic;
            await _context.SaveChangesAsync();

            TempData["Success"] = set.IsPublic ? "Đã công khai bộ từ." : "Đã ẩn bộ từ.";
            return RedirectToAction("Sets");
        }

        public async Task<IActionResult> Reports()
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.VocabularySet)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id, string status)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái báo cáo thành công!";
            return RedirectToAction("Reports");
        }
    }
}
