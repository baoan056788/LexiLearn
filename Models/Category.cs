using System.ComponentModel.DataAnnotations;

namespace LexiLearn.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public ICollection<VocabularySet> VocabularySets { get; set; } = new List<VocabularySet>();
    }
}
