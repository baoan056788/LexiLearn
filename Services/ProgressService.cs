using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class ProgressService
    {
        private readonly AppDbContext _context;

        public ProgressService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int userId)
        {
            var totalSets = await _context.VocabularySets.CountAsync(s => s.UserId == userId);
            var totalCards = await _context.VocabularyCards
                .CountAsync(c => c.VocabularySet!.UserId == userId);

            var progresses = await _context.Progresses
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var learnedCards = progresses.Sum(p => p.LearnedCards);
            var overallMastery = progresses.Any()
                ? Math.Round(progresses.Average(p => p.MasteryPercent), 1)
                : 0;

            var totalTests = await _context.Tests.CountAsync(t => t.UserId == userId);
            var averageScore = totalTests > 0
                ? Math.Round(await _context.Tests.Where(t => t.UserId == userId).AverageAsync(t => t.Score), 1)
                : 0;

            var recentSets = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Category)
                .Include(s => s.Cards)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(6)
                .ToListAsync();

            var recentTests = await _context.Tests
                .AsNoTracking()
                .Include(t => t.VocabularySet)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentSessions = await _context.StudySessions
                .AsNoTracking()
                .Include(s => s.VocabularySet)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .Take(5)
                .ToListAsync();

            var streak = await CalculateStreakAsync(userId);

            return new DashboardViewModel
            {
                TotalSets = totalSets,
                TotalCards = totalCards,
                LearnedCards = learnedCards,
                Streak = streak,
                OverallMastery = overallMastery,
                TotalTests = totalTests,
                AverageScore = averageScore,
                RecentSets = recentSets,
                RecentTests = recentTests,
                RecentSessions = recentSessions
            };
        }

        public async Task<int> CalculateStreakAsync(int userId)
        {
            var studyDates = await _context.StudySessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.StartedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            var testDates = await _context.Tests
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .Select(t => t.CreatedAt.Date)
                .Distinct()
                .ToListAsync();

            var allDates = studyDates.Union(testDates)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (!allDates.Any()) return 0;

            int streak = 0;
            var today = DateTime.Today;

            // Check if there's activity today or yesterday
            if (allDates.First() != today && allDates.First() != today.AddDays(-1))
                return 0;

            var currentDate = allDates.First();
            foreach (var date in allDates)
            {
                if (date == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        public async Task<Progress?> GetProgressAsync(int userId, int setId)
        {
            return await _context.Progresses
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);
        }

        public async Task<List<Progress>> GetAllProgressAsync(int userId)
        {
            return await _context.Progresses
                .AsNoTracking()
                .Include(p => p.VocabularySet)
                    .ThenInclude(vs => vs!.Category)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            return new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalSets = await _context.VocabularySets.CountAsync(),
                TotalCards = await _context.VocabularyCards.CountAsync(),
                TotalStudySessions = await _context.StudySessions.CountAsync(),
                PendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending"),
                RecentUsers = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };
        }
    }
}
