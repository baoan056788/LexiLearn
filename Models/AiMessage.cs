using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class AiMessage
    {
        [Key]
        public int AiMessageId { get; set; }

        public int AiConversationId { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "user";

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(AiConversationId))]
        public AiConversation? Conversation { get; set; }
    }
}
