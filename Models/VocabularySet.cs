using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class VocabularySet
    {
        [Key]
        public int SetId { get; set; }

        public int UserId { get; set; }

        public int? CategoryId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public ICollection<VocabularyCard> Cards { get; set; } = new List<VocabularyCard>();
        public ICollection<Test> Tests { get; set; } = new List<Test>();
        public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
        public ICollection<Progress> Progresses { get; set; } = new List<Progress>();
    }
}
