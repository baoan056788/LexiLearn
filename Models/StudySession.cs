using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class StudySession
    {
        [Key]
        public int SessionId { get; set; }

        public int UserId { get; set; }

        public int SetId { get; set; }

        [Required, MaxLength(50)]
        public string Mode { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? EndedAt { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("SetId")]
        public VocabularySet? VocabularySet { get; set; }

        public ICollection<StudyResult> Results { get; set; } = new List<StudyResult>();
    }
}
