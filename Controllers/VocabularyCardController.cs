using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class VocabularyCardController : Controller
    {
        private readonly AppDbContext _context;

        public VocabularyCardController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public record SaveWordDto(int SetId, string Term, string Meaning, string? Ipa, string? Example, string? AudioUrl);

        [HttpPost("api/VocabularyCard/CreateAjax")]
        public async Task<IActionResult> CreateAjax([FromBody] SaveWordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Term) || string.IsNullOrWhiteSpace(dto.Meaning))
                return BadRequest("Thiếu thông tin từ vựng.");

            var set = await _context.VocabularySets
                .FirstOrDefaultAsync(s => s.SetId == dto.SetId && s.UserId == GetUserId());

            if (set == null) return Unauthorized("Không tìm thấy bộ từ.");

            var card = new VocabularyCard
            {
                SetId = dto.SetId,
                Term = dto.Term,
                Meaning = dto.Meaning,
                Ipa = dto.Ipa,
                Example = dto.Example,
                AudioUrl = dto.AudioUrl,
                CreatedAt = DateTime.Now
            };

            _context.VocabularyCards.Add(card);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, cardId = card.CardId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VocabularyCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin thẻ từ.";
                return RedirectToAction("Details", "VocabularySet", new { id = model.SetId });
            }

            // Verify the set belongs to the user
            var set = await _context.VocabularySets
                .FirstOrDefaultAsync(s => s.SetId == model.SetId && s.UserId == GetUserId());

            if (set == null) return NotFound();

            var card = new VocabularyCard
            {
                SetId = model.SetId,
                Term = model.Term,
                Meaning = model.Meaning,
                Ipa = model.Ipa,
                Example = model.Example,
                ImageUrl = model.ImageUrl
            };

            _context.VocabularyCards.Add(card);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm thẻ từ \"{card.Term}\" thành công!";
            return RedirectToAction("Details", "VocabularySet", new { id = model.SetId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var card = await _context.VocabularyCards
                .Include(c => c.VocabularySet)
                .FirstOrDefaultAsync(c => c.CardId == id);

            if (card == null) return NotFound();
            if (card.VocabularySet?.UserId != GetUserId()) return Forbid();

            var model = new VocabularyCardViewModel
            {
                CardId = card.CardId,
                SetId = card.SetId,
                Term = card.Term,
                Meaning = card.Meaning,
                Ipa = card.Ipa,
                Example = card.Example,
                ImageUrl = card.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VocabularyCardViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var card = await _context.VocabularyCards
                .Include(c => c.VocabularySet)
                .FirstOrDefaultAsync(c => c.CardId == model.CardId);

            if (card == null) return NotFound();
            if (card.VocabularySet?.UserId != GetUserId()) return Forbid();

            card.Term = model.Term;
            card.Meaning = model.Meaning;
            card.Ipa = model.Ipa;
            card.Example = model.Example;
            card.ImageUrl = model.ImageUrl;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thẻ từ thành công!";
            return RedirectToAction("Details", "VocabularySet", new { id = card.SetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.VocabularyCards
                .Include(c => c.VocabularySet)
                .FirstOrDefaultAsync(c => c.CardId == id);

            if (card == null) return NotFound();
            if (card.VocabularySet?.UserId != GetUserId()) return Forbid();

            var setId = card.SetId;
            _context.VocabularyCards.Remove(card);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa thẻ từ thành công!";
            return RedirectToAction("Details", "VocabularySet", new { id = setId });
        }
    }
}
