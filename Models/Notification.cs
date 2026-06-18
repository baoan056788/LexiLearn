using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = "system";

        [Required, MaxLength(50)]
        public string Status { get; set; } = "draft";

        public int CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? SentAt { get; set; }

        [ForeignKey("CreatedById")]
        public User? CreatedBy { get; set; }

        public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    }
}
