using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class CardReview
    {
        [Key]
        public int CardReviewId { get; set; }

        public int UserId { get; set; }

        public int CardId { get; set; }

        public int RepetitionCount { get; set; }

        public int IntervalDays { get; set; }

        public double EaseFactor { get; set; } = 2.5;

        public DateTime DueAt { get; set; } = DateTime.Today;

        public DateTime? LastReviewedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(CardId))]
        public VocabularyCard? VocabularyCard { get; set; }
    }
}
