namespace LexiLearn.ViewModels
{
    public class LectureQuizViewModel
    {
        public int LectureId { get; set; }
        public string LectureTitle { get; set; } = string.Empty;
        public List<QuizItemViewModel> Questions { get; set; } = new();
        public int TotalQuestions => Questions.Count;
    }

    public class QuizItemViewModel
    {
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? UserAnswer { get; set; }
        public string? Explanation { get; set; }
        public int? SectionId { get; set; }
        public string? SectionHtml { get; set; }
    }

    public class QuizSubmitViewModel
    {
        public int LectureId { get; set; }
        public Dictionary<int, string> Answers { get; set; } = new();
    }

    public class QuizResultViewModel
    {
        public int LectureId { get; set; }
        public string LectureTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double ScorePercent => TotalQuestions > 0 ? Math.Round((double)CorrectCount / TotalQuestions * 100, 1) : 0;
        public List<QuizItemViewModel> Questions { get; set; } = new();
    }
}

