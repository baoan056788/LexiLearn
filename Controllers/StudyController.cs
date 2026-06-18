using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Services;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class StudyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StudyService _studyService;

        public StudyController(AppDbContext context, StudyService studyService)
        {
            _context = context;
            _studyService = studyService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public async Task<IActionResult> Flashcard(int id)
        {
            var set = await _context.VocabularySets
                .Include(s => s.Cards)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.SetId == id);

            if (set == null) return NotFound();
            if (!set.Cards.Any())
            {
                TempData["Error"] = "Bộ từ chưa có thẻ từ nào!";
                return RedirectToAction("Details", "VocabularySet", new { id });
            }

            return View(set);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFlashcardResult([FromBody] FlashcardResultRequest request)
        {
            if (request?.Results == null || !request.Results.Any())
                return BadRequest();

            var cardResults = request.Results.ToDictionary(r => r.CardId, r => r.IsKnown);
            await _studyService.SaveFlashcardResultsAsync(GetUserId(), request.SetId, cardResults);

            return Json(new { success = true });
        }

        public async Task<IActionResult> MatchGame(int id)
        {
            var set = await _context.VocabularySets
                .Include(s => s.Cards)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.SetId == id);

            if (set == null) return NotFound();
            if (set.Cards.Count < 4)
            {
                TempData["Error"] = "Cần ít nhất 4 thẻ từ để chơi Match Game!";
                return RedirectToAction("Details", "VocabularySet", new { id });
            }

            return View(set);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMatchResult([FromBody] MatchResultRequest request)
        {
            await _studyService.SaveMatchGameResultAsync(
                GetUserId(), request.SetId, request.CorrectCount, request.TotalCards, request.TimeSeconds);

            return Json(new { success = true });
        }
    }

    public class FlashcardResultRequest
    {
        public int SetId { get; set; }
        public List<CardResult> Results { get; set; } = new();
    }

    public class CardResult
    {
        public int CardId { get; set; }
        public bool IsKnown { get; set; }
    }

    public class MatchResultRequest
    {
        public int SetId { get; set; }
        public int CorrectCount { get; set; }
        public int TotalCards { get; set; }
        public int TimeSeconds { get; set; }
    }
}
