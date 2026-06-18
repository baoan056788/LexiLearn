using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class FeedbackController : Controller
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status, int page = 1)
        {
            int pageSize = 15;
            var query = _context.Feedbacks.Include(f => f.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(f => f.Status == status);
            }

            var totalItems = await query.CountAsync();
            var feedbacks = await query.OrderBy(f => f.Status == "Pending" ? 0 : 1)
                                       .ThenByDescending(f => f.CreatedAt)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Status = status;

            return View(feedbacks);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                feedback.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật trạng thái phản hồi.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa phản hồi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
