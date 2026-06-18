using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class PinController : Controller
    {
        private readonly AppDbContext _context;

        public PinController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        private IActionResult RedirectSafely(string? returnUrl, PinnedItem? pin = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                && !returnUrl.StartsWith("/Test/Start", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            if (pin?.ItemType == "VocabularySet" && pin.ItemId.HasValue)
            {
                return RedirectToAction("Details", "VocabularySet", new { id = pin.ItemId.Value });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(string itemType, int? itemId, string title, string url, string returnUrl)
        {
            var userId = GetUserId();

            var existingPin = await _context.PinnedItems
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ItemType == itemType && p.ItemId == itemId && p.Url == url);

            if (existingPin != null)
            {
                _context.PinnedItems.Remove(existingPin);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã bỏ ghim thành công.";
            }
            else
            {
                var newPin = new PinnedItem
                {
                    UserId = userId,
                    ItemType = itemType,
                    ItemId = itemId,
                    Title = title,
                    Url = url,
                    CreatedAt = DateTime.Now
                };
                _context.PinnedItems.Add(newPin);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã ghim thành công.";
            }

            return RedirectSafely(returnUrl, existingPin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpin(int id, string returnUrl)
        {
            var userId = GetUserId();
            var pin = await _context.PinnedItems.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (pin != null)
            {
                _context.PinnedItems.Remove(pin);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã bỏ ghim.";
            }

            return RedirectSafely(returnUrl, pin);
        }
    }
}
