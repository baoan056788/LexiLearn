using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class LearnController : Controller
    {
        private readonly AppDbContext _context;

        public LearnController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public async Task<IActionResult> Index(int id)
        {
            var set = await _context.VocabularySets
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.SetId == id);

            if (set == null) return NotFound();
            if (set.Cards.Count < 4)
            {
                TempData["Error"] = "Cần ít nhất 4 thẻ từ để học chế độ này!";
                return RedirectToAction("Details", "VocabularySet", new { id });
            }

            return View(set);
        }
    }
}
