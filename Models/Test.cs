using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Test
    {
        [Key]
        public int TestId { get; set; }

        public int UserId { get; set; }

        public int SetId { get; set; }

        public double Score { get; set; } = 0;

        public int TotalQuestions { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("SetId")]
        public VocabularySet? VocabularySet { get; set; }

        public ICollection<TestQuestion> Questions { get; set; } = new List<TestQuestion>();
    }
}
