using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class VocabularyCard
    {
        [Key]
        public int CardId { get; set; }

        public int SetId { get; set; }

        [Required, MaxLength(100)]
        public string Term { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Meaning { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Ipa { get; set; }

        [MaxLength(500)]
        public string? Example { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        [MaxLength(255)]
        public string? AudioUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SetId")]
        public VocabularySet? VocabularySet { get; set; }

        public ICollection<StudyResult> StudyResults { get; set; } = new List<StudyResult>();
        public ICollection<CardReview> CardReviews { get; set; } = new List<CardReview>();
    }
}
