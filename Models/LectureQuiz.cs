using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class LectureQuiz
    {
        [Key]
        public int QuizId { get; set; }

        public int LectureId { get; set; }

        public int? SectionId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OptionA { get; set; }

        [MaxLength(500)]
        public string? OptionB { get; set; }

        [MaxLength(500)]
        public string? OptionC { get; set; }

        [MaxLength(500)]
        public string? OptionD { get; set; }

        [Required, MaxLength(1)]
        public string CorrectAnswer { get; set; } = "A";

        public string? Explanation { get; set; }

        public int SortOrder { get; set; } = 0;

        [ForeignKey("LectureId")]
        public Lecture? Lecture { get; set; }

        [ForeignKey("SectionId")]
        public LectureSection? Section { get; set; }
    }
}
