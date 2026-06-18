using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LexiLearn.Data;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userCount = await _context.Users.CountAsync();
            var setCount = await _context.VocabularySets.CountAsync();
            var cardCount = await _context.VocabularyCards.CountAsync();
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending");
            
            var recentDate = DateTime.Now.AddDays(-7);
            var studySessionsLast7Days = await _context.StudySessions
                .Where(s => s.StartedAt >= recentDate)
                .CountAsync();

            ViewBag.UserCount = userCount;
            ViewBag.SetCount = setCount;
            ViewBag.CardCount = cardCount;
            ViewBag.PendingReports = pendingReports;
            ViewBag.StudySessionsLast7Days = studySessionsLast7Days;

            return View();
        }
    }
}
