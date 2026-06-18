using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.ViewModels;

namespace LexiLearn.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var publicSets = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Where(s => s.IsPublic)
                .OrderByDescending(s => s.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalSets = await _context.VocabularySets.CountAsync();
            ViewBag.TotalCards = await _context.VocabularyCards.CountAsync();

            return View(publicSets);
        }

        public async Task<IActionResult> Explore(string? search, int? categoryId, int page = 1)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            var query = _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Where(s => s.IsPublic);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search) || (s.Description != null && s.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);

            var totalItems = await query.CountAsync();
            var pageSize = 12;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var sets = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ExploreViewModel
            {
                SearchQuery = search,
                CategoryId = categoryId,
                Categories = categories,
                Sets = sets,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
