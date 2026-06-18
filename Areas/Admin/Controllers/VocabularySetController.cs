using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class VocabularySetController : Controller
    {
        private readonly AppDbContext _context;

        public VocabularySetController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchString, int? categoryId, int page = 1)
        {
            int pageSize = 12;
            var query = _context.VocabularySets
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(v => v.Title.Contains(searchString) || (v.Description != null && v.Description.Contains(searchString)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(v => v.CategoryId == categoryId.Value);
            }

            var totalItems = await query.CountAsync();
            var sets = await query.OrderByDescending(v => v.CreatedAt)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            ViewBag.Categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;

            return View(sets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var set = await _context.VocabularySets
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.Category)
                .Include(v => v.Cards)
                .FirstOrDefaultAsync(v => v.SetId == id);

            if (set == null) return NotFound();

            return View(set);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var set = await _context.VocabularySets.FindAsync(id);
            if (set == null) return NotFound();

            set.IsPublic = !set.IsPublic;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(set.IsPublic ? "công khai" : "ẩn")} bộ từ '{set.Title}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var set = await _context.VocabularySets.FindAsync(id);
            if (set == null) return NotFound();

            _context.VocabularySets.Remove(set);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bộ từ vựng thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
