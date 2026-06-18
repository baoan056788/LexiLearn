using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .Include(n => n.CreatedBy)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string title, string message, string type, string targetType, int? targetUserId)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Tiêu đề và nội dung không được để trống");
                return View();
            }

            var adminId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var notification = new Notification
            {
                Title = title,
                Content = message,
                Type = type ?? "system",
                CreatedById = int.Parse(adminId ?? "1"),
                CreatedAt = DateTime.Now,
                Status = "sent"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Create recipients
            var recipientIds = new List<int>();
            if (targetType == "All")
            {
                recipientIds = await _context.Users.Where(u => u.IsActive).Select(u => u.UserId).ToListAsync();
            }
            else if (targetType == "Specific" && targetUserId.HasValue)
            {
                recipientIds.Add(targetUserId.Value);
            }

            var recipients = recipientIds.Select(userId => new NotificationRecipient
            {
                NotificationId = notification.NotificationId,
                UserId = userId,
                IsRead = false
            });

            _context.NotificationRecipients.AddRange(recipients);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi thông báo thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thông báo.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
