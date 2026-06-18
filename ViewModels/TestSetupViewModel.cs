namespace LexiLearn.ViewModels
{
    public class TestSetupViewModel
    {
        public int SetId { get; set; }
        public int QuestionCount { get; set; } = 20;
        public string AnswerLanguage { get; set; } = "Cả hai"; // Tiếng Anh, Tiếng Việt, Cả hai
        public bool IncludeTrueFalse { get; set; } = true;
        public bool IncludeMultipleChoice { get; set; } = true;
        public bool IncludeMatching { get; set; } = false;
        public bool IncludeWritten { get; set; } = false;
        public string? SpecificCardIds { get; set; }
    }
}
