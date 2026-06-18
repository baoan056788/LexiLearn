using LexiLearn.Data;
using LexiLearn.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class ReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VocabularyCard>> GetCardsToReviewAsync(int userId, int setId)
        {
            // Get cards that were marked as Learning or Review, or answered incorrectly
            var reviewCardIds = await _context.StudyResults
                .AsNoTracking()
                .Include(sr => sr.StudySession)
                .Where(sr => sr.StudySession!.UserId == userId && sr.StudySession.SetId == setId)
                .GroupBy(sr => sr.CardId)
                .Select(g => new
                {
                    CardId = g.Key,
                    LatestResult = g.OrderByDescending(sr => sr.CreatedAt).First()
                })
                .Where(x => x.LatestResult.Status != "Known")
                .Select(x => x.CardId)
                .ToListAsync();

            if (reviewCardIds.Any())
            {
                return await _context.VocabularyCards
                    .AsNoTracking()
                    .Where(c => reviewCardIds.Contains(c.CardId))
                    .ToListAsync();
            }

            // If no review cards found, return all cards from the set
            return await _context.VocabularyCards
                .AsNoTracking()
                .Where(c => c.SetId == setId)
                .ToListAsync();
        }

        public async Task<List<VocabularyCard>> GetAllReviewCardsAsync(int userId)
        {
            // Get all cards the user has studied that are not marked as "Known"
            var reviewCardIds = await _context.StudyResults
                .AsNoTracking()
                .Include(sr => sr.StudySession)
                .Where(sr => sr.StudySession!.UserId == userId)
                .GroupBy(sr => sr.CardId)
                .Select(g => new
                {
                    CardId = g.Key,
                    LatestResult = g.OrderByDescending(sr => sr.CreatedAt).First()
                })
                .Where(x => x.LatestResult.Status != "Known")
                .Select(x => x.CardId)
                .ToListAsync();

            return await _context.VocabularyCards
                .AsNoTracking()
                .Include(c => c.VocabularySet)
                .Where(c => reviewCardIds.Contains(c.CardId))
                .ToListAsync();
        }
    }
}
