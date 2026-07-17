using LexiLearn.Models;

namespace LexiLearn.ViewModels
{
    public class LectureReaderViewModel
    {
        public Lecture Lecture { get; set; } = null!;
        public List<LectureSection> Sections { get; set; } = new();
        public List<UserAnnotation> UserAnnotations { get; set; } = new();
        public List<LectureQuiz> Quizzes { get; set; } = new();
        public bool IsOwner { get; set; }
    }
}
