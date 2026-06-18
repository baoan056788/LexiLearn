using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class NotificationRecipient
    {
        [Key]
        public int Id { get; set; }

        public int NotificationId { get; set; }

        public int UserId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
