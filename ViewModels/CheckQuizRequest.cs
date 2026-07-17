namespace LexiLearn.ViewModels
{
    public class CheckQuizRequest
    {
        public int QuizId { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty;
    }
}
