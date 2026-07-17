using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class StudyNote
    {
        [Key]
        public int StudyNoteId { get; set; }

        public int UserId { get; set; }

        public int? SetId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? RelatedTerm { get; set; }

        [MaxLength(250)]
        public string? Tags { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsPinned { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(SetId))]
        public VocabularySet? VocabularySet { get; set; }
    }
}
