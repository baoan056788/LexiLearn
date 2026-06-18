using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // new, read, processed
        [Required, MaxLength(50)]
        public string Status { get; set; } = "new";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
