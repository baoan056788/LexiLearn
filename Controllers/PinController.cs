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

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
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

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
