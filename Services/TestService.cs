using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class TestService
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new();

        public TestService(AppDbContext context)
        {
            _context = context;
        }

        private static string FormatMeaning(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(",", ";").Trim();
        }

        private static string NormalizeAnswer(string? value)
        {
            return string.Join(";",
                FormatMeaning(value)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(part => part.ToLowerInvariant()));
        }

        public async Task<TestStartViewModel?> GenerateTestAsync(TestSetupViewModel setup)
        {
            var set = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.SetId == setup.SetId);

            if (set == null || set.Cards.Count < 4)
                return null;

            var allCards = set.Cards.ToList();
            var testCards = allCards;

            if (!string.IsNullOrEmpty(setup.SpecificCardIds))
            {
                var specificIds = setup.SpecificCardIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToHashSet();
                testCards = allCards.Where(c => specificIds.Contains(c.CardId)).ToList();
            }

            var questions = new List<TestQuestionViewModel>();

            // Shuffle cards and take requested count
            var countToTake = !string.IsNullOrEmpty(setup.SpecificCardIds) ? testCards.Count : setup.QuestionCount;
            var shuffledCards = testCards.OrderBy(x => _random.Next()).Take(countToTake).ToList();

            foreach (var card in shuffledCards)
            {
                var formattedMeaning = FormatMeaning(card.Meaning);
                
                // Get 3 wrong answers from ALL cards
                var wrongAnswers = allCards
                    .Where(c => c.CardId != card.CardId)
                    .OrderBy(x => _random.Next())
                    .Take(3)
                    .Select(c => FormatMeaning(c.Meaning))
                    .ToList();

                // Create options list with correct answer
                var options = new List<string> { formattedMeaning };
                options.AddRange(wrongAnswers);

                // Shuffle options
                options = options.OrderBy(x => _random.Next()).ToList();

                questions.Add(new TestQuestionViewModel
                {
                    CardId = card.CardId,
                    QuestionText = $"Nghĩa của từ \"{card.Term}\" là gì?",
                    CorrectAnswer = formattedMeaning,
                    OptionA = options[0],
                    OptionB = options[1],
                    OptionC = options[2],
                    OptionD = options[3]
                });
            }

            return new TestStartViewModel
            {
                SetId = setup.SetId,
                SetTitle = set.Title,
                TotalCards = testCards.Count,
                Questions = questions
            };
        }

        public async Task<TestResultViewModel> SubmitTestAsync(int userId, int setId, List<TestAnswerViewModel> answers)
        {
            var set = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.SetId == setId);

            if (set == null)
                throw new Exception("Không tìm thấy bộ từ");

            int correctCount = 0;
            var test = new Test
            {
                UserId = userId,
                SetId = setId,
                TotalQuestions = answers.Count
            };

            _context.Tests.Add(test);
            await _context.SaveChangesAsync();

            var questions = new List<TestQuestion>();

            foreach (var answer in answers)
            {
                var card = set.Cards.FirstOrDefault(c => c.CardId == answer.CardId);
                if (card == null) continue;

                var isCorrect = NormalizeAnswer(card.Meaning) == NormalizeAnswer(answer.UserAnswer);
                if (isCorrect) correctCount++;

                var question = new TestQuestion
                {
                    TestId = test.TestId,
                    CardId = answer.CardId,
                    QuestionText = $"Nghĩa của từ \"{card.Term}\" là gì?",
                    CorrectAnswer = FormatMeaning(card.Meaning),
                    UserAnswer = FormatMeaning(answer.UserAnswer),
                    IsCorrect = isCorrect
                };
                questions.Add(question);
                _context.TestQuestions.Add(question);
            }

            test.Score = answers.Count > 0 ? Math.Round((double)correctCount / answers.Count * 100, 1) : 0;
            await _context.SaveChangesAsync();

            // Update progress
            var totalCards = await _context.VocabularyCards.CountAsync(c => c.SetId == setId);
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);

            if (progress == null)
            {
                progress = new Progress { UserId = userId, SetId = setId };
                _context.Progresses.Add(progress);
            }
            progress.TotalCards = totalCards;
            progress.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return new TestResultViewModel
            {
                TestId = test.TestId,
                SetTitle = set.Title,
                Score = test.Score,
                TotalQuestions = test.TotalQuestions,
                CorrectCount = correctCount,
                CreatedAt = test.CreatedAt,
                Questions = questions
            };
        }

        public async Task<List<Test>> GetTestHistoryAsync(int userId)
        {
            return await _context.Tests
                .AsNoTracking()
                .Include(t => t.VocabularySet)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<TestResultViewModel?> GetTestResultAsync(int testId)
        {
            var test = await _context.Tests
                .AsNoTracking()
                .Include(t => t.VocabularySet)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.VocabularyCard)
                .FirstOrDefaultAsync(t => t.TestId == testId);

            if (test == null) return null;

            return new TestResultViewModel
            {
                TestId = test.TestId,
                SetId = test.SetId,
                SetTitle = test.VocabularySet?.Title ?? "",
                Score = test.Score,
                TotalQuestions = test.TotalQuestions,
                CorrectCount = test.Questions.Count(q => q.IsCorrect),
                CreatedAt = test.CreatedAt,
                Questions = test.Questions.ToList()
            };
        }

        public async Task<bool> DeleteTestHistoryAsync(int testId, int userId)
        {
            var test = await _context.Tests.FirstOrDefaultAsync(t => t.TestId == testId && t.UserId == userId);
            if (test == null) return false;
            
            var questions = _context.TestQuestions.Where(q => q.TestId == testId);
            _context.TestQuestions.RemoveRange(questions);
            
            _context.Tests.Remove(test);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
