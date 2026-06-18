using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        public int RoleId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public ICollection<VocabularySet> VocabularySets { get; set; } = new List<VocabularySet>();
        public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
        public virtual ICollection<Test>? Tests { get; set; }
        public virtual ICollection<Progress>? Progresses { get; set; }
        public virtual ICollection<PinnedItem>? PinnedItems { get; set; }
        public ICollection<CardReview> CardReviews { get; set; } = new List<CardReview>();
        public ICollection<AiConversation> AiConversations { get; set; } = new List<AiConversation>();
    }
}
