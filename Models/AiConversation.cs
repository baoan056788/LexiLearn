using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class AiConversation
    {
        [Key]
        public int AiConversationId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = "Cuộc trò chuyện mới";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
    }
}
