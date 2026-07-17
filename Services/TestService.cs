using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class TestService
    {
        private readonly AppDbContext _context;
        private readonly StudyService _studyService;
        private readonly Random _random = new();

        public TestService(AppDbContext context, StudyService studyService)
        {
            _context = context;
            _studyService = studyService;
        }

        private static string FormatMeaning(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(",", ";").Trim();
        }

        /// <summary>
        /// Lấy nghĩa chính (từ đồng nghĩa đầu tiên/ngắn nhất) để hiển thị
        /// trong các ô trắc nghiệm, nối từ và câu đúng/sai.
        /// </summary>
        private static string GetPrimaryMeaning(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var parts = value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            // Prefer shortest synonym (most concise for display), fall back to first
            return parts.OrderBy(p => p.Length).FirstOrDefault() ?? parts[0];
        }

        /// <summary>
        /// Trả về tất cả các từ đồng nghĩa hợp lệ (lowercase, trimmed) của một nghĩa.
        /// </summary>
        private static IReadOnlyList<string> GetAllVariants(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<string>();
            return value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeAnswer(string? value)
        {
            return string.Join(";",
                FormatMeaning(value)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(part => part.ToLowerInvariant()));
        }

        private List<string> GetSelectedQuestionTypes(TestSetupViewModel setup)
        {
            var types = new List<string>();

            if (setup.IncludeMultipleChoice) types.Add("multiple-choice");
            if (setup.IncludeTrueFalse) types.Add("true-false");
            if (setup.IncludeMatching) types.Add("matching");
            if (setup.IncludeWritten) types.Add("written");

            return types;
        }

        private string PickAnswerLanguage(string configuredLanguage, int questionIndex)
        {
            return configuredLanguage switch
            {
                "Tiếng Anh" => "Tiếng Anh",
                "Tiếng Việt" => "Tiếng Việt",
                _ => questionIndex % 2 == 0 ? "Tiếng Anh" : "Tiếng Việt"
            };
        }

        private List<string> BuildWrongAnswers(List<VocabularyCard> allCards, VocabularyCard currentCard, string answerLanguage)
        {
            // Use GetPrimaryMeaning for both English and Vietnamese so options are concise
            IEnumerable<string> pool = answerLanguage == "Tiếng Anh"
                ? allCards
                    .Where(c => c.CardId != currentCard.CardId)
                    .Select(c => GetPrimaryMeaning(c.Term))
                : allCards
                    .Where(c => c.CardId != currentCard.CardId)
                    .Select(c => GetPrimaryMeaning(c.Meaning));

            return pool
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(_ => _random.Next())
                .Take(3)
                .ToList();
        }

        private TestQuestionViewModel CreateMultipleChoiceQuestion(VocabularyCard card, List<VocabularyCard> allCards, string answerLanguage)
        {
            // Display: show primary (shortest) synonym — applies to both English and Vietnamese
            var correctDisplay = answerLanguage == "Tiếng Anh"
                ? GetPrimaryMeaning(card.Term)
                : GetPrimaryMeaning(card.Meaning);

            // CorrectAnswer stores ALL variants (comma/semicolon-separated) for scoring
            var correctAnswer = answerLanguage == "Tiếng Anh"
                ? FormatMeaning(card.Term)       // now supports "term1; term2" like Meaning
                : FormatMeaning(card.Meaning);

            var questionText = answerLanguage == "Tiếng Anh"
                ? $"Từ tiếng Anh nào có nghĩa: \"{GetPrimaryMeaning(card.Meaning)}\"?"
                : $"Nghĩa tiếng Việt của từ \"{GetPrimaryMeaning(card.Term)}\" là gì?";

            var options = new List<string> { correctDisplay };
            options.AddRange(BuildWrongAnswers(allCards, card, answerLanguage));

            options = options
                .Where(option => !string.IsNullOrWhiteSpace(option))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(_ => _random.Next())
                .ToList();

            while (options.Count < 4)
            {
                options.Add(correctDisplay);
            }

            return new TestQuestionViewModel
            {
                CardId = card.CardId,
                QuestionType = "multiple-choice",
                PromptLanguage = answerLanguage == "Tiếng Anh" ? "Tiếng Việt" : "Tiếng Anh",
                AnswerLanguage = answerLanguage,
                QuestionText = questionText,
                CorrectAnswer = correctAnswer,   // full variants for scoring
                OptionA = options[0],
                OptionB = options[1],
                OptionC = options[2],
                OptionD = options[3],
                Example = card.Example
            };
        }

        private TestQuestionViewModel CreateTrueFalseQuestion(VocabularyCard card, List<VocabularyCard> allCards, string answerLanguage)
        {
            var isTrueStatement = _random.Next(2) == 0;
            // Show primary term/meaning in the statement so it looks clean
            var correctDisplay = answerLanguage == "Tiếng Anh" ? GetPrimaryMeaning(card.Term) : GetPrimaryMeaning(card.Meaning);
            var wrongDisplay = BuildWrongAnswers(allCards, card, answerLanguage).FirstOrDefault() ?? correctDisplay;
            var shownAnswer = isTrueStatement ? correctDisplay : wrongDisplay;

            var questionText = answerLanguage == "Tiếng Anh"
                ? $"Phát biểu này đúng hay sai: \"{shownAnswer}\" là từ tiếng Anh của \"{GetPrimaryMeaning(card.Meaning)}\"."
                : $"Phát biểu này đúng hay sai: \"{shownAnswer}\" là nghĩa tiếng Việt của từ \"{GetPrimaryMeaning(card.Term)}\".";

            return new TestQuestionViewModel
            {
                CardId = card.CardId,
                QuestionType = "true-false",
                PromptLanguage = "Kiểm tra",
                AnswerLanguage = "Đúng/Sai",
                QuestionText = questionText,
                CorrectAnswer = isTrueStatement ? "Đúng" : "Sai",
                OptionA = "Đúng",
                OptionB = "Sai",
                StatementText = shownAnswer,
                Example = card.Example
            };
        }

        private TestQuestionViewModel CreateMatchingQuestion(VocabularyCard card, List<VocabularyCard> allCards, string answerLanguage)
        {
            var question = CreateMultipleChoiceQuestion(card, allCards, answerLanguage);
            question.QuestionType = "matching";
            // Use primary meaning in the prompt for brevity
            question.QuestionText = answerLanguage == "Tiếng Anh"
                ? $"Chọn từ tiếng Anh khớp với nghĩa: \"{GetPrimaryMeaning(card.Meaning)}\"."
                : $"Chọn nghĩa tiếng Việt khớp với từ: \"{GetPrimaryMeaning(card.Term)}\".";
            return question;
        }

        private TestQuestionViewModel CreateWrittenQuestion(VocabularyCard card, string answerLanguage)
        {
            // For English answers: FormatMeaning(card.Term) enables multi-synonym support (e.g. "term1, term2")
            // For Vietnamese answers: FormatMeaning(card.Meaning) already handles synonyms
            var correctAnswer = answerLanguage == "Tiếng Anh"
                ? FormatMeaning(card.Term)
                : FormatMeaning(card.Meaning);

            var questionText = answerLanguage == "Tiếng Anh"
                ? $"Viết từ tiếng Anh có nghĩa: \"{GetPrimaryMeaning(card.Meaning)}\"."
                : $"Viết nghĩa tiếng Việt của từ \"{GetPrimaryMeaning(card.Term)}\".";

            return new TestQuestionViewModel
            {
                CardId = card.CardId,
                QuestionType = "written",
                PromptLanguage = answerLanguage == "Tiếng Anh" ? "Tiếng Việt" : "Tiếng Anh",
                AnswerLanguage = answerLanguage,
                QuestionText = questionText,
                CorrectAnswer = correctAnswer,
                Example = card.Example
            };
        }

        private TestQuestionViewModel CreateQuestion(string questionType, VocabularyCard card, List<VocabularyCard> allCards, string answerLanguage)
        {
            return questionType switch
            {
                "true-false" => CreateTrueFalseQuestion(card, allCards, answerLanguage),
                "matching" => CreateMatchingQuestion(card, allCards, answerLanguage),
                "written" => CreateWrittenQuestion(card, answerLanguage),
                _ => CreateMultipleChoiceQuestion(card, allCards, answerLanguage)
            };
        }

        public async Task<TestStartViewModel?> GenerateTestAsync(TestSetupViewModel setup)
        {
            var set = await _context.VocabularySets
                .AsNoTracking()
                .Include(s => s.Cards.Where(c => !c.IsHidden))
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

            var selectedTypes = GetSelectedQuestionTypes(setup);
            if (!selectedTypes.Any())
                return null;

            var countToTake = !string.IsNullOrEmpty(setup.SpecificCardIds)
                ? testCards.Count
                : Math.Min(setup.QuestionCount, testCards.Count);

            countToTake = Math.Max(1, countToTake);

            var shuffledCards = testCards
                .OrderBy(_ => _random.Next())
                .Take(countToTake)
                .ToList();

            var questions = new List<TestQuestionViewModel>();

            for (int i = 0; i < shuffledCards.Count; i++)
            {
                var card = shuffledCards[i];
                var questionType = selectedTypes[i % selectedTypes.Count];
                var answerLanguage = PickAnswerLanguage(setup.AnswerLanguage, i);
                questions.Add(CreateQuestion(questionType, card, allCards, answerLanguage));
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

                // Get all valid synonyms from the correct answer (split by , or ;)
                var validVariants = GetAllVariants(answer.CorrectAnswer);

                // Get all variants the user typed (they may also type a comma-separated list)
                var userVariants = GetAllVariants(answer.UserAnswer);

                // Accept if ANY user variant matches ANY valid variant
                var isCorrect = userVariants.Any(u => validVariants.Contains(u));

                if (isCorrect) correctCount++;

                var question = new TestQuestion
                {
                    TestId = test.TestId,
                    CardId = answer.CardId,
                    QuestionText = answer.QuestionText,
                    CorrectAnswer = FormatMeaning(answer.CorrectAnswer),
                    UserAnswer = FormatMeaning(answer.UserAnswer),
                    IsCorrect = isCorrect
                };

                questions.Add(question);
                _context.TestQuestions.Add(question);
            }

            await _studyService.UpdateCardReviewsAsync(userId, questions.ToDictionary(q => q.CardId, q => q.IsCorrect));

            test.Score = answers.Count > 0 ? Math.Round((double)correctCount / answers.Count * 100, 1) : 0;
            await _context.SaveChangesAsync();

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
                SetId = setId,
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
                SetTitle = test.VocabularySet?.Title ?? string.Empty,
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
