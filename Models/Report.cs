using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        public int ReporterId { get; set; }

        public int SetId { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ReporterId")]
        public User? Reporter { get; set; }

        [ForeignKey("SetId")]
        public VocabularySet? VocabularySet { get; set; }
    }
}
