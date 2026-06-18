using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class TestQuestion
    {
        [Key]
        public int QuestionId { get; set; }

        public int TestId { get; set; }

        public int CardId { get; set; }

        [Required, MaxLength(255)]
        public string QuestionText { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string CorrectAnswer { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? UserAnswer { get; set; }

        public bool IsCorrect { get; set; } = false;

        [MaxLength(255)]
        public string? OptionA { get; set; }
        [MaxLength(255)]
        public string? OptionB { get; set; }
        [MaxLength(255)]
        public string? OptionC { get; set; }
        [MaxLength(255)]
        public string? OptionD { get; set; }

        [ForeignKey("TestId")]
        public Test? Test { get; set; }

        [ForeignKey("CardId")]
        public VocabularyCard? VocabularyCard { get; set; }
    }
}
