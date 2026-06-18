using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status, int page = 1)
        {
            int pageSize = 15;
            var query = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.VocabularySet)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var totalItems = await query.CountAsync();
            var reports = await query.OrderBy(r => r.Status == "Pending" ? 0 : 1) // Pending first
                                     .ThenByDescending(r => r.CreatedAt)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Status = status;

            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> Resolve(int id, string actionType)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = "Resolved";
            
            if (actionType == "delete_item")
            {
                var set = await _context.VocabularySets.FindAsync(report.SetId);
                if (set != null) _context.VocabularySets.Remove(set);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xử lý báo cáo thành công.";
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = "Rejected";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối báo cáo (Không vi phạm).";

            return RedirectToAction(nameof(Index));
        }
    }
}
