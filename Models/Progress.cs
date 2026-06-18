using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Progress
    {
        [Key]
        public int ProgressId { get; set; }

        public int UserId { get; set; }

        public int SetId { get; set; }

        public int TotalCards { get; set; } = 0;

        public int LearnedCards { get; set; } = 0;

        public int ReviewingCards { get; set; } = 0;

        public double MasteryPercent { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("SetId")]
        public VocabularySet? VocabularySet { get; set; }
    }
}
