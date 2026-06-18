using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class RelatedWord
    {
        [Key]
        public int Id { get; set; }

        public int WordId { get; set; }

        [Required, MaxLength(100)]
        public string RelatedWordText { get; set; } = string.Empty;

        [ForeignKey("WordId")]
        public WordDefinition? WordDefinition { get; set; }
    }
}
