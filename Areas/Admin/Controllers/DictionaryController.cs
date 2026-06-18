using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class DictionaryController : Controller
    {
        private readonly AppDbContext _context;

        public DictionaryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            int pageSize = 20;
            var query = _context.WordDefinitions.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(w => w.Word.Contains(searchString));
            }

            var totalItems = await query.CountAsync();
            var words = await query.OrderBy(w => w.Word)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;

            return View(words);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var word = await _context.WordDefinitions.FindAsync(id);
            if (word != null)
            {
                _context.WordDefinitions.Remove(word);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa từ khỏi từ điển nội bộ.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
