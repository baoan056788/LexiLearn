using LexiLearn.Data;
using LexiLearn.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class StudyService
    {
        private readonly AppDbContext _context;

        public StudyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StudySession> CreateSessionAsync(int userId, int setId, string mode)
        {
            var session = new StudySession
            {
                UserId = userId,
                SetId = setId,
                Mode = mode,
                StartedAt = DateTime.Now
            };

            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task EndSessionAsync(int sessionId)
        {
            var session = await _context.StudySessions.FindAsync(sessionId);
            if (session != null)
            {
                session.EndedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<VocabularyCard>> GetCardsForStudyAsync(int setId)
        {
            return await _context.VocabularyCards
                .AsNoTracking()
                .Where(c => c.SetId == setId)
                .OrderBy(c => c.CardId)
                .ToListAsync();
        }

        public async Task SaveStudyResultAsync(int sessionId, int cardId, bool isCorrect, string status)
        {
            var result = new StudyResult
            {
                SessionId = sessionId,
                CardId = cardId,
                IsCorrect = isCorrect,
                Status = status
            };

            _context.StudyResults.Add(result);
            await _context.SaveChangesAsync();
        }

        public async Task SaveFlashcardResultsAsync(int userId, int setId, Dictionary<int, bool> cardResults)
        {
            var session = await CreateSessionAsync(userId, setId, "Flashcard");

            foreach (var kvp in cardResults)
            {
                var result = new StudyResult
                {
                    SessionId = session.SessionId,
                    CardId = kvp.Key,
                    IsCorrect = kvp.Value,
                    Status = kvp.Value ? "Known" : "Learning"
                };
                _context.StudyResults.Add(result);
            }

            session.EndedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Update progress
            await UpdateProgressAsync(userId, setId);
        }

        public async Task SaveMatchGameResultAsync(int userId, int setId, int correctCount, int totalCards, int timeSeconds)
        {
            var session = new StudySession
            {
                UserId = userId,
                SetId = setId,
                Mode = "MatchGame",
                StartedAt = DateTime.Now.AddSeconds(-timeSeconds),
                EndedAt = DateTime.Now
            };
            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();

            await UpdateProgressAsync(userId, setId);
        }

        private async Task UpdateProgressAsync(int userId, int setId)
        {
            var totalCards = await _context.VocabularyCards.CountAsync(c => c.SetId == setId);

            // Get the latest result for each card
            var latestResults = await _context.StudyResults
                .Include(sr => sr.StudySession)
                .Where(sr => sr.StudySession!.UserId == userId && sr.StudySession.SetId == setId)
                .GroupBy(sr => sr.CardId)
                .Select(g => g.OrderByDescending(sr => sr.CreatedAt).First())
                .ToListAsync();

            var learnedCards = latestResults.Count(r => r.Status == "Known");
            var reviewingCards = latestResults.Count(r => r.Status == "Review" || r.Status == "Learning");

            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);

            if (progress == null)
            {
                progress = new Progress
                {
                    UserId = userId,
                    SetId = setId
                };
                _context.Progresses.Add(progress);
            }

            progress.TotalCards = totalCards;
            progress.LearnedCards = learnedCards;
            progress.ReviewingCards = reviewingCards;
            progress.MasteryPercent = totalCards > 0 ? Math.Round((double)learnedCards / totalCards * 100, 1) : 0;
            progress.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}
