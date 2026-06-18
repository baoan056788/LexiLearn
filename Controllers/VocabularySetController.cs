using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using ExcelDataReader;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class VocabularySetController : Controller
    {
        private readonly AppDbContext _context;

        public VocabularySetController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public async Task<IActionResult> MySets(string? mode = null)
        {
            var userId = GetUserId();
            var sets = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Mode = mode;
            return View(sets);
        }

        [HttpGet("api/VocabularySet/MySets")]
        public async Task<IActionResult> MySetsApi()
        {
            var userId = GetUserId();
            var sets = await _context.VocabularySets
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.SetId, s.Title })
                .ToListAsync();

            return Json(sets);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new VocabularySetViewModel
            {
                Categories = await _context.Categories.AsNoTracking().ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VocabularySetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
                return View(model);
            }

            var set = new VocabularySet
            {
                UserId = GetUserId(),
                CategoryId = model.CategoryId,
                Title = model.Title,
                Description = model.Description,
                IsPublic = model.IsPublic
            };

            _context.VocabularySets.Add(set);
            await _context.SaveChangesAsync();

            if (model.ImportFile != null && model.ImportFile.Length > 0)
            {
                try
                {
                    using (var stream = model.ImportFile.OpenReadStream())
                    {
                        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                        {
                            var result = reader.AsDataSet();
                            if (result.Tables.Count > 0)
                            {
                                var table = result.Tables[0];
                                var cards = new List<VocabularyCard>();
                                var importedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                // Skip first row (header)
                                for (int i = 1; i < table.Rows.Count; i++)
                                {
                                    var row = table.Rows[i];
                                    if (row[0] != null && !string.IsNullOrWhiteSpace(row[0].ToString()))
                                    {
                                        var term = row[0].ToString()!.Trim();
                                        var meaning = table.Columns.Count > 1 && row[1] != null ? row[1].ToString()!.Trim() : "";
                                        var ipa = table.Columns.Count > 2 && row[2] != null ? row[2].ToString()?.Trim() : null;
                                        var example = table.Columns.Count > 3 && row[3] != null ? row[3].ToString()?.Trim() : null;

                                        if (!string.IsNullOrWhiteSpace(meaning) && importedTerms.Add(term))
                                        {
                                            cards.Add(new VocabularyCard
                                            {
                                                SetId = set.SetId,
                                                Term = term,
                                                Meaning = meaning,
                                                Ipa = ipa,
                                                Example = example
                                            });
                                        }
                                    }
                                }
                                _context.VocabularyCards.AddRange(cards);
                                await _context.SaveChangesAsync();
                                TempData["Success"] = $"Tạo bộ từ và nạp thành công {cards.Count} từ vựng!";
                                return RedirectToAction("Details", new { id = set.SetId });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    TempData["Error"] = "Tạo bộ từ thành công nhưng xảy ra lỗi khi đọc file Excel. Vui lòng kiểm tra lại định dạng file.";
                    return RedirectToAction("Details", new { id = set.SetId });
                }
            }

            TempData["Success"] = "Tạo bộ từ thành công!";
            return RedirectToAction("Details", new { id = set.SetId });
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetUserId();
            var set = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SetId == id);

            if (set == null) return NotFound();

            // Allow access if the set belongs to the user or is public
            if (set.UserId != userId && !set.IsPublic)
                return Forbid();

            ViewBag.IsOwner = set.UserId == userId;
            return View(set);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();
            var set = await _context.VocabularySets
                .FirstOrDefaultAsync(s => s.SetId == id && s.UserId == userId);

            if (set == null) return NotFound();

            var model = new VocabularySetViewModel
            {
                SetId = set.SetId,
                Title = set.Title,
                Description = set.Description,
                CategoryId = set.CategoryId,
                IsPublic = set.IsPublic,
                Categories = await _context.Categories.AsNoTracking().ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VocabularySetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
                return View(model);
            }

            var userId = GetUserId();
            var set = await _context.VocabularySets
                .FirstOrDefaultAsync(s => s.SetId == model.SetId && s.UserId == userId);

            if (set == null) return NotFound();

            set.Title = model.Title;
            set.Description = model.Description;
            set.CategoryId = model.CategoryId;
            set.IsPublic = model.IsPublic;
            set.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật bộ từ thành công!";
            return RedirectToAction("Details", new { id = set.SetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Copy(int id)
        {
            var userId = GetUserId();
            var originalSet = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.SetId == id && s.IsPublic);

            if (originalSet == null) return NotFound("Bộ từ không tồn tại hoặc không được chia sẻ công khai.");
            if (originalSet.UserId == userId)
            {
                TempData["Error"] = "Bạn không thể sao chép bộ từ của chính mình.";
                return RedirectToAction("Details", new { id = id });
            }

            var newSet = new VocabularySet
            {
                UserId = userId,
                CategoryId = originalSet.CategoryId,
                Title = originalSet.Title + " (Bản sao)",
                Description = originalSet.Description,
                IsPublic = false
            };

            _context.VocabularySets.Add(newSet);
            await _context.SaveChangesAsync();

            _context.VocabularyCards.AddRange(originalSet.Cards.Select(card => new VocabularyCard
            {
                SetId = newSet.SetId,
                Term = card.Term,
                Meaning = card.Meaning,
                Ipa = card.Ipa,
                Example = card.Example,
                ImageUrl = card.ImageUrl,
                AudioUrl = card.AudioUrl
            }));
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã sao chép bộ từ thành công!";
            return RedirectToAction("Details", new { id = newSet.SetId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var set = await _context.VocabularySets
                .FirstOrDefaultAsync(s => s.SetId == id && s.UserId == userId);

            if (set == null) return NotFound();

            _context.VocabularySets.Remove(set);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa bộ từ thành công!";
            return RedirectToAction("MySets");
        }
    }
}
