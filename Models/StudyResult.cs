using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class StudyResult
    {
        [Key]
        public int ResultId { get; set; }

        public int SessionId { get; set; }

        public int CardId { get; set; }

        public bool IsCorrect { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Learning"; // Known, Learning, Review

        public int? AnswerTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SessionId")]
        public StudySession? StudySession { get; set; }

        [ForeignKey("CardId")]
        public VocabularyCard? VocabularyCard { get; set; }
    }
}
