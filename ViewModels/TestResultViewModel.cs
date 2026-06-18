using LexiLearn.Models;

namespace LexiLearn.ViewModels
{
    public class TestResultViewModel
    {
        public int TestId { get; set; }
        public int SetId { get; set; }
        public string SetTitle { get; set; } = string.Empty;
        public double Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    }

    public class TestStartViewModel
    {
        public int SetId { get; set; }
        public string SetTitle { get; set; } = string.Empty;
        public int TotalCards { get; set; }
        public List<TestQuestionViewModel> Questions { get; set; } = new List<TestQuestionViewModel>();
    }

    public class TestQuestionViewModel
    {
        public int CardId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string? UserAnswer { get; set; }
    }

    public class TestSubmitViewModel
    {
        public int SetId { get; set; }
        public List<TestAnswerViewModel> Answers { get; set; } = new List<TestAnswerViewModel>();
    }

    public class TestAnswerViewModel
    {
        public int CardId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
    }
}
