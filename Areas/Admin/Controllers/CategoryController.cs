using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string categoryName, string description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var category = new Category
            {
                CategoryName = categoryName,
                Description = description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string categoryName, string description)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            category.CategoryName = categoryName;
            category.Description = description;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Check if any sets are using this category
            var hasSets = await _context.VocabularySets.AnyAsync(s => s.CategoryId == id);
            if (hasSets)
            {
                TempData["Error"] = "Không thể xóa danh mục đang có chứa bộ từ vựng.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
